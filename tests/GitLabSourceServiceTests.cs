using System.Text;
using Aiursoft.CSTools.Services;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.Canon;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class GitLabSourceServiceTests
{
    private const string Source = "https://gitlab.example.com/source/project.git";

    [TestMethod]
    public async Task UpdateAsync_CloneAndCachedFetchUseCurrentCredentialWithoutPersistingIt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gitmirror-auth-{Guid.NewGuid():N}");
        var credentials = new List<string>();
        var commands = new List<string>();
        var runner = new StubCommandService((arguments, environment) =>
        {
            Assert.IsFalse(arguments.Contains("first-token", StringComparison.Ordinal));
            Assert.IsFalse(arguments.Contains("rotated-token", StringComparison.Ordinal));
            Assert.AreEqual($"http.{Source}.extraHeader", environment["GIT_CONFIG_KEY_0"]);
            Assert.AreEqual("credential.helper", environment["GIT_CONFIG_KEY_1"]);
            Assert.AreEqual(string.Empty, environment["GIT_CONFIG_VALUE_1"]);
            Assert.AreEqual("false", environment["GIT_CONFIG_VALUE_2"]);
            credentials.Add(environment["GIT_CONFIG_VALUE_0"]!);
            commands.Add(arguments);
            return (0, arguments == "remote get-url origin" ? Source + "\n" : string.Empty, string.Empty);
        });
        var service = CreateService(runner);
        try
        {
            await service.UpdateAsync(path, Source, "first-token");
            Directory.CreateDirectory(Path.Combine(path, ".git"));
            await service.UpdateAsync(path, Source, "rotated-token");

            Assert.IsTrue(commands.First().StartsWith("clone -- ", StringComparison.Ordinal));
            Assert.AreEqual("fetch origin", commands.Last());
            Assert.AreEqual("Authorization: " + Basic("first-token"), credentials.First());
            Assert.AreEqual("Authorization: " + Basic("rotated-token"), credentials.Last());
            Assert.AreEqual(0, Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length);
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }

    [TestMethod]
    public async Task UpdateAsync_AuthenticationFailureDoesNotExposeOrRetryWithoutCredential()
    {
        const string token = "source-secret";
        var requestCount = 0;
        var runner = new StubCommandService((_, _) =>
        {
            requestCount++;
            return (128, string.Empty, $"Authentication failed: {token} {Basic(token)}");
        });
        var service = CreateService(runner);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.UpdateAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), Source, token));

        Assert.IsFalse(exception.ToString().Contains(token, StringComparison.Ordinal));
        Assert.IsFalse(exception.ToString().Contains(Basic(token), StringComparison.Ordinal));
        Assert.IsTrue(exception.Message.Contains("exit 128", StringComparison.Ordinal));
        Assert.AreEqual(1, requestCount);
    }

    [TestMethod]
    [DataRow("http://gitlab.example.com/source/project.git")]
    [DataRow("https://user:password@gitlab.example.com/source/project.git")]
    public async Task UpdateAsync_RejectsInsecureCredentialTransport(string source)
    {
        var runner = new StubCommandService((_, _) => throw new AssertFailedException("Git must not run."));
        var service = CreateService(runner);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.UpdateAsync(Path.GetTempPath(), source, "source-secret"));
    }

    [TestMethod]
    public async Task UpdateBranchesAsync_AuthenticatesEveryNestedFetchWithTheCurrentCredential()
    {
        var fetches = 0;
        var commands = new List<string>();
        var runner = new StubCommandService((arguments, environment) =>
        {
            Assert.AreEqual("Authorization: " + Basic("branch-token"), environment["GIT_CONFIG_VALUE_0"]);
            Assert.AreEqual($"http.{Source}.extraHeader", environment["GIT_CONFIG_KEY_0"]);
            Assert.IsFalse(arguments.Contains("branch-token", StringComparison.Ordinal));
            commands.Add(arguments);
            if (arguments.EndsWith("fetch --verbose", StringComparison.Ordinal)) fetches++;
            return (0, arguments.EndsWith("branch -r", StringComparison.Ordinal)
                ? "origin/master\norigin/feature\n"
                : arguments.Contains("branch --format", StringComparison.Ordinal) ? "master\n" : string.Empty,
                string.Empty);
        });
        await CreateService(runner).UpdateBranchesAsync(Path.GetTempPath(), Source, "branch-token");

        Assert.AreEqual(2, fetches, "Both branch discovery passes must authenticate their fetch.");
        Assert.IsTrue(commands.Any(command => command.Contains("checkout -b feature", StringComparison.Ordinal)));
        Assert.IsTrue(commands.Any(command => command.Contains("reset --hard origin/master", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task UpdateBranchesAsync_AuthenticationFailureDoesNotExposeCredential()
    {
        const string token = "branch-secret";
        var runner = new StubCommandService((_, environment) =>
        {
            Assert.AreEqual("Authorization: " + Basic(token), environment["GIT_CONFIG_VALUE_0"]);
            return (128, string.Empty, $"Authentication failed: {token} {Basic(token)}");
        });
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(runner).UpdateBranchesAsync(Path.GetTempPath(), Source, token));
        Assert.IsFalse(exception.ToString().Contains(token, StringComparison.Ordinal));
        Assert.IsFalse(exception.ToString().Contains(Basic(token), StringComparison.Ordinal));
    }

    private static GitLabSourceService CreateService(CommandService runner) => new(runner,
        new WorkspaceManager(NullLogger<WorkspaceManager>.Instance,
            new RetryEngine(NullLogger<RetryEngine>.Instance),
            new GitCommandRunner(runner, NullLogger<GitCommandRunner>.Instance)));

    private static string Basic(string token) =>
        "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"oauth2:{token}"));

    private sealed class StubCommandService(
        Func<string, IDictionary<string, string?>, (int code, string output, string error)> run) : CommandService
    {
        public override Task<(int code, string output, string error)> RunCommandAsync(
            string bin, string arg, string path, TimeSpan? timeout = null, bool killTimeoutProcess = true,
            IDictionary<string, string?>? environmentVariables = null)
        {
            Assert.AreEqual("git", bin);
            return Task.FromResult(run(arg, environmentVariables!));
        }
    }
}
