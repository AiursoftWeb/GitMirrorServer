using Aiursoft.GitMirrorServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Services.BackgroundJobs;

public class MirrorScheduledService(
    BackgroundJobQueue backgroundJobQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<MirrorScheduledService> logger) : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Mirror Scheduled Service is starting.");
        // Check every minute
        _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            // 1. Check if job is already running or pending
            var isRunningOrPending = backgroundJobQueue.GetPendingJobs().Any(j => j.QueueName == "MirrorQueue") ||
                                     backgroundJobQueue.GetProcessingJobs().Any(j => j.QueueName == "MirrorQueue");

            if (isRunningOrPending)
            {
                // Already running, skip
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<GitMirrorServerDbContext>();

            // 2. Get interval
            var interval = await settingsService.GetIntSettingAsync("MirrorIntervalMinutes");
            if (interval <= 0) interval = 30; // Default safety

            // 3. Check last run time
            var lastRun = await dbContext.MirrorJobExecutions
                .OrderByDescending(j => j.StartTime)
                .Select(j => j.StartTime)
                .FirstOrDefaultAsync();

            if (lastRun == default || lastRun.AddMinutes(interval) < DateTime.UtcNow)
            {
                logger.LogInformation("Time to run mirror job. Last run: {LastRun}, Interval: {Interval}", lastRun, interval);
                backgroundJobQueue.QueueWithDependency<MirrorService>(
                    queueName: "MirrorQueue",
                    jobName: "Scheduled Mirror Job",
                    job: async (service) => await service.RunMirrorAsync()
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in MirrorScheduledService");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Mirror Scheduled Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}