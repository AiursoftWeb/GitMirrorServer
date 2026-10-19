using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aiursoft.GitMirrorServer.Abstractions;
using Aiursoft.GitMirrorServer.Models;

namespace Aiursoft.GitMirrorServer.Services;

public class GitHubService : IGitService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public GitHubService(string baseUrl, string? token, HttpMessageHandler? handler = null)
    {
        _baseUrl = baseUrl;
        _httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitMirror", "1.0"));
    }

    public async Task<IReadOnlyCollection<GitRepository>> GetRepositoriesAsync(string orgOrUser, bool isOrg,
        bool includePrivate = false, bool includeArchived = false)
    {
        string endpoint = isOrg
            ? $"{_baseUrl}/orgs/{orgOrUser}/repos?per_page=100"
            : $"{_baseUrl}/users/{orgOrUser}/repos?per_page=100";

        if (!isOrg && includePrivate)
        {
            // GitHub's public /users/{name}/repos endpoint never returns private repositories.
            using var userResponse = await _httpClient.GetAsync($"{_baseUrl}/user");
            userResponse.EnsureSuccessStatusCode();
            using var user = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
            if (!string.Equals(user.RootElement.GetProperty("login").GetString(), orgOrUser,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "To include Private personal GitHub repositories, the source token must belong to the configured source user.");
            }
            endpoint = $"{_baseUrl}/user/repos?per_page=100&affiliation=owner";
        }

        var repos = new List<GitHubRepo>();
        for (var page = 1; ; page++)
        {
            using var response = await _httpClient.GetAsync(page == 1 ? endpoint : $"{endpoint}&page={page}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var batch = JsonSerializer.Deserialize<List<GitHubRepo>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<GitHubRepo>();
            repos.AddRange(batch);
            if (batch.Count < 100)
            {
                break;
            }
        }

        return repos.Where(r => (includePrivate || !r.Private) && (includeArchived || !r.Archived))
            .Select(r => new GitRepository
        {
            Name = r.Name,
            Description = r.Description ?? string.Empty,
            Visibility = r.Private ? "private" : "public",
            IsPrivate = r.Private,
            Archived = r.Archived
        }).ToList();
    }

    public async Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg,
        bool? isPrivate = null, bool requirePrivate = false)
    {
        // First check if repo exists
        var checkEndpoint = $"{_baseUrl}/repos/{orgOrUser}/{repositoryName}";
        var checkResponse = await _httpClient.GetAsync(checkEndpoint);
        if (checkResponse.IsSuccessStatusCode)
        {
            if (requirePrivate)
            {
                using var existing = JsonDocument.Parse(await checkResponse.Content.ReadAsStringAsync());
                if (!existing.RootElement.GetProperty("private").GetBoolean())
                {
                    throw new InvalidOperationException(
                        "The existing GitHub target is Public. Make it Private before mirroring a Private source, or explicitly configure Public publication.");
                }
            }
            return;
        }
        if (checkResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            checkResponse.EnsureSuccessStatusCode();
        }

        // Resolve the target account itself; a mirror may target a user or an organization.
        using var ownerResponse = await _httpClient.GetAsync($"{_baseUrl}/users/{orgOrUser}");
        ownerResponse.EnsureSuccessStatusCode();
        using var owner = JsonDocument.Parse(await ownerResponse.Content.ReadAsStreamAsync());
        isOrg = string.Equals(owner.RootElement.GetProperty("type").GetString(),
            "Organization", StringComparison.OrdinalIgnoreCase);

        // Create the repo
        string createEndpoint;
        if (!isOrg)
        {
            // /user/repos always creates under the token owner, regardless of orgOrUser.
            // Refuse to create a repository under a different account by accident.
            var userResponse = await _httpClient.GetAsync($"{_baseUrl}/user");
            userResponse.EnsureSuccessStatusCode();
            using var user = JsonDocument.Parse(await userResponse.Content.ReadAsStreamAsync());
            var login = user.RootElement.GetProperty("login").GetString();
            if (!string.Equals(login, orgOrUser, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Authenticated GitHub user '{login}' does not match target owner '{orgOrUser}'.");
            }

            // For user repositories
            createEndpoint = $"{_baseUrl}/user/repos";
        }
        else
        {
            // For organization repositories
            createEndpoint = $"{_baseUrl}/orgs/{orgOrUser}/repos";
        }

        var createContent = new
        {
            name = repositoryName,
            @private = isPrivate ?? false
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(createContent),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _httpClient.PostAsync(createEndpoint, jsonContent);
        createResponse.EnsureSuccessStatusCode();
    }

    public string GetCloneUrl(string orgOrUser, string repositoryName)
    {
        return $"https://github.com/{orgOrUser}/{repositoryName}.git";
    }

    public string GetPushUrl(string orgOrUser, string repositoryName, string token)
    {
        return $"https://x-access-token:{token}@github.com/{orgOrUser}/{repositoryName}.git";
    }
}
