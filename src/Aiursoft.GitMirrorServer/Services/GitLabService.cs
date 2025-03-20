using System.Net.Http.Headers;
using System.Text.Json;
using Aiursoft.GitMirrorServer.Abstractions;
using Aiursoft.GitMirrorServer.Models;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.GitMirrorServer.Services;

public class GitLabService : IGitService, ITransientDependency
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public GitLabService(string baseUrl, string? token)
    {
        _baseUrl = baseUrl.TrimEnd('/') + "/api/v4";
        _httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
        }
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IReadOnlyCollection<GitRepository>> GetRepositoriesAsync(string orgOrUser, bool isOrg)
    {
        string endpoint;
        if (isOrg)
        {
            // For GitLab, organizations are called "groups"
            endpoint = $"{_baseUrl}/groups/{orgOrUser}/projects?per_page=100&include_subgroups=true";
        }
        else
        {
            // For user-owned repos
            endpoint = $"{_baseUrl}/users/{orgOrUser}/projects?per_page=100";
        }

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var repos = JsonSerializer.Deserialize<List<GitLabRepo>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<GitLabRepo>();

        return repos.Select(r => new GitRepository
        {
            Name = r.Name,
            Description = r.Description ?? string.Empty,
            IsPrivate = r.Visibility != "public",
            Archived = r.Archived
        }).ToList();
    }

    public Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg)
    {
        throw new NotImplementedException();
    }

    public string GetCloneUrl(string orgOrUser, string repositoryName)
    {
        return $"{_baseUrl.Replace("/api/v4", "")}/{orgOrUser}/{repositoryName}.git";
    }

    public string GetPushUrl(string orgOrUser, string repositoryName, string token)
    {
        // Not typically needed for our use case
        return
            $"{_baseUrl.Replace("/api/v4", "")}:{token}@{_baseUrl.Replace("https://", "").Replace("/api/v4", "")}/{orgOrUser}/{repositoryName}.git";
    }
}