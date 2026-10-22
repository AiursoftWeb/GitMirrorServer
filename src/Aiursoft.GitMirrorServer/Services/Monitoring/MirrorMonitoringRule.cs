using System.Text.Json;

namespace Aiursoft.GitMirrorServer.Services.Monitoring;

public sealed class MirrorMonitoringRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int FailureCount { get; set; } = 3;
    public string Severity { get; set; } = "sev3";
    public bool IsEnabled { get; set; } = true;

    public static string DefaultJson { get; } = JsonSerializer.Serialize(new[]
    {
        new MirrorMonitoringRule { FailureCount = 3 },
        new MirrorMonitoringRule { FailureCount = 9, Severity = "sev2" },
    });

    public static List<MirrorMonitoringRule> Parse(string json)
    {
        var rules = JsonSerializer.Deserialize<List<MirrorMonitoringRule?>>(json)
                    ?? throw new InvalidOperationException("Monitoring rules must be a JSON array.");
        if (rules.Count > 20 || rules.Any(rule => rule == null || !rule.IsValid()) ||
            rules.Select(rule => rule!.Id).Distinct().Count() != rules.Count)
            throw new InvalidOperationException("Use at most 20 unique rules, positive consecutive failed-round thresholds and sev0 to sev4.");
        return rules.Select(rule => rule!).ToList();
    }

    public bool IsValid() => Id != Guid.Empty && FailureCount is >= 1 and <= 1000000 && Severity is "sev0" or "sev1" or "sev2" or "sev3" or "sev4";
}
