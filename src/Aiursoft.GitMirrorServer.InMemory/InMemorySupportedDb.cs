using Aiursoft.DbTools;
using Aiursoft.DbTools.InMemory;
using Aiursoft.GitMirrorServer.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.GitMirrorServer.InMemory;

public class InMemorySupportedDb : SupportedDatabaseType<GitMirrorServerDbContext>
{
    public override string DbType => "InMemory";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurInMemoryDb<InMemoryContext>();
    }

    public override GitMirrorServerDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InMemoryContext>();
    }
}
