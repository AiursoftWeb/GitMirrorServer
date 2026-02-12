using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Models.UsersViewModels;

public class UserRoleViewModel
{
    [Display(Name = "Role name")]
    public required string RoleName { get; set; }

    [Display(Name = "Is selected")]
    public bool IsSelected { get; set; }
}
