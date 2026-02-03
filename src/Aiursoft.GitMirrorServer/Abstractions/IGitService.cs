using Aiursoft.GitMirrorServer.Models;

namespace Aiursoft.GitMirrorServer.Abstractions;

public interface IGitService
{
    Task<IReadOnlyCollection<GitRepository>> GetRepositoriesAsync(string orgOrUser, bool isOrg);
    Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg);
    string GetCloneUrl(string orgOrUser, string repositoryName);
    string GetPushUrl(string orgOrUser, string repositoryName, string token);
}
