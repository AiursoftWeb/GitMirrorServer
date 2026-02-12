using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.DashboardViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Dashboard";
    }

    [Display(Name = "Total mirrors")]
    public int TotalMirrors { get; set; }

    [Display(Name = "Last run time")]
    public DateTime? LastRunTime { get; set; }

    [Display(Name = "Last run duration")]
    public TimeSpan? LastRunDuration { get; set; }

    [Display(Name = "Last run success count")]
    public int LastRunSuccessCount { get; set; }

    [Display(Name = "Last run failure count")]
    public int LastRunFailureCount { get; set; }

    [Display(Name = "Total history count")]
    public int TotalHistoryCount { get; set; }
}