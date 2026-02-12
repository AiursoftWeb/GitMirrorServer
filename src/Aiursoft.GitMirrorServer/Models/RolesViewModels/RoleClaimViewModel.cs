using System.ComponentModel.DataAnnotations;

namespace Aiursoft.GitMirrorServer.Models.RolesViewModels;

public class RoleClaimViewModel
{
    [Display(Name = "Key")]
    public required string Key { get; set; }

    [Display(Name = "Name")]
    public required string Name { get; set; }

    [Display(Name = "Description")]
    public required string Description { get; set; }

    [Display(Name = "Is selected")]
    public bool IsSelected { get; set; }
}
