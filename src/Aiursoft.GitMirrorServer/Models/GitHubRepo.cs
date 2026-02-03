namespace Aiursoft.GitMirrorServer.Models;

public class GitHubRepo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool Private { get; init; }
    public bool Archived { get; init; }
}
