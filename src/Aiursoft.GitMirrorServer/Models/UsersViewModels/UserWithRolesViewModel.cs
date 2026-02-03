using Aiursoft.GitMirrorServer.Entities;

namespace Aiursoft.GitMirrorServer.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    public required User User { get; set; }
    public required IList<string> Roles { get; set; }
}
