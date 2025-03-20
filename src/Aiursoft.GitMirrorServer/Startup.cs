using Aiursoft.Scanner;
using Aiursoft.WebTools.Abstractions.Models;

namespace Aiursoft.GitMirrorServer;

public class MirrorConfig
{
    public required string FromType { get; init; }
    public required string FromToken { get; init; }
    public required string FromServer { get; init; }
    public required string FromOrgName { get; init; }
    public required string FromOrgType { get; init; }
    public required string TargetType { get; init; }
    public required string TargetServer { get; init; }
    public required string TargetToken { get; init; }
    public required string TargetOrgName { get; init; }
}

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        var section = configuration.GetSection("Mirrors");
        services.Configure<List<MirrorConfig>>(section);
        services.AddLibraryDependencies();

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
