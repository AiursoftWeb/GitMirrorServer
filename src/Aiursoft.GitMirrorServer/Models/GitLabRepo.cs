namespace Aiursoft.GitMirrorServer.Models;

public class GitLabRepo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public bool Archived { get; set; }
}