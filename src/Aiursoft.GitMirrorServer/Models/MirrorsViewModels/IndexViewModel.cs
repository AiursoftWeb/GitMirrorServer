using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.MirrorsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Mirrors List";
    }
    public required List<MirrorConfiguration> Mirrors { get; set; }
}
