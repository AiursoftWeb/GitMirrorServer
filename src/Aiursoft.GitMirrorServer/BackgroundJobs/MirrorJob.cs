using Aiursoft.CSTools.Tools;
using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.Options;

namespace Aiursoft.GitMirrorServer.BackgroundJobs
{
    public class MirrorJob(
        IConfiguration configuration,
        IOptions<List<MirrorConfig>> configs,
        ILogger<MirrorJob> logger,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env)
        : IHostedService, IDisposable, ISingletonDependency
    {
        private readonly ILogger _logger = logger;
        private Timer? _timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (env.IsDevelopment() || !EntryExtends.IsProgramEntry())
            {
                _logger.LogInformation("Skip cleaner in development environment");
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
                await DoMirror();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred");
            }
        }

        public async Task DoMirror()
        {
            var diskRoot = configuration["Storage:Path"];
            logger.LogInformation("Start to mirror {Count} configs, using disk root: {diskRoot}", configs.Value.Count, diskRoot);
            // Mirror logic here
            await Task.CompletedTask;
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
