using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.HistoryViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Job Details";
    }

    public required MirrorJobExecution Job { get; set; }
}
