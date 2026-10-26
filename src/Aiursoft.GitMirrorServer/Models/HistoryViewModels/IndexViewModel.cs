using System.ComponentModel.DataAnnotations;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.HistoryViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Job History";
    }

    public int CurrentPage { get; set; } = 1;
    public int PageSize => 50;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    [Display(Name = "History")]
    public required List<MirrorJobExecution> History { get; set; }
}
