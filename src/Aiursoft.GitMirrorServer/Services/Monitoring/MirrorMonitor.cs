using System.Security.Cryptography;
using System.Text;
using Aiursoft.GitMirrorServer.Entities;

namespace Aiursoft.GitMirrorServer.Services.Monitoring;

public sealed class MirrorMonitor
{
    private readonly object _gate = new();
    private readonly Dictionary<string, MirrorSnapshot> _states = new(StringComparer.Ordinal);
    public SemaphoreSlim MirrorGate { get; } = new(1, 1);
    public SemaphoreSlim SyncGate { get; } = new(1, 1);

    public void Observe(MirrorConfiguration config, MirrorRepoExecution result, DateTimeOffset observedAt)
    {
        var source = SafeServer(config.FromServer) + "/" + config.FromOrgName;
        var target = SafeServer(config.TargetServer) + "/" + config.TargetOrgName;
        var identity = $"{config.Id:N}\n{config.FromType}\n{source}\n{config.TargetType}\n{target}\n{result.RepoName}";
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        lock (_gate)
        {
            _states.TryGetValue(key, out var previous);
            if (previous?.JobId == result.JobExecutionId) return;
            var failures = result.IsSuccess ? 0 : Math.Min((previous?.Failures ?? 0) + 1, 1000000);
            _states[key] = new MirrorSnapshot(key, config.Id, result.RepoName, source, target,
                result.JobExecutionId, failures, result.IsSuccess ? null : failures == 1 ? observedAt : previous?.FailureSince,
                observedAt, result.IsSuccess ? observedAt : previous?.LastSuccess,
                result.IsSuccess ? "" : ClassifyFailure(result.ErrorMessage),
                previous?.NextReportId ?? Guid.NewGuid(),
                result.IsSuccess ? Guid.NewGuid() : previous?.PendingRecovery);
        }
    }

    public List<MirrorSnapshot> Snapshot()
    {
        lock (_gate) return _states.Values.ToList();
    }

    public MirrorSnapshot? Snapshot(string key)
    {
        lock (_gate) return _states.GetValueOrDefault(key);
    }

    public void AdvanceReceipt(string key, Guid reportId)
    {
        lock (_gate)
            if (_states.TryGetValue(key, out var state) && state.NextReportId == reportId)
                _states[key] = state with { NextReportId = Guid.NewGuid() };
    }

    public void ConfirmRecovery(string key, Guid recovery)
    {
        lock (_gate)
            if (_states.TryGetValue(key, out var state) && state.PendingRecovery == recovery)
                _states[key] = state with { PendingRecovery = null };
    }

    private static string SafeServer(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return "(configured Git server)";
        return uri.GetLeftPart(UriPartial.Authority).Replace(uri.UserInfo + "@", "", StringComparison.Ordinal)
               + uri.AbsolutePath.TrimEnd('/');
    }

    // Git errors can contain authenticated remote URLs. Do not copy raw errors or logs into incidents.
    private static string ClassifyFailure(string? error)
    {
        var text = error?.ToLowerInvariant() ?? "";
        if (text.Contains("authentication") || text.Contains("401") || text.Contains("403") || text.Contains("permission denied"))
            return "Authentication or permission failure. Verify source-read and target-write access and token validity.";
        if (text.Contains("timed out") || text.Contains("timeout") || text.Contains("resolve host") || text.Contains("connect"))
            return "Connection, DNS or timeout failure. Check connectivity to both Git servers.";
        if (text.Contains("no space") || text.Contains("disk full"))
            return "Local storage is full. Check free space in the mirror workspace.";
        if (text.Contains("rejected") || text.Contains("protected branch"))
            return "The target rejected the push. Check branch protection and mirror write permissions.";
        return "The repository mirror did not complete. Consult the protected execution log for the precise Git error.";
    }
}

public sealed record MirrorSnapshot(string Key, Guid ConfigurationId, string Repository, string Source, string Target,
    Guid JobId, int Failures, DateTimeOffset? FailureSince, DateTimeOffset LastObserved, DateTimeOffset? LastSuccess,
    string FailureSummary, Guid NextReportId, Guid? PendingRecovery);
