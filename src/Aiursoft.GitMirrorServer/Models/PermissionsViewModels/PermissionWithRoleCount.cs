using System.ComponentModel.DataAnnotations;
using Aiursoft.GitMirrorServer.Authorization;

namespace Aiursoft.GitMirrorServer.Models.PermissionsViewModels;

public class PermissionWithRoleCount
{
    public required PermissionDescriptor Permission { get; init; }

    [Display(Name = "Role count")]
    public required int RoleCount { get; init; }

    [Display(Name = "User count")]
    public required int UserCount { get; init; }
}
