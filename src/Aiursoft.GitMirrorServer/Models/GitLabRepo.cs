namespace Aiursoft.GitMirrorServer.Models;

public class GitLabRepo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Visibility { get; init; }
    public bool Archived { get; init; }
}
