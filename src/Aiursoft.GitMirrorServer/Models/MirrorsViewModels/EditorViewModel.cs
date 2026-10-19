using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.MirrorsViewModels;

public class EditorViewModel : UiStackLayoutViewModel
{
    [Obsolete("Only for model binding")]
    public EditorViewModel()
    {
    }

    public EditorViewModel(string pageTitle)
    {
        PageTitle = pageTitle;
    }

    public Guid Id { get; set; }

    [MaxLength(50)]
    [Required(ErrorMessage = "The From Type is required.")]
    [Display(Name = "Source Git provider")]
    public string FromType { get; set; } = "GitLab";

    [MaxLength(1000)]
    [Required(ErrorMessage = "The From Server URL is required.")]
    [Display(Name = "Source server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string FromServer { get; set; } = "https://gitlab.com";

    [MaxLength(1000)]
    [Display(Name = "Source access token")]
    public string? FromToken { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required(ErrorMessage = "The From Organization/User Name is required.")]
    [Display(Name = "Source organization or username")]
    public string FromOrgName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Required(ErrorMessage = "Please specify if this is an Organization or a User.")]
    [Display(Name = "Source account type")]
    public string OrgOrUser { get; set; } = "Org";

    [MaxLength(50)]
    [Required(ErrorMessage = "The Target Type is required.")]
    [Display(Name = "Target Git provider")]
    public string TargetType { get; set; } = "GitHub";

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Server URL is required.")]
    [Display(Name = "Target server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string TargetServer { get; set; } = "https://api.github.com";

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Token is required.")]
    [Display(Name = "Target access token")]
    public string TargetToken { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required(ErrorMessage = "The Target Organization Name is required.")]
    [Display(Name = "Target organization or username")]
    public string TargetOrgName { get; set; } = string.Empty;


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

    [Display(Name = "I confirm that Private source repositories may be published to Public GitHub repositories.")]
    public bool ConfirmPrivatePublication { get; set; }

    public bool IsCreate { get; set; }
}
