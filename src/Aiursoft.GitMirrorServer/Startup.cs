using Aiursoft.GitMirrorServer.BackgroundJobs;
using Aiursoft.GitMirrorServer.Models.Configuration;
using Aiursoft.GitRunner;
using Aiursoft.Scanner;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Aiursoft.GitMirrorServer;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        var section = configuration.GetSection("Mirrors");
        services.Configure<List<MirrorConfig>>(section);
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                var oidcConfig = configuration.GetSection("OIDC");

                options.Authority = oidcConfig["Authority"];
                options.ClientId = oidcConfig["ClientId"];
                options.ClientSecret = oidcConfig["ClientSecret"];
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                options.TokenValidationParameters.RoleClaimType = "groups";
            });

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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDefaultControllerRoute();
    }
}
