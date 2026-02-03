namespace Aiursoft.GitMirrorServer.Models;

public class GitRepository
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsPrivate { get; init; }
    public bool Archived { get; init; }
}
