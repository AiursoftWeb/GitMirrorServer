using System.ComponentModel.DataAnnotations;
using Aiursoft.GitMirrorServer.Entities;

namespace Aiursoft.GitMirrorServer.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    [Display(Name = "User")]
    public required User User { get; set; }

    [Display(Name = "Roles")]
    public required IList<string> Roles { get; set; }
}
