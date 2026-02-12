using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Entities;

public class GlobalSetting
{
    [Key]
    [Required(ErrorMessage = "Key is required.")]
    [Display(Name = "Key")]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string Key { get; set; }

    [Display(Name = "Value")]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? Value { get; set; }
}
