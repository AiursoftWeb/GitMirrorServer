using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Entities;

public class MirrorConfiguration
{
    [Key]
    public Guid Id { get; init; }

    [MaxLength(50)]
    [Required(ErrorMessage = "The From Type is required.")]
    [Display(Name = "From Type")]
    public required string FromType { get; set; }

    [MaxLength(1000)]
    [Required(ErrorMessage = "The From Server URL is required.")]
    [Display(Name = "From Server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public required string FromServer { get; set; }

    [MaxLength(1000)]
    [Display(Name = "From Token")]
    public string? FromToken { get; set; }

    [MaxLength(100)]
    [Required(ErrorMessage = "The From Organization/User Name is required.")]
    [Display(Name = "From Organization/User")]
    public required string FromOrgName { get; set; }

    [MaxLength(50)]
    [Required(ErrorMessage = "Please specify if this is an Organization or a User.")]
    [Display(Name = "Source Type (Org/User)")]
    public required string OrgOrUser { get; set; }

    [MaxLength(50)]
    [Required(ErrorMessage = "The Target Type is required.")]
    [Display(Name = "Target Type")]
    public required string TargetType { get; set; }

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Server URL is required.")]
    [Display(Name = "Target Server URL")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public required string TargetServer { get; set; }

    [MaxLength(1000)]
    [Required(ErrorMessage = "The Target Token is required.")]
    [Display(Name = "Target Token")]
    public required string TargetToken { get; set; }

    [MaxLength(100)]
    [Required(ErrorMessage = "The Target Organization Name is required.")]
    [Display(Name = "Target Organization")]
    public required string TargetOrgName { get; set; }

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;
}