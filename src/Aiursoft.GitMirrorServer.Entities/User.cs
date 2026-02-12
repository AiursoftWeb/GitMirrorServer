using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.GitMirrorServer.Entities;

public class User : IdentityUser
{
    public const string DefaultAvatarPath = "avatar/default-avatar.jpg";

    [MaxLength(30)]
    [MinLength(2)]
    [Required(ErrorMessage = "Display name is required.")]
    [Display(Name = "Display name")]
    public required string DisplayName { get; set; }

    [MaxLength(150)] 
    [MinLength(2)] 
    [Display(Name = "Avatar relative path")]
    public string AvatarRelativePath { get; set; } = DefaultAvatarPath;

    [Display(Name = "Creation time")]
    public DateTime CreationTime { get; init; } = DateTime.UtcNow;
}
