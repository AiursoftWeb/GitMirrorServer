namespace Aiursoft.GitMirrorServer.Models.Configuration;

public class MirrorConfig
{
    public required string FromType { get; init; }
    public required string? FromToken { get; init; }
    public required string FromServer { get; init; }
    public required string FromOrgName { get; init; }
    public required string OrgOrUser { get; init; }
    public required string TargetType { get; init; }
    public required string TargetServer { get; init; }
    public required string TargetToken { get; init; }
    public required string TargetOrgName { get; init; }
}