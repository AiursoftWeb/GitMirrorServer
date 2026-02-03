using Aiursoft.GitMirrorServer.Abstractions;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.GitMirrorServer.Services;

public class GitServiceFactory : ITransientDependency
{
    public IGitService CreateGitService(string serviceType, string baseUrl, string? token)
    {
        return serviceType.ToLowerInvariant() switch
        {
            "github" => new GitHubService(baseUrl, token),
            "gitlab" => new GitLabService(baseUrl, token),
            _ => throw new ArgumentException($"Unsupported git service type: {serviceType}")
        };
    }
}
