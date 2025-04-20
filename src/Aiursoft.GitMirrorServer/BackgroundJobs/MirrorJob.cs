using Aiursoft.CSTools.Tools;
using Aiursoft.GitMirrorServer.Models.Configuration;
using Aiursoft.GitMirrorServer.Services;
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
        private const int IntervalMinutes = 45;
        private Timer? _timer;

        public static DateTime LastRunTime = DateTime.MinValue;
        public static TimeSpan LastRunDuration = TimeSpan.Zero;
        public static bool LastRunSuccess;
        public static int SuccessMirrorCount;
        public static int FailMirrorCount;
        public static int TotalMirroredCount;
        public static int TotalMirrorServerConfigsCount;
        public static DateTime EstimatedNextRunTime = DateTime.MinValue;
        public static List<(string OrgName, string RepoName, string ErrorMessage)> FailedMirrors { get; private set; } = new List<(string, string, string)>();

        private static void ClearStats()
        {
            LastRunTime = DateTime.MinValue;
            LastRunDuration = TimeSpan.Zero;
            LastRunSuccess = false;
            SuccessMirrorCount = 0;
            FailMirrorCount = 0;
            TotalMirroredCount = 0;
            TotalMirrorServerConfigsCount = 0;
            EstimatedNextRunTime = DateTime.MinValue;
            FailedMirrors.Clear();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!EntryExtends.IsProgramEntry())
            {
                _logger.LogInformation("Skip mirror in test environment.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Timed Background Service is starting. Mirror all repos every {Interval} minutes.",
                IntervalMinutes);
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(IntervalMinutes));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            try
            {
                _logger.LogInformation("Mirror job started");
                ClearStats();
                // Calculate next estimated run time based on the timer interval (IntervalMinutes minutes)
                EstimatedNextRunTime = DateTime.UtcNow.AddMinutes(IntervalMinutes);
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
            var startTime = DateTime.UtcNow;
            LastRunTime = startTime;
            TotalMirrorServerConfigsCount = configs.Value.Count;

            // Reset counters for this run
            SuccessMirrorCount = 0;
            FailMirrorCount = 0;
            TotalMirroredCount = 0;

            var diskRoot = configuration["Storage:Path"]!;
            logger.LogInformation("Start to mirror {Count} configs, using disk root: {diskRoot}", configs.Value.Count,
                diskRoot);

            try
            {
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
                                .GetRepositoriesAsync(config.FromOrgName,
                                    isOrg: config.OrgOrUser.ToLower().StartsWith("org")))
                            .Where(r => !r.Archived)
                            .ToList();
                        logger.LogInformation("Found {count} repositories to mirror", repos.Count());

                        foreach (var repo in repos)
                        {
                            var repoPath = Path.Combine(diskRoot, config.FromOrgName, repo.Name);
                            Directory.CreateDirectory(repoPath);

                            logger.LogInformation("Processing repository: {repoName}", repo.Name);
                            TotalMirroredCount++;

                            try
                            {
                                // Ensure target repo exists
                                logger.LogInformation("Ensuring target repository {repo} exists", repo.Name);
                                await targetService.EnsureRepositoryExistsAsync(config.TargetOrgName, repo.Name,
                                    isOrg: config.TargetType.ToLowerInvariant() == "org");

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
                                    logger.LogInformation("Pushing all branches and tags for {repo} to target",
                                        repo.Name);
                                    await workspaceManager.PushAllBranchesAndTags(repoPath, "target", force: true);

                                    logger.LogInformation("Successfully mirrored repository {repo}", repo.Name);
                                    SuccessMirrorCount++;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error mirroring repository {repo}", repo.Name);
                                    FailMirrorCount++;
                                    FailedMirrors.Add((config.FromOrgName, repo.Name, ex.Message));
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error setting up repository {repo}", repo.Name);
                                FailMirrorCount++;
                                FailedMirrors.Add((config.FromOrgName, repo.Name, ex.Message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing mirror config: {FromOrg} -> {ToOrg}",
                            config.FromOrgName, config.TargetOrgName);
                    }
                }

                LastRunSuccess = true;
            }
            catch (Exception)
            {
                LastRunSuccess = false;
                throw;
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                LastRunDuration = endTime - startTime;

                logger.LogInformation(
                    "Mirror job completed in {duration}. Success: {success}, Failed: {failed}, Total: {total}",
                    LastRunDuration, SuccessMirrorCount, FailMirrorCount, TotalMirroredCount);
            }
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
