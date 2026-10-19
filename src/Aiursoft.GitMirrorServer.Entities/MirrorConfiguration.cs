using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Entities;

public class MirrorConfiguration
{
    [Key]
    public Guid Id { get; init; }

    [MaxLength(50)]
    [Required(ErrorMessage = "The From Type is required.")]
    [Display(Name = "Source Git provider")]
    public required string FromType { get; set; }

    [MaxLength(1000)]
    [Required(ErrorMessage = "The From Server URL is required.")]
    [Display(Name = "Source server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public required string FromServer { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Source access token")]
    public string? FromToken { get; set; }

    [MaxLength(100)]
    [Required(ErrorMessage = "The From Organization/User Name is required.")]
    [Display(Name = "Source organization or username")]
    public required string FromOrgName { get; set; }

    [MaxLength(50)]
    [Required(ErrorMessage = "Please specify if this is an Organization or a User.")]
    [Display(Name = "Source account type")]
    public required string OrgOrUser { get; set; }

    [MaxLength(50)]
    [Required(ErrorMessage = "The Target Type is required.")]
    [Display(Name = "Target Git provider")]
    public required string TargetType { get; set; }

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Server URL is required.")]
    [Display(Name = "Target server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public required string TargetServer { get; set; }

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Token is required.")]
    [Display(Name = "Target access token")]
    public required string TargetToken { get; set; }

    [MaxLength(100)]
    [Required(ErrorMessage = "The Target Organization Name is required.")]
    [Display(Name = "Target organization or username")]
    public required string TargetOrgName { get; set; }


    [Display(Name = "Include Public repositories")]
    public bool IncludePublicRepositories { get; set; } = true;

    [Display(Name = "Include Internal repositories (GitLab)")]
    public bool IncludeInternalRepositories { get; set; } = true;

    [Display(Name = "Include Private repositories")]
    public bool IncludePrivateRepositories { get; set; }

    [Display(Name = "Include archived repositories")]
    public bool IncludeArchivedRepositories { get; set; }

    [Display(Name = "Visibility of new GitHub repositories")]
    public bool NewGitHubRepositoriesPrivate { get; set; }

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;
}