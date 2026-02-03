using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Aiursoft.DbTools.Sqlite;
using Aiursoft.GitMirrorServer.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.GitMirrorServer.Sqlite;

[ExcludeFromCodeCoverage]
public class SqliteSupportedDb(bool allowCache, bool splitQuery) : SupportedDatabaseType<GitMirrorServerDbContext>
{
    public override string DbType => "Sqlite";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurSqliteWithCache<SqliteContext>(
            connectionString,
            splitQuery: splitQuery,
            allowCache: allowCache);
    }

    public override GitMirrorServerDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SqliteContext>();
    }
}
