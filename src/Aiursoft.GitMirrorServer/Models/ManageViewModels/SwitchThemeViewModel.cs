using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required(ErrorMessage = "Theme is required.")]
    [Display(Name = "Theme")]
    public required string Theme { get; set; }
}
