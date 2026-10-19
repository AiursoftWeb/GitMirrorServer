using Aiursoft.GitMirrorServer.Models;

namespace Aiursoft.GitMirrorServer.Abstractions;

public interface IGitService
{
    Task<IReadOnlyCollection<GitRepository>> GetRepositoriesAsync(string orgOrUser, bool isOrg,
        bool includePrivate = false, bool includeArchived = false);
    Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg,
        bool? isPrivate = null, bool requirePrivate = false);
    string GetCloneUrl(string orgOrUser, string repositoryName);
    string GetPushUrl(string orgOrUser, string repositoryName, string token);
}
