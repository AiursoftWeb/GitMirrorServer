using System.ComponentModel.DataAnnotations;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.HistoryViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Job Details";
    }

    [Display(Name = "Job")]
    public required MirrorJobExecution Job { get; set; }
}
