using System.Text;
using Aiursoft.CSTools.Services;
using Aiursoft.GitRunner;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.GitMirrorServer.Services;

public class GitSourceService(CommandService commandService, WorkspaceManager workspaceManager) : ITransientDependency
{
    public async Task UpdateAsync(string path, string sourceUrl, string token)
    {
        var environment = CreateEnvironment(sourceUrl, token);
        var source = new Uri(sourceUrl);

        async Task<string> RunAsync(string arguments)
        {
            var (code, output, error) = await commandService.RunCommandAsync(
                "git", arguments, path, environmentVariables: environment);
            if (code != 0)
            {
                var diagnostic = Redact(error, token);
                throw new InvalidOperationException($"Git source update failed (exit {code}): {diagnostic}");
            }

            return output;
        }

        if (!Directory.Exists(Path.Combine(path, ".git")))
        {
            await RunAsync($"clone -- \"{source.AbsoluteUri}\" .");
            return;
        }

        var remote = (await RunAsync("remote get-url origin")).Trim();
        if (!string.Equals(remote, source.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The cached repository does not match the configured Git source.");
        }

        await RunAsync("reset --hard HEAD");
        await RunAsync("clean -fdx");
        await RunAsync("fetch origin");
    }

    public async Task UpdateBranchesAsync(string path, string sourceUrl, string token)
    {
        var environment = CreateEnvironment(sourceUrl, token);
        try
        {
            await workspaceManager.EnsureAllLocalBranchesUpToDateWithRemote(path, "origin", environment);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Git branch update failed: {Redact(exception.Message, token)}");
        }
    }

    private static string Redact(string message, string token) => message
        .Replace(Convert.ToBase64String(Encoding.UTF8.GetBytes($"oauth2:{token}")), "[REDACTED]", StringComparison.Ordinal)
        .Replace(token, "[REDACTED]", StringComparison.Ordinal);

    private static Dictionary<string, string?> CreateEnvironment(string sourceUrl, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var source = new Uri(sourceUrl);
        if (source.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(source.UserInfo))
        {
            throw new InvalidOperationException("Authenticated Git sources require an HTTPS URL without credentials.");
        }

        var encodedCredential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"oauth2:{token}"));
        // Runtime configuration belongs only to this child process, never to a URL or .git/config.
        return new Dictionary<string, string?>
        {
            ["GIT_TERMINAL_PROMPT"] = "0",
            ["GIT_CONFIG_COUNT"] = "3",
            ["GIT_CONFIG_KEY_0"] = $"http.{source.AbsoluteUri}.extraHeader",
            ["GIT_CONFIG_VALUE_0"] = $"Authorization: Basic {encodedCredential}",
            ["GIT_CONFIG_KEY_1"] = "credential.helper",
            ["GIT_CONFIG_VALUE_1"] = string.Empty,
            ["GIT_CONFIG_KEY_2"] = "http.followRedirects",
            ["GIT_CONFIG_VALUE_2"] = "false"
        };
    }
}
