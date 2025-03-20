namespace Aiursoft.GitMirrorServer.Models;

public class GitRepository
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }

    public bool Archived { get; set; }
}
