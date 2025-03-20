using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Aiursoft.CSTools.Tools;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.Options;

namespace Aiursoft.GitMirrorServer.BackgroundJobs
{
    public class MirrorJob(
        IConfiguration configuration,
        IOptions<List<MirrorConfig>> configs,
        ILogger<MirrorJob> logger,
        IServiceScopeFactory scopeFactory)
        : IHostedService, IDisposable, ISingletonDependency
    {
        private readonly ILogger _logger = logger;
        private Timer? _timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!EntryExtends.IsProgramEntry())
            {
                _logger.LogInformation("Skip cleaner in test environment.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Timed Background Service is starting");
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            try
            {
                _logger.LogInformation("Cleaner task started!");
                using var scope = scopeFactory.CreateScope();
                var serviceFactory = scope.ServiceProvider.GetRequiredService<GitServiceFactory>();
                var workspaceManager = scope.ServiceProvider.GetRequiredService<WorkspaceManager>();
                await DoMirror(serviceFactory, workspaceManager);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred");
            }
        }

        public async Task DoMirror(GitServiceFactory serviceFactory, WorkspaceManager workspaceManager)
        {
            var diskRoot = configuration["Storage:Path"]!;
            logger.LogInformation("Start to mirror {Count} configs, using disk root: {diskRoot}", configs.Value.Count,
                diskRoot);

            foreach (var config in configs.Value)
            {
                logger.LogInformation("Processing mirror: {FromOrg} ({FromType}) -> {ToOrg} ({ToType})",
                    config.FromOrgName, config.FromType, config.TargetOrgName, config.TargetType);

                try
                {
                    // Create source and target services
                    var sourceService = serviceFactory.CreateGitService(
                        config.FromType, config.FromServer, config.FromToken);

                    var targetService = serviceFactory.CreateGitService(
                        config.TargetType, config.TargetServer, config.TargetToken);

                    // Get repositories from source
                    var repos = (await sourceService
                            .GetRepositoriesAsync(config.FromOrgName, isOrg: config.OrgOrUser.ToLower().StartsWith("org")))
                        .Where(r => !r.Archived)
                        .ToList();
                    logger.LogInformation("Found {count} repositories to mirror", repos.Count());

                    foreach (var repo in repos)
                    {
                        var repoPath = Path.Combine(diskRoot, config.FromOrgName, repo.Name);
                        Directory.CreateDirectory(repoPath);

                        logger.LogInformation("Processing repository: {repoName}", repo.Name);

                        try
                        {
                            // Ensure target repo exists
                            logger.LogInformation("Ensuring target repository {repo} exists", repo.Name);
                            await targetService.EnsureRepositoryExistsAsync(config.TargetOrgName, repo.Name, isOrg: config.TargetType.ToLowerInvariant() == "org");

                            // Get URLs
                            var sourceUrl = sourceService.GetCloneUrl(config.FromOrgName, repo.Name);
                            var targetUrl =
                                targetService.GetPushUrl(config.TargetOrgName, repo.Name, config.TargetToken);

                            logger.LogInformation("Setting up local repository at {path}", repoPath);

                            try
                            {
                                // Reset or clone the repo
                                await workspaceManager.ResetRepo(
                                    repoPath,
                                    null, // No specific branch
                                    sourceUrl,
                                    CloneMode.Full);

                                // Update all local branches to match remote
                                logger.LogInformation("Updating all branches for {repo}", repo.Name);
                                await workspaceManager.EnsureAllLocalBranchesUpToDateWithRemote(repoPath);

                                // Add target remote
                                logger.LogInformation("Setting up target remote for {repo}", repo.Name);
                                await workspaceManager.AddOrSetRemoteUrl(repoPath, "target", targetUrl);

                                // Push all branches and tags to target
                                logger.LogInformation("Pushing all branches and tags for {repo} to target", repo.Name);
                                await workspaceManager.PushAllBranchesAndTags(repoPath, "target", force: true);

                                logger.LogInformation("Successfully mirrored repository {repo}", repo.Name);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error mirroring repository {repo}", repo.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error setting up repository {repo}", repo.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing mirror config: {FromOrg} -> {ToOrg}",
                        config.FromOrgName, config.TargetOrgName);
                }
            }

            logger.LogInformation("Mirror job completed");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

public interface IGitService
{
    Task<IReadOnlyCollection<GitRepository>> GetRepositoriesAsync(string orgOrUser, bool isOrg);
    Task EnsureRepositoryExistsAsync(string orgOrUser, string repositoryName, bool isOrg);
    string GetCloneUrl(string orgOrUser, string repositoryName);
    string GetPushUrl(string orgOrUser, string repositoryName, string token);
}

public class GitRepository
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }

    public bool Archived { get; set; }
}

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

public class GitHubRepo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Private { get; set; }

    public bool Archived { get; set; }
}

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

public class GitLabRepo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public bool Archived { get; set; }
}

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
