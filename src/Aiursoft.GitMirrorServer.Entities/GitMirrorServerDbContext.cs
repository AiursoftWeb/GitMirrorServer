using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Entities;

[ExcludeFromCodeCoverage]

public abstract class GitMirrorServerDbContext(DbContextOptions options) : IdentityDbContext<User>(options), ICanMigrate
{
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
    public DbSet<MirrorConfiguration> MirrorConfigurations => Set<MirrorConfiguration>();
    public DbSet<MirrorJobExecution> MirrorJobExecutions => Set<MirrorJobExecution>();
    public DbSet<MirrorRepoExecution> MirrorRepoExecutions => Set<MirrorRepoExecution>();

    public virtual  Task MigrateAsync(CancellationToken cancellationToken) =>
        Database.MigrateAsync(cancellationToken);

    public virtual  Task<bool> CanConnectAsync() =>
        Database.CanConnectAsync();
}
