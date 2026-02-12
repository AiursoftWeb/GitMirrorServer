using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.RolesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Roles";
    }

    [Display(Name = "Roles")]
    public required List<IdentityRoleWithCount> Roles { get; init; }
}
