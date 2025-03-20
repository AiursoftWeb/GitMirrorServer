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

    public async Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg)
    {
        try
        {
            // Check if the repository exists
            var exists = await RepositoryExistsAsync(orgOrUser, repositoryName, isOrg);
            if (exists)
            {
                return; // Repository already exists
            }

            // Create the repository
            var endpoint = $"{_baseUrl}/projects";

            var projectData = new Dictionary<string, object>
            {
                { "name", repositoryName },
                { "visibility", "private" } // Default to private
            };

            // If it's an organization (group), we need to find the group ID
            if (isOrg)
            {
                var groupId = await GetGroupIdAsync(orgOrUser);
                if (groupId.HasValue)
                {
                    projectData.Add("namespace_id", groupId.Value);
                }
                else
                {
                    throw new Exception($"Group '{orgOrUser}' not found.");
                }
            }

            var requestContent = new StringContent(
                JsonSerializer.Serialize(projectData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(endpoint, requestContent);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to ensure repository exists: {ex.Message}", ex);
        }
    }

    private async Task<bool> RepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg)
    {
        string endpoint;
        if (isOrg)
        {
            endpoint = $"{_baseUrl}/groups/{orgOrUser}/projects?search={repositoryName}";
        }
        else
        {
            endpoint = $"{_baseUrl}/users/{orgOrUser}/projects?search={repositoryName}";
        }

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var repos = JsonSerializer.Deserialize<List<GitLabRepo>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<GitLabRepo>();

        return repos.Any(r => r.Name.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<int?> GetGroupIdAsync(string groupPath)
    {
        var endpoint = $"{_baseUrl}/groups?search={groupPath}";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var groups = JsonSerializer.Deserialize<List<GitLabGroup>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<GitLabGroup>();

        var group = groups.FirstOrDefault(g => g.Path.Equals(groupPath, StringComparison.OrdinalIgnoreCase));
        return group?.Id;
    }

    private class GitLabGroup
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public int Id { get; init; }
        public string Path { get; init; } = string.Empty;
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
