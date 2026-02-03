using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.HistoryViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Job History";
    }

    public required List<MirrorJobExecution> History { get; set; }
}
