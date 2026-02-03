using Aiursoft.GitMirrorServer.Authorization;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.GitMirrorServer.Models.RolesViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Role Details";
    }

    public required IdentityRole Role { get; set; }

    public required List<PermissionDescriptor> Permissions { get; set; }

    public required IList<User> UsersInRole { get; set; }
}
