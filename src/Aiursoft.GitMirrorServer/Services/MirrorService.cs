using System.Text;
using Aiursoft.CSTools.Tools;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Models;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Services;

public class MirrorService(
    GitMirrorServerDbContext dbContext,
    GitServiceFactory serviceFactory,
    WorkspaceManager workspaceManager,
    ILogger<MirrorService> logger,
    IConfiguration configuration) : IScopedDependency
{
    public async Task RunMirrorAsync()
    {
        var jobExecution = new MirrorJobExecution
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            IsSuccess = true
        };
        dbContext.MirrorJobExecutions.Add(jobExecution);
        await dbContext.SaveChangesAsync();

        try
        {
            var configs = await dbContext.MirrorConfigurations.ToListAsync();
            var diskRoot = configuration["Storage:Path"] ?? Path.Combine(Path.GetTempPath(), "gitmirrors");
            
            logger.LogInformation("Start to mirror {Count} configs, using disk root: {diskRoot}", configs.Count, diskRoot);
            jobExecution.TotalCount = 0; // Will count repos

            foreach (var config in configs)
            {
                logger.LogInformation("Processing mirror: {FromOrg} ({FromType}) -> {ToOrg} ({ToType})",
                    config.FromOrgName, config.FromType, config.TargetOrgName, config.TargetType);

                try
                {
                    var sourceService = serviceFactory.CreateGitService(
                        config.FromType, config.FromServer, config.FromToken);

                    var targetService = serviceFactory.CreateGitService(
                        config.TargetType, config.TargetServer, config.TargetToken);

                    var repos = (await sourceService
                            .GetRepositoriesAsync(config.FromOrgName,
                                isOrg: config.OrgOrUser.ToLower().StartsWith("org")))
                        .Where(r => !r.Archived)
                        .ToList();
                    
                    logger.LogInformation("Found {count} repositories to mirror", repos.Count());
                    jobExecution.TotalCount += repos.Count;

                    foreach (var repo in repos)
                    {
                        var repoLog = new StringBuilder();
                        void Log(string message)
                        {
                            logger.LogInformation(message);
                            repoLog.AppendLine($"[{DateTime.UtcNow:O}] {message}");
                        }
                        
                        var repoExecution = new MirrorRepoExecution
                        {
                            Id = Guid.NewGuid(),
                            JobExecutionId = jobExecution.Id,
                            FromOrg = config.FromOrgName,
                            RepoName = repo.Name,
                            TargetOrg = config.TargetOrgName,
                            IsSuccess = false
                        };

                        var repoPath = Path.Combine(diskRoot, config.FromOrgName, repo.Name);
                        Log($"Processing repository: {repo.Name}");

                        try
                        {
                            await RepositoryMirrorRetryRunner.RunAsync(
                                repo.Name,
                                jobExecution,
                                repoExecution,
                                async () =>
                                {
                                    Log($"Ensuring target repository {repo.Name} exists");
                                    await targetService.EnsureRepositoryExistsAsync(config.TargetOrgName, repo.Name,
                                        isOrg: true);

                                    var sourceUrl = sourceService.GetCloneUrl(config.FromOrgName, repo.Name);
                                    var targetUrl = targetService.GetPushUrl(
                                        config.TargetOrgName,
                                        repo.Name,
                                        config.TargetToken);

                                    Directory.CreateDirectory(repoPath);
                                    Log($"Setting up local repository at {repoPath}");

                                    await workspaceManager.ResetRepo(
                                        repoPath,
                                        null,
                                        sourceUrl,
                                        CloneMode.Full);

                                    Log($"Updating all branches for {repo.Name}");
                                    await workspaceManager.EnsureAllLocalBranchesUpToDateWithRemote(repoPath);

                                    Log($"Setting up target remote for {repo.Name}");
                                    await workspaceManager.AddOrSetRemoteUrl(repoPath, "target", targetUrl);

                                    Log($"Pushing all branches and tags for {repo.Name} to target");
                                    await workspaceManager.PushAllBranchesAndTags(repoPath, "target", force: true);

                                    Log($"Successfully mirrored repository {repo.Name}");
                                },
                                () => FolderDeleter.DeleteByForce(repoPath),
                                Log,
                                (attempt, exception) => logger.LogError(
                                    exception,
                                    "Attempt {Attempt}/{MaximumAttempts} failed while mirroring repository {Repo}",
                                    attempt,
                                    RepositoryMirrorRetryRunner.MaximumAttempts,
                                    repo.Name));
                        }
                        finally
                        {
                            await RecordRepositoryExecutionAsync(jobExecution, repoExecution, repoLog.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing mirror config: {FromOrg} -> {ToOrg}",
                        config.FromOrgName, config.TargetOrgName);
                    // This error is at config level, not repo level. 
                    // Should we record a general error on job?
                    if (string.IsNullOrEmpty(jobExecution.ErrorMessage))
                        jobExecution.ErrorMessage = ex.Message;
                    else
                        jobExecution.ErrorMessage += $"; {ex.Message}";
                    
                    jobExecution.IsSuccess = false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error in mirror job");
            jobExecution.IsSuccess = false;
            jobExecution.ErrorMessage = $"Critical error: {ex.Message}";
        }
        finally
        {
            jobExecution.EndTime = DateTime.UtcNow;
            dbContext.MirrorJobExecutions.Update(jobExecution);
            await dbContext.SaveChangesAsync();
        }
    }

    internal async Task RecordRepositoryExecutionAsync(
        MirrorJobExecution jobExecution,
        MirrorRepoExecution repoExecution,
        string log)
    {
        repoExecution.Log = log;
        dbContext.MirrorRepoExecutions.Add(repoExecution);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Do not retry an unsaved repository record when saving the next repository.
            dbContext.Entry(repoExecution).State = EntityState.Detached;
            logger.LogError(ex, "Failed to record mirror result for {Repo}", repoExecution.RepoName);
            jobExecution.IsSuccess = false;
            var error = $"Failed to record mirror result for {repoExecution.RepoName}: {ex.Message}";
            jobExecution.ErrorMessage = string.IsNullOrEmpty(jobExecution.ErrorMessage)
                ? error
                : $"{jobExecution.ErrorMessage}; {error}";
        }
    }

}
