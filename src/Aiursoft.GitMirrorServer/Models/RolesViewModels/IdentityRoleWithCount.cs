using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.GitMirrorServer.Models.RolesViewModels;

public class IdentityRoleWithCount
{
    public required IdentityRole Role { get; init; }

    [Display(Name = "User count")]
    public required int UserCount { get; init; }

    [Display(Name = "Permission count")]
    public required int PermissionCount { get; init; }

    [Display(Name = "Permission names")]
    public required List<string> PermissionNames { get; init; }
}
