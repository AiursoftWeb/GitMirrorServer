using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Models.GlobalSettingsViewModels;

public class EditViewModel
{
    [Required(ErrorMessage = "Key is required.")]
    [Display(Name = "Key")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "Value")]
    public string? Value { get; set; }
}
