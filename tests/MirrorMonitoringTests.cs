using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.GitMirrorServer.Services.Monitoring;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class MirrorMonitoringTests
{
    internal static MirrorConfiguration CreateConfiguration() => new()
    {
        Id = Guid.NewGuid(), FromType = "GitLab", FromServer = "https://source.test", FromOrgName = "team",
        OrgOrUser = "Org", TargetType = "GitHub", TargetServer = "https://target.test", TargetOrgName = "backup",
        TargetToken = "target-token", FromToken = "source-token"
    };

    internal static MirrorRepoExecution Result(bool success = false, string name = "example") => new()
    {
        Id = Guid.NewGuid(), JobExecutionId = Guid.NewGuid(), FromOrg = "team", TargetOrg = "backup",
        RepoName = name, IsSuccess = success, ErrorMessage = success ? null : "403 authentication failed"
    };

    [TestMethod]
    public async Task FourFailedAttempts_AreOneFailedRound_AndSuccessAfterRetriesResets()
    {
        var monitor = new MirrorMonitor();
        var config = CreateConfiguration();
        var job = new MirrorJobExecution { Id = Guid.NewGuid(), IsSuccess = true };
        var result = Result();
        await RepositoryMirrorRetryRunner.RunAsync("example", job, result,
            () => Task.FromException(new InvalidOperationException("unavailable")), () => { }, _ => { }, (_, _) => { }, _ => Task.CompletedTask);
        monitor.Observe(config, result, DateTimeOffset.UtcNow);
        monitor.Observe(config, result, DateTimeOffset.UtcNow);
        Assert.AreEqual(1, monitor.Snapshot().Single().Failures);
        var recovered = Result();
        var attempts = 0;
        await RepositoryMirrorRetryRunner.RunAsync("example", job, recovered,
            () => ++attempts < 4 ? Task.FromException(new InvalidOperationException("transient")) : Task.CompletedTask,
            () => { }, _ => { }, (_, _) => { }, _ => Task.CompletedTask);
        monitor.Observe(config, recovered, DateTimeOffset.UtcNow);
        Assert.AreEqual(4, attempts);
        Assert.AreEqual(0, monitor.Snapshot().Single().Failures);
        Assert.IsNotNull(monitor.Snapshot().Single().PendingRecovery);
    }

    [TestMethod]
    public async Task Cancellation_DoesNotProduceACompletedRound()
    {
        var job = new MirrorJobExecution { Id = Guid.NewGuid(), IsSuccess = true };
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => RepositoryMirrorRetryRunner.RunAsync(
            "example", job, Result(), () => Task.FromException(new OperationCanceledException()),
            () => Assert.Fail("Canceled work must not delete the workspace."), _ => { }, (_, _) => { }));
        Assert.AreEqual(0, job.FailureCount);
        Assert.AreEqual(0, job.SuccessCount);
    }

    [TestMethod]
    public void RepositoriesAndRoutesAreIndependent_AndCredentialsAreNotCopied()
    {
        var monitor = new MirrorMonitor();
        var config = CreateConfiguration();
        config.FromServer = "https://username:password@source.test/path?token=secret";
        config.TargetServer = "https://username:password@target.test/path#secret";
        for (var i = 0; i < 9; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        monitor.Observe(config, Result(true, "other"), DateTimeOffset.UtcNow);
        var anotherRoute = CreateConfiguration();
        monitor.Observe(anotherRoute, Result(), DateTimeOffset.UtcNow);
        Assert.AreEqual(3, monitor.Snapshot().Count);
        var state = monitor.Snapshot().Single(item => item.ConfigurationId == config.Id && item.Repository == "example");
        Assert.AreEqual(9, state.Failures);
        Assert.AreEqual("https://source.test/path/team", state.Source);
        Assert.AreEqual("https://target.test/path/backup", state.Target);
        Assert.DoesNotContain("password", state.ToString());
        Assert.Contains("Authentication", state.FailureSummary);
    }

    [TestMethod]
    public void FixedStatePerRepository_RestartDoesNotInventSuccess()
    {
        var monitor = new MirrorMonitor();
        var config = CreateConfiguration();
        for (var i = 0; i < 10000; i++) monitor.Observe(config, Result(), DateTimeOffset.UtcNow);
        Assert.AreEqual(1, monitor.Snapshot().Count);
        Assert.AreEqual(10000, monitor.Snapshot().Single().Failures);
        Assert.IsNull(monitor.Snapshot().Single().LastSuccess);
        Assert.IsEmpty(new MirrorMonitor().Snapshot());
    }

    [TestMethod]
    public void ConfirmingOldRecoveryDoesNotForgetANewerSuccess()
    {
        var monitor = new MirrorMonitor();
        var config = CreateConfiguration();
        monitor.Observe(config, Result(true), DateTimeOffset.UtcNow);
        var old = monitor.Snapshot().Single();
        monitor.Observe(config, Result(true), DateTimeOffset.UtcNow);
        monitor.ConfirmRecovery(old.Key, old.PendingRecovery!.Value);
        Assert.IsNotNull(monitor.Snapshot().Single().PendingRecovery);
    }

    [TestMethod]
    public void DefaultRulesAndValidation_UseInclusiveThresholdsAndKeepEmptyRulesEmpty()
    {
        var rules = MirrorMonitoringRule.Parse(MirrorMonitoringRule.DefaultJson);
        CollectionAssert.AreEqual(new[] { 1, 3, 9 }, rules.Select(item => item.FailureCount).ToArray());
        CollectionAssert.AreEqual(new[] { "sev4", "sev3", "sev2" }, rules.Select(item => item.Severity).ToArray());
        CollectionAssert.AreEqual(new[] { true, false, false }, rules.Select(item => item.ResolveOnRecovery).ToArray());
        Assert.IsEmpty(MirrorMonitoringRule.Parse("[]"));
        Assert.ThrowsExactly<InvalidOperationException>(() => MirrorMonitoringRule.Parse("[null]"));
        Assert.ThrowsExactly<InvalidOperationException>(() => MirrorMonitoringRule.Parse("[{\"FailureCount\":0}]"));
        Assert.ThrowsExactly<InvalidOperationException>(() => MirrorMonitoringRule.Parse("[{\"Severity\":\"sev9\"}]"));
    }
}
