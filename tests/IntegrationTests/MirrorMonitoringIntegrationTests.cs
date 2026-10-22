using System.Net;
using System.Text.RegularExpressions;
using Aiursoft.AlertCenter.SDK.Models;
using Aiursoft.GitMirrorServer.Configuration;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.GitMirrorServer.Services.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using static Aiursoft.GitMirrorServer.Tests.MirrorMonitoringTests;

namespace Aiursoft.GitMirrorServer.Tests.IntegrationTests;

[TestClass]
public class MirrorMonitoringIntegrationTests : TestBase
{
    [TestInitialize]
    public override async Task CreateServer()
    {
        await base.CreateServer();
        // The existing in-memory provider can retain settings across test hosts.
        var settings = GetService<GlobalSettingsService>();
        foreach (var definition in SettingsMap.Definitions.Where(item => item.Key.StartsWith("AlertCenter_", StringComparison.Ordinal)))
            await settings.UpdateSettingAsync(definition.Key, definition.DefaultValue);
    }

    private async Task<(MirrorMonitoringService Service, MirrorMonitor Monitor, RecordingIncidents Client, GlobalSettingsService Settings)> Setup()
    {
        var settings = GetService<GlobalSettingsService>();
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterEnabled, "True");
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterApiKey, "test-application-key");
        var monitor = new MirrorMonitor();
        var client = new RecordingIncidents();
        return (new MirrorMonitoringService(monitor, new MirrorMonitoringSettings(settings), client,
            NullLogger<MirrorMonitoringService>.Instance), monitor, client, settings);
    }

    [TestMethod]
    public async Task ThresholdsDedupEscalationAndRecovery_UseThreeAndNineCompletedRounds()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var round = 1; round <= 9; round++)
        {
            monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
            await service.ExecuteAsync();
            Assert.AreEqual(round >= 3 ? 1 : 0, client.All.Count);
            if (round >= 3) Assert.AreEqual(round >= 9 ? "sev2" : "sev3", client.All[0].Level);
        }
        Assert.AreEqual(1, client.Escalations);
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.CreateCalls);
        monitor.Observe(config, Result(true), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual("mitigated", client.All.Single().Status);
        Assert.AreEqual(1, client.Mitigations);
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.Mitigations);
        var content = client.All.Single().Content!;
        Assert.Contains("consecutive completed rounds", content);
        Assert.Contains("Authentication or permission failure", content, "Precise errors must stay behind existing permissions.");
        Assert.Contains("Impact:", content);
        Assert.Contains("Next steps:", content);
        Assert.Contains("/History/Details/", content);
        Assert.DoesNotContain(config.TargetToken, content);
    }

    [TestMethod]
    public async Task ManualClosureWhileFailurePersists_CreatesANewIncident()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        var original = client.All.Single();
        original.Status = "resolved";
        await service.ExecuteAsync();
        Assert.AreEqual(2, client.All.Count);
        Assert.AreNotEqual(original.ReportId, client.All.Last().ReportId);
        Assert.AreNotEqual(original.IncidentId, client.All.Last().IncidentId);
        await service.ExecuteAsync();
        Assert.AreEqual(2, client.All.Count);
    }

    [TestMethod]
    public async Task LostCreateResponse_ReconcilesReceiptBeforeLaterManualClosure()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        client.ThrowAfterCreate = true;
        await service.ExecuteAsync();
        client.ThrowAfterCreate = false;
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.All.Count);
        client.All.Single().Status = "resolved";
        await service.ExecuteAsync();
        Assert.AreEqual(2, client.All.Count);
        Assert.AreEqual(2, client.All.Select(item => item.ReportId).Distinct().Count());
    }

    [TestMethod]
    public async Task GreenThenFailureBeforeSynchronization_StillMitigatesOldEpisode()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        monitor.Observe(config, Result(true), DateTimeOffset.UtcNow);
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual(2, client.All.Count);
        Assert.AreEqual("mitigated", client.All[0].Status);
        Assert.AreEqual("new", client.All[1].Status);
    }

    [TestMethod]
    public async Task RecoveryFailureIsRetried_WithoutCreatingAnotherIncident()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        monitor.Observe(config, Result(true), DateTimeOffset.UtcNow);
        client.FailMitigate = true;
        await service.ExecuteAsync();
        Assert.IsNotNull(monitor.Snapshot().Single().PendingRecovery);
        Assert.AreEqual("new", client.All.Single().Status);
        client.FailMitigate = false;
        await service.ExecuteAsync();
        Assert.AreEqual("mitigated", client.All.Single().Status);
        Assert.IsNull(monitor.Snapshot().Single().PendingRecovery);
    }

    [TestMethod]
    public async Task RestartUnknownAndDisabledRecovery_DoNotCloseOldIncidents()
    {
        var (service, monitor, client, settings) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        var restarted = new MirrorMonitor();
        var afterRestart = new MirrorMonitoringService(restarted, new MirrorMonitoringSettings(settings), client,
            NullLogger<MirrorMonitoringService>.Instance);
        await afterRestart.ExecuteAsync();
        Assert.AreEqual(0, client.Mitigations);
        restarted.Observe(config, Result(), DateTimeOffset.UtcNow);
        await afterRestart.ExecuteAsync();
        Assert.AreEqual(0, client.Mitigations);
        Assert.AreEqual(1, client.All.Count);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterAutoMitigate, "False");
        restarted.Observe(config, Result(true), DateTimeOffset.UtcNow);
        await afterRestart.ExecuteAsync();
        Assert.AreEqual(0, client.Mitigations);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterAutoMitigate, "True");
        await afterRestart.ExecuteAsync();
        Assert.AreEqual(1, client.Mitigations);
    }

    [TestMethod]
    public async Task PaginationFailure_PreventsAllWrites()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        client.FailSecondPage = true;
        await service.ExecuteAsync();
        Assert.AreEqual(2, client.Queries);
        Assert.AreEqual(0, client.CreateCalls);
        Assert.AreEqual(0, client.Mitigations);
        Assert.AreEqual(3, monitor.Snapshot().Single().Failures);
        client.FailSecondPage = false;
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.All.Count);
    }

    [TestMethod]
    public async Task ManyRepositoriesUseOneQueryPerPass_AndDisabledIntegrationMakesNone()
    {
        var (service, monitor, client, settings) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 100; i++)
            for (var round = 0; round < 3; round++)
                monitor.Observe(config, Result(name: $"repo-{i}"), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.Queries);
        Assert.AreEqual(100, client.CreateCalls);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterEnabled, "False");
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.Queries);
    }

    [TestMethod]
    public async Task RulesChangedOrRemoved_DoNotInventRecovery_OrDowngrade()
    {
        var (service, monitor, client, settings) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 9; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual("sev2", client.All.Single().Level);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterRules, "[]");
        await settings.SeedSettingsAsync();
        await service.ExecuteAsync();
        Assert.AreEqual("[]", await settings.GetSettingValueAsync(SettingsMap.AlertCenterRules));
        Assert.AreEqual("new", client.All.Single().Status);
        Assert.AreEqual(0, client.Mitigations);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterRules, "[{\"FailureCount\":1,\"Severity\":\"sev4\"}]");
        await service.ExecuteAsync();
        Assert.AreEqual("sev2", client.All.Single().Level);
        Assert.AreEqual(0, client.Escalations);
    }

    [TestMethod]
    public async Task PagePermissionCsrfAndRuleEditing_AreEnforced()
    {
        var anonymous = await Http.GetAsync("/AlertMonitoring");
        AssertRedirect(anonymous, "/Account/Login", exact: false);
        await RegisterAndLoginAsync();
        var denied = await Http.GetAsync("/AlertMonitoring");
        Assert.AreEqual(HttpStatusCode.Found, denied.StatusCode);
        Assert.Contains("/Error/Code403", denied.Headers.Location!.ToString());
        await PostForm("/Account/LogOff", []);
        await LoginAsAdmin();
        var page = await Http.GetStringAsync("/AlertMonitoring");
        Assert.Contains("four internal attempts", page);
        Assert.Contains("value=\"sev2\"", page);
        var version = Regex.Match(page, "name=\"version\" value=\"([^\"]+)\"").Groups[1].Value;
        var form = new Dictionary<string, string> { ["version"] = version, ["failureCount"] = "5", ["severity"] = "sev1", ["isEnabled"] = "true" };
        Assert.AreEqual(HttpStatusCode.BadRequest, (await PostForm("/AlertMonitoring/SaveRule", form, includeToken: false)).StatusCode);
        var saved = await PostForm("/AlertMonitoring/SaveRule", form, tokenUrl: "/AlertMonitoring");
        AssertRedirect(saved, "/AlertMonitoring");
        Assert.AreEqual(HttpStatusCode.Conflict, (await PostForm("/AlertMonitoring/SaveRule", form, tokenUrl: "/AlertMonitoring")).StatusCode);
        var json = await GetService<GlobalSettingsService>().GetSettingValueAsync(SettingsMap.AlertCenterRules);
        var rules = MirrorMonitoringRule.Parse(json);
        Assert.AreEqual(3, rules.Count);
        Assert.AreEqual(5, rules.Last().FailureCount);
        Assert.AreEqual("sev1", rules.Last().Severity);
    }

    [TestMethod]
    public async Task SettingsOverridesAndSecretStorage_KeepTheKeyOutOfBothPages()
    {
        var (_, _, _, settings) = await Setup();
        await LoginAsAdmin();
        var db = GetService<GitMirrorServerDbContext>();
        Assert.DoesNotContain("test-application-key", (await db.GlobalSettings.AsNoTracking().SingleAsync(s => s.Key == SettingsMap.AlertCenterApiKey)).Value!);
        Assert.DoesNotContain("test-application-key", await Http.GetStringAsync("/GlobalSettings"));
        Assert.DoesNotContain("test-application-key", await Http.GetStringAsync("/AlertMonitoring"));
        var config = GetService<IConfiguration>();
        config[$"GlobalSettings:{SettingsMap.AlertCenterRules}"] = "[]";
        try
        {
            Assert.IsTrue(settings.IsOverriddenByConfig(SettingsMap.AlertCenterRules));
            Assert.Contains("Rules are overridden", await Http.GetStringAsync("/AlertMonitoring"));
        }
        finally { config[$"GlobalSettings:{SettingsMap.AlertCenterRules}"] = null; }
    }

    [TestMethod]
    public async Task SuccessWhileCreateIsInFlight_IsRetainedForTheNextPass()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        client.AfterCreate = () => monitor.Observe(config, Result(true), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual("new", client.All.Single().Status);
        Assert.IsNotNull(monitor.Snapshot().Single().PendingRecovery);
        await service.ExecuteAsync();
        Assert.AreEqual("mitigated", client.All.Single().Status);
    }

    [TestMethod]
    public async Task DuplicateCleanupDoesNotTouchOtherIntegrationsOrUnknownRepositories()
    {
        var (service, monitor, client, _) = await Setup();
        var config = CreateConfiguration();
        for (var i = 0; i < 3; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        var original = client.All.Single();
        client.All.Add(new IncidentDetails { IncidentId = Guid.NewGuid(), Version = Guid.NewGuid(), Status = "new",
            Level = "sev3", Metadata = (Newtonsoft.Json.Linq.JObject)original.Metadata.DeepClone(), ReceivedAtUtc = DateTimeOffset.UtcNow });
        client.All.Add(new IncidentDetails { IncidentId = Guid.NewGuid(), Status = "new", Level = "sev3",
            Metadata = new() { ["source"] = "Another.Integration", ["repositoryKey"] = monitor.Snapshot().Single().Key } });
        client.All.Add(new IncidentDetails { IncidentId = Guid.NewGuid(), Status = "new", Level = "sev3",
            Metadata = new() { ["source"] = MirrorMonitoringService.Source, ["repositoryKey"] = "not-observed-since-restart" } });
        await service.ExecuteAsync();
        Assert.AreEqual(1, client.Mitigations);
        Assert.AreEqual("new", client.All[0].Status);
        Assert.AreEqual("mitigated", client.All[1].Status);
        Assert.AreEqual("new", client.All[2].Status);
        Assert.AreEqual("new", client.All[3].Status);
    }

    [TestMethod]
    public async Task ConnectionSnapshotUsesUpdatedKeyOnTheNextPass()
    {
        var (service, monitor, client, settings) = await Setup();
        monitor.Observe(CreateConfiguration(), Result(true), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual("test-application-key", client.Configuration!.ApiKey);
        await settings.UpdateSettingAsync(SettingsMap.AlertCenterApiKey, "new-key");
        monitor.Observe(CreateConfiguration(), Result(true), DateTimeOffset.UtcNow);
        await service.ExecuteAsync();
        Assert.AreEqual("new-key", client.Configuration.ApiKey);
    }

    private sealed class RecordingIncidents : IMonitoringIncidentClient
    {
        public List<IncidentDetails> All { get; } = [];
        public int Queries { get; private set; }
        public int CreateCalls { get; private set; }
        public int Escalations { get; private set; }
        public int Mitigations { get; private set; }
        public Action? AfterCreate { get; set; }
        public MonitoringConfiguration? Configuration { get; private set; }
        public bool ThrowAfterCreate { get; set; }
        public bool FailSecondPage { get; set; }
        public bool FailMitigate { get; set; }
        public void Configure(MonitoringConfiguration configuration)
        {
            configuration.Validate();
            Configuration = configuration;
        }
        public Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request)
        {
            Queries++;
            Assert.IsTrue(request.ActiveOnly);
            Assert.AreEqual("source", request.MetadataKey);
            Assert.AreEqual(MirrorMonitoringService.Source, request.MetadataValue);
            if (FailSecondPage && request.Page == 2) throw new HttpRequestException("second page failed");
            if (FailSecondPage) return Task.FromResult(new QueryIncidentsResponse
            {
                Items = [new IncidentDetails { IncidentId = Guid.NewGuid(), Metadata = new() }],
                TotalCount = 2, Page = 1, PageSize = 100
            });
            var active = All.Where(i => i.Status is not ("mitigated" or "resolved")).ToList();
            return Task.FromResult(new QueryIncidentsResponse { Items = active, TotalCount = active.Count, Page = request.Page, PageSize = request.PageSize });
        }
        public Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request)
        {
            CreateCalls++;
            if (All.All(item => item.ReportId != request.ReportId))
                All.Add(new IncidentDetails { IncidentId = Guid.NewGuid(), Version = Guid.NewGuid(), ReportId = request.ReportId,
                    Title = request.Title, Content = request.Content, Level = request.Level, Status = "new", Metadata = request.Metadata!, ReceivedAtUtc = DateTimeOffset.UtcNow });
            AfterCreate?.Invoke();
            if (ThrowAfterCreate) throw new HttpRequestException("Lost response");
            return Task.FromResult(new CreateIncidentResponse());
        }
        public Task<ChangeIncidentSeverityResponse> ChangeSeverityAsync(Guid id, ChangeIncidentSeverityRequest request)
        {
            var incident = All.Single(i => i.IncidentId == id); Assert.AreEqual(incident.Version, request.Version);
            incident.Level = request.Level; incident.Version = Guid.NewGuid(); Escalations++;
            return Task.FromResult(new ChangeIncidentSeverityResponse());
        }
        public Task<MitigateIncidentResponse> MitigateAsync(Guid id, MitigateIncidentRequest request)
        {
            if (FailMitigate) throw new HttpRequestException("mitigate failed");
            var incident = All.Single(i => i.IncidentId == id); Assert.AreEqual(incident.Version, request.Version);
            incident.Status = "mitigated"; incident.Version = Guid.NewGuid(); Mitigations++;
            return Task.FromResult(new MitigateIncidentResponse());
        }
    }
}
