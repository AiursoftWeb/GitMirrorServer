using System.Text;
using Aiursoft.CSTools.Services;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.GitMirrorServer.Services;

public class GitLabSourceService(CommandService commandService) : ITransientDependency
{
    public async Task UpdateAsync(string path, string sourceUrl, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var source = new Uri(sourceUrl);
        if (source.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(source.UserInfo))
        {
            throw new InvalidOperationException("Authenticated GitLab sources require an HTTPS URL without credentials.");
        }

        var encodedCredential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"oauth2:{token}"));
        // Runtime configuration belongs only to this child process, never to a URL or .git/config.
        var environment = new Dictionary<string, string?>
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

        async Task<string> RunAsync(string arguments)
        {
            var (code, output, error) = await commandService.RunCommandAsync(
                "git", arguments, path, environmentVariables: environment);
            if (code != 0)
            {
                var diagnostic = error.Replace(encodedCredential, "[REDACTED]", StringComparison.Ordinal)
                    .Replace(token, "[REDACTED]", StringComparison.Ordinal);
                throw new InvalidOperationException($"GitLab source update failed (exit {code}): {diagnostic}");
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
            throw new InvalidOperationException("The cached repository does not match the configured GitLab source.");
        }

        await RunAsync("reset --hard HEAD");
        await RunAsync("clean -fdx");
        await RunAsync("fetch origin");
    }
}
