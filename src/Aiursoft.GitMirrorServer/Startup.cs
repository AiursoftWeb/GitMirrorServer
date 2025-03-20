using Aiursoft.GitMirrorServer.BackgroundJobs;
using Aiursoft.GitMirrorServer.Models.Configuration;
using Aiursoft.GitRunner;
using Aiursoft.Scanner;
using Aiursoft.WebTools.Abstractions.Models;

namespace Aiursoft.GitMirrorServer;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        var section = configuration.GetSection("Mirrors");
        services.Configure<List<MirrorConfig>>(section);
        services.AddLibraryDependencies();
        services.AddGitRunner();
        services.AddSingleton<IHostedService, MirrorJob>();

        services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(Startup).Assembly);
    }

    public void Configure(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}
