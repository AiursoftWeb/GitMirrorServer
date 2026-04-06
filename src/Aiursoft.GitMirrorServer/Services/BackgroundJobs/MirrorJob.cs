using Aiursoft.Canon.BackgroundJobs;

namespace Aiursoft.GitMirrorServer.Services.BackgroundJobs;

public class MirrorJob(
    MirrorService mirrorService,
    ILogger<MirrorJob> logger) : IBackgroundJob
{
    public string Name => "Mirror Job";
    public string Description => "Mirrors all configured repositories between Git servers.";

    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting scheduled mirror job...");
        await mirrorService.RunMirrorAsync();
        logger.LogInformation("Mirror job completed.");
    }
}
