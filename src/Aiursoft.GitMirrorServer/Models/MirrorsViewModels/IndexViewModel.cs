using System.ComponentModel.DataAnnotations;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.MirrorsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Mirrors List";
    }

    [Display(Name = "Mirrors")]
    public required List<MirrorConfiguration> Mirrors { get; set; }
}
