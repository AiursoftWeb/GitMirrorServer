using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class RepositoryMirrorRetryRunnerTests
{
    [TestMethod]
    public async Task RunAsync_FirstAttemptSucceeds_RecordsOneSuccessWithoutRetry()
    {
        var state = CreateState();
        var attempts = 0;
        var cleanupCount = 0;
        var delays = new List<TimeSpan>();
        var logs = new List<string>();

        await RepositoryMirrorRetryRunner.RunAsync(
            "example",
            state.Job,
            state.Repository,
            () =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            () => cleanupCount++,
            logs.Add,
            (_, _) => { },
            delay =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        Assert.AreEqual(1, attempts);
        Assert.AreEqual(0, cleanupCount);
        Assert.AreEqual(0, delays.Count);
        Assert.AreEqual(1, state.Job.SuccessCount);
        Assert.AreEqual(0, state.Job.FailureCount);
        Assert.IsTrue(state.Job.IsSuccess);
        Assert.IsTrue(state.Repository.IsSuccess);
        Assert.IsNull(state.Repository.ErrorMessage);
        Assert.IsTrue(logs.Any(log => log.Contains("Attempt 1/4", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task RunAsync_TransientFailures_StopsAfterSuccessfulAttempt()
    {
        var state = CreateState();
        var attempts = 0;
        var cleanupCount = 0;
        var delays = new List<TimeSpan>();
        var logs = new List<string>();

        await RepositoryMirrorRetryRunner.RunAsync(
            "example",
            state.Job,
            state.Repository,
            () =>
            {
                attempts++;
                return attempts < 3
                    ? Task.FromException(new InvalidOperationException($"failure {attempts}"))
                    : Task.CompletedTask;
            },
            () => cleanupCount++,
            logs.Add,
            (_, _) => { },
            delay =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        Assert.AreEqual(3, attempts);
        Assert.AreEqual(2, cleanupCount);
        CollectionAssert.AreEqual(
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) },
            delays);
        Assert.AreEqual(1, state.Job.SuccessCount);
        Assert.AreEqual(0, state.Job.FailureCount);
        Assert.IsTrue(state.Job.IsSuccess);
        Assert.IsTrue(state.Repository.IsSuccess);
        Assert.IsTrue(logs.Any(log => log.Contains("Attempt 2/4 failed", StringComparison.Ordinal)));
        Assert.IsTrue(logs.Any(log => log.Contains("Attempt 3/4", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task RunAsync_AllAttemptsFail_RecordsOneFailureAfterFourAttempts()
    {
        var state = CreateState();
        var attempts = 0;
        var cleanupCount = 0;
        var loggedExceptions = new List<(int Attempt, Exception Exception)>();
        var delays = new List<TimeSpan>();
        var logs = new List<string>();

        await RepositoryMirrorRetryRunner.RunAsync(
            "example",
            state.Job,
            state.Repository,
            () =>
            {
                attempts++;
                return Task.FromException(new InvalidOperationException($"failure {attempts}"));
            },
            () => cleanupCount++,
            logs.Add,
            (attempt, exception) => loggedExceptions.Add((attempt, exception)),
            delay =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        Assert.AreEqual(4, attempts);
        Assert.AreEqual(4, cleanupCount);
        Assert.AreEqual(4, loggedExceptions.Count);
        CollectionAssert.AreEqual(
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) },
            delays);
        Assert.AreEqual(0, state.Job.SuccessCount);
        Assert.AreEqual(1, state.Job.FailureCount);
        Assert.IsFalse(state.Job.IsSuccess);
        Assert.IsFalse(state.Repository.IsSuccess);
        StringAssert.Contains(state.Repository.ErrorMessage, "after 4 attempts");
        StringAssert.Contains(state.Repository.ErrorMessage, "failure 4");

        for (var attempt = 1; attempt <= RepositoryMirrorRetryRunner.MaximumAttempts; attempt++)
        {
            Assert.IsTrue(logs.Any(log => log.Contains($"Attempt {attempt}/4", StringComparison.Ordinal)));
        }
    }

    private static (MirrorJobExecution Job, MirrorRepoExecution Repository) CreateState()
    {
        var job = new MirrorJobExecution
        {
            Id = Guid.NewGuid(),
            IsSuccess = true
        };
        var repository = new MirrorRepoExecution
        {
            Id = Guid.NewGuid(),
            JobExecutionId = job.Id,
            FromOrg = "source",
            RepoName = "example",
            TargetOrg = "target"
        };

        return (job, repository);
    }
}
