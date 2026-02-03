using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required]
    public required string Theme { get; set; }
}
