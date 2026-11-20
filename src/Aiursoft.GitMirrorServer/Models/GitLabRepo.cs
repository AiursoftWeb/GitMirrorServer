using System.Text.Json.Serialization;

namespace Aiursoft.GitMirrorServer.Models;

public class GitLabRepo
{
    public long Id { get; init; }
    public required string Path { get; init; }
    public string? Description { get; init; }
    public required string Visibility { get; init; }
    public bool Archived { get; init; }

    [JsonPropertyName("marked_for_deletion_on")]
    public string? MarkedForDeletionOn { get; init; }

    [JsonPropertyName("marked_for_deletion_at")]
    public string? MarkedForDeletionAt { get; init; }

    // GitLab can omit the deletion fields from listings while still exposing the renamed path.
    public bool IsPendingDeletion =>
        !string.IsNullOrWhiteSpace(MarkedForDeletionOn) ||
        !string.IsNullOrWhiteSpace(MarkedForDeletionAt) ||
        (Id > 0 && Path.EndsWith($"-deletion_scheduled-{Id}", StringComparison.Ordinal));
}
