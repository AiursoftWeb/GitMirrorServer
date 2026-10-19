using Aiursoft.GitMirrorServer.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Aiursoft.GitMirrorServer.MySql;
using Aiursoft.GitMirrorServer.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Tests.IntegrationTests;

/// <summary>
/// This test class ensures that the Entity Framework migrations are up-to-date for all supported database providers.
/// If you change the database model (entities), you must create a new migration for both SQLite and MySQL.
/// </summary>
[TestClass]
public class MigrationTests
{
    [TestMethod]
    public void TestSqliteMigrations()
    {
        var options = new DbContextOptionsBuilder<SqliteContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        using var context = new SqliteContext(options);
        var hasPendingChanges = context.Database.HasPendingModelChanges();
        Assert.IsFalse(hasPendingChanges, "There are pending model changes for Sqlite. Please run 'dotnet ef migrations add' for Sqlite.");
    }

    [TestMethod]
    [DataRow("GitLab", "Org", false)]
    [DataRow("GitHub", "Org", true)]
    [DataRow("GitHub", "User", false)]
    public async Task ExistingMirrorPolicyIsPreservedDuringSqliteUpgrade(string fromType, string ownerType, bool includePrivate)
    {
        var options = new DbContextOptionsBuilder<SqliteContext>()
            .UseSqlite("DataSource=:memory:").Options;
        await using var context = new SqliteContext(options);
        await context.Database.OpenConnectionAsync();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260203135308_AddHistory");
        var id = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO MirrorConfigurations
                (Id, FromType, FromServer, FromToken, FromOrgName, OrgOrUser,
                 TargetType, TargetServer, TargetToken, TargetOrgName, CreationTime)
            VALUES ({id}, {fromType}, 'https://source.example.com', 'source-token', 'source', {ownerType},
                    'GitHub', 'https://api.github.com', 'target-token', 'target', {DateTime.UtcNow});
            """);

        await migrator.MigrateAsync();

        var config = await context.MirrorConfigurations.SingleAsync();
        Assert.IsTrue(config.IncludePublicRepositories);
        Assert.IsTrue(config.IncludeInternalRepositories);
        Assert.AreEqual(includePrivate, config.IncludePrivateRepositories);
        Assert.IsFalse(config.IncludeArchivedRepositories);
        Assert.IsFalse(config.NewGitHubRepositoriesPrivate);
        Assert.AreEqual("source-token", config.FromToken);
        Assert.AreEqual("target-token", config.TargetToken);

        context.MirrorConfigurations.Add(new MirrorConfiguration
        {
            FromType = fromType, FromServer = "https://source.example.com", FromOrgName = "new-source",
            OrgOrUser = "Org", TargetType = "GitHub", TargetServer = "https://api.github.com",
            TargetToken = "target-token", TargetOrgName = "target"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.IsFalse((await context.MirrorConfigurations.SingleAsync(c => c.FromOrgName == "new-source"))
            .IncludePrivateRepositories, "New relationships must use the new default, including after an upgrade.");
    }

    [TestMethod]
    public void TestMySqlMigrations()
    {
        var options = new DbContextOptionsBuilder<MySqlContext>()
            .UseMySql("Server=localhost;Database=test;Uid=root;Pwd=password;", new MySqlServerVersion(new Version(8, 0, 31)))
            .Options;
        using var context = new MySqlContext(options);
        var hasPendingChanges = context.Database.HasPendingModelChanges();
        Assert.IsFalse(hasPendingChanges, "There are pending model changes for MySql. Please run 'dotnet ef migrations add' for MySql.");
    }
}
