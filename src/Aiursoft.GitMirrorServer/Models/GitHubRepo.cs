namespace Aiursoft.GitMirrorServer.Models;

public class GitHubRepo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Private { get; set; }

    public bool Archived { get; set; }
}