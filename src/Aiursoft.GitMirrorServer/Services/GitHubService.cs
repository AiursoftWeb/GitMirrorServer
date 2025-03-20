using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aiursoft.GitMirrorServer.Abstractions;
using Aiursoft.GitMirrorServer.Models;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.GitMirrorServer.Services;

public class GitHubService : IGitService, ITransientDependency
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public GitHubService(string baseUrl, string? token)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitMirror", "1.0"));
    }

    public async Task<IReadOnlyCollection<GitRepository>> GetRepositoriesAsync(string orgOrUser, bool isOrg)
    {
        string endpoint = isOrg
            ? $"{_baseUrl}/orgs/{orgOrUser}/repos?per_page=100"
            : $"{_baseUrl}/users/{orgOrUser}/repos?per_page=100";

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var repos = JsonSerializer.Deserialize<List<GitHubRepo>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<GitHubRepo>();

        return repos.Select(r => new GitRepository
        {
            Name = r.Name,
            Description = r.Description ?? string.Empty,
            IsPrivate = r.Private,
            Archived = r.Archived
        }).ToList();
    }

    public async Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg)
    {
        // First check if repo exists
        var checkEndpoint = $"{_baseUrl}/repos/{orgOrUser}/{repositoryName}";
        var checkResponse = await _httpClient.GetAsync(checkEndpoint);
        if (checkResponse.IsSuccessStatusCode)
        {
            return;
        }

        // Create the repo
        string createEndpoint;
        if (!isOrg)
        {
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
            @private = false // Adjust as needed
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