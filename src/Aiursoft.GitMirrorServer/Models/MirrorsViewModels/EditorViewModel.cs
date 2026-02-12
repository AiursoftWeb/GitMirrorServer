using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.MirrorsViewModels;

public class EditorViewModel : UiStackLayoutViewModel
{
    public EditorViewModel()
    {
        PageTitle = "Edit Mirror";
    }

    public Guid Id { get; set; }

    [MaxLength(50)]
    [Required(ErrorMessage = "The From Type is required.")]
    [Display(Name = "From Type")]
    public string FromType { get; set; } = "GitLab";

    [MaxLength(1000)]
    [Required(ErrorMessage = "The From Server URL is required.")]
    [Display(Name = "From Server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string FromServer { get; set; } = "https://gitlab.com";

    [MaxLength(1000)]
    [Display(Name = "From Token")]
    public string? FromToken { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required(ErrorMessage = "The From Organization/User Name is required.")]
    [Display(Name = "From Organization/User")]
    public string FromOrgName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Required(ErrorMessage = "Please specify if this is an Organization or a User.")]
    [Display(Name = "Source Type (Org/User)")]
    public string OrgOrUser { get; set; } = "Org";

    [MaxLength(50)]
    [Required(ErrorMessage = "The Target Type is required.")]
    [Display(Name = "Target Type")]
    public string TargetType { get; set; } = "GitHub";

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Server URL is required.")]
    [Display(Name = "Target Server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string TargetServer { get; set; } = "https://api.github.com";

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Token is required.")]
    [Display(Name = "Target Token")]
    public string TargetToken { get; set; } = string.Empty;

    [MaxLength(100)]
    [Required(ErrorMessage = "The Target Organization Name is required.")]
    [Display(Name = "Target Organization")]
    public string TargetOrgName { get; set; } = string.Empty;

    public bool IsCreate { get; set; }
}
