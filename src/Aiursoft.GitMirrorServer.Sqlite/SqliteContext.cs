using System.Diagnostics.CodeAnalysis;
using Aiursoft.GitMirrorServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : GitMirrorServerDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
