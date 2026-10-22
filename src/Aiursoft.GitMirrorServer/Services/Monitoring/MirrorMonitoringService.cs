using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.Canon.BackgroundJobs;
using Newtonsoft.Json.Linq;

namespace Aiursoft.GitMirrorServer.Services.Monitoring;

public sealed class MirrorMonitoringService(MirrorMonitor monitor, MirrorMonitoringSettings settings,
    IMonitoringIncidentClient client, ILogger<MirrorMonitoringService> logger) : IBackgroundJob
{
    public const string Source = "GitMirrorServer.RepositoryMonitoring";
    public string Name => "Mirror AlertCenter Monitoring";
    public string Description => "Synchronizes consecutive failed mirror rounds and confirmed recovery with AlertCenter.";

    public async Task ExecuteAsync()
    {
        await monitor.SyncGate.WaitAsync();
        try
        {
            var config = await settings.LoadAsync();
            if (!config.Enabled) return;
            config.Validate();
            client.Configure(config);
            var snapshots = monitor.Snapshot();
            if (!snapshots.Any(state => state.PendingRecovery != null || config.Rules.Any(rule =>
                    rule.IsEnabled && state.Failures >= rule.FailureCount))) return;
            // Complete all pages before the first write. Unknown repositories are never treated as recovered.
            var incidents = await LoadIncidentsAsync();
            var owned = incidents.Where(item => item.Metadata.Value<string>("source") == Source)
                .ToLookup(item => item.Metadata.Value<string>("repositoryKey"));
            foreach (var snapshot in snapshots)
            {
                try
                {
                    var active = owned[snapshot.Key].OrderBy(item => item.ReceivedAtUtc).ThenBy(item => item.IncidentId).ToList();
                    if (active.Any(item => item.ReportId == snapshot.NextReportId))
                        monitor.AdvanceReceipt(snapshot.Key, snapshot.NextReportId);
                    if (config.AutoMitigate && snapshot.PendingRecovery is { } recovery)
                    {
                        foreach (var incident in active)
                            await MitigateAsync(incident, $"A completed mirror round succeeded at {snapshot.LastSuccess:O}. Internal retries are not separate rounds. Final resolution remains manual.");
                        monitor.ConfirmRecovery(snapshot.Key, recovery);
                        active.Clear();
                    }
                    var rule = config.Rules.Where(rule => rule.IsEnabled && snapshot.Failures >= rule.FailureCount)
                        .OrderBy(rule => rule.Severity, StringComparer.Ordinal).FirstOrDefault();
                    if (rule == null) continue;
                    if (active.Count == 0)
                    {
                        var current = monitor.Snapshot(snapshot.Key);
                        if (current == null || current.JobId != snapshot.JobId) continue;
                        var title = $"{snapshot.Repository}: mirror failed {snapshot.Failures} consecutive rounds";
                        await client.CreateAsync(new CreateIncidentRequest
                        {
                            ReportId = current.NextReportId,
                            Title = title[..Math.Min(title.Length, 200)], Level = rule.Severity,
                            OccurredAtUtc = snapshot.FailureSince ?? snapshot.LastObserved,
                            Content = Describe(snapshot, rule, config),
                            Metadata = new JObject { ["source"] = Source, ["repositoryKey"] = snapshot.Key,
                                ["configurationId"] = snapshot.ConfigurationId.ToString(), ["repository"] = snapshot.Repository }
                        });
                        monitor.AdvanceReceipt(snapshot.Key, current.NextReportId);
                    }
                    else
                    {
                        var incident = active[0];
                        if (string.CompareOrdinal(rule.Severity, incident.Level) < 0)
                            await client.ChangeSeverityAsync(incident.IncidentId, new ChangeIncidentSeverityRequest
                            {
                                Version = incident.Version, Level = rule.Severity, Reason = Describe(snapshot, rule, config)
                            });
                        foreach (var duplicate in active.Skip(1))
                            await MitigateAsync(duplicate, $"Duplicate repository incident. Continue response in incident {incident.IncidentId}.");
                    }
                }
                catch (Exception) { logger.LogError("Mirror incident synchronization failed for {RepositoryKey}; retained state for retry", snapshot.Key); }
            }
        }
        catch (Exception) { logger.LogError("Mirror monitoring pass failed; retained state for retry. Check connection settings and API key scopes."); }
        finally { monitor.SyncGate.Release(); }
    }

    private async Task MitigateAsync(IncidentDetails incident, string reason) =>
        await client.MitigateAsync(incident.IncidentId, new MitigateIncidentRequest { Version = incident.Version, Reason = reason });

    private async Task<List<IncidentDetails>> LoadIncidentsAsync()
    {
        var results = new List<IncidentDetails>();
        int? total = null;
        for (var page = 1; ; page++)
        {
            var response = await client.QueryAsync(new QueryIncidentsRequest
            {
                ActiveOnly = true, MetadataKey = "source", MetadataValue = Source, Page = page, PageSize = 100
            });
            total ??= response.TotalCount;
            if (response.TotalCount != total || response.Page != page || response.PageSize != 100)
                throw new InvalidOperationException("Incident pagination changed; retry from a fresh snapshot.");
            results.AddRange(response.Items);
            if (results.Count >= total)
            {
                if (results.Count != total || results.Select(item => item.IncidentId).Distinct().Count() != results.Count)
                    throw new InvalidOperationException("Incident pages overlap or have inconsistent totals.");
                return results;
            }
            if (response.Items.Count == 0) throw new InvalidOperationException("Incident listing is incomplete.");
        }
    }

    private static string Describe(MirrorSnapshot state, MirrorMonitoringRule rule, MonitoringConfiguration config)
    {
        var description = $"Repository: {state.Repository}\nSource: {state.Source}\nTarget: {state.Target}\n\n" +
            $"Mirroring failed in {state.Failures} consecutive completed rounds since {state.FailureSince:O}. " +
            $"The configured rule triggers {rule.Severity} at {rule.FailureCount} failed rounds. " +
            $"Each scheduled or manually started mirror run counts once per repository, after all {RepositoryMirrorRetryRunner.MaximumAttempts} internal attempts. " +
            "A half-hour schedule therefore needs separate failed runs, not three individual retry attempts.\n" +
            $"Last result (UTC): {state.LastObserved:O}. Last observed successful round (UTC): {state.LastSuccess?.ToString("O") ?? "not observed in this process"}.\n\n" +
            $"Evidence: {state.FailureSummary}\n\n" +
            "Impact: The target copy may be stale; this does not establish that the source repository or its deployed application is down.\n" +
            "Next steps: Inspect the repository execution log, check source-read and target-write credentials, Git server connectivity, target branch protection and local disk space. " +
            "After fixing the cause, run the mirror again and verify that all branches and tags reach the target. Do not paste tokens or authenticated Git URLs into incident comments.\n" +
            $"Execution log: {config.PublicBaseUrl.TrimEnd('/')}/History/Details/{state.JobId}\n" +
            (config.AutoMitigate ? "Recovery: A completed successful mirror round resets the counter and automatically mitigates this incident; final resolution is manual."
                : "Recovery: A successful round resets the counter. Automatic mitigation is disabled; mitigate and resolve manually.");
        return description[..Math.Min(description.Length, 4000)];
    }
}
