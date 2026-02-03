namespace Aiursoft.GitMirrorServer.Services.BackgroundJobs;

public class MirrorScheduledService(
    BackgroundJobQueue backgroundJobQueue,
    ILogger<MirrorScheduledService> logger) : IHostedService, IDisposable
{
    private Timer? _timer;
    private const int IntervalMinutes = 45;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Mirror Scheduled Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(IntervalMinutes));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        logger.LogInformation("Queueing scheduled mirror job.");
        backgroundJobQueue.QueueWithDependency<MirrorService>(
            queueName: "MirrorQueue", 
            jobName: "Scheduled Mirror Job",
            job: async (service) => await service.RunMirrorAsync()
        );
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
