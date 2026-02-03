using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.DashboardViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Dashboard";
    }

    public int TotalMirrors { get; set; }
    public DateTime? LastRunTime { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public int LastRunSuccessCount { get; set; }
    public int LastRunFailureCount { get; set; }
    public int TotalHistoryCount { get; set; }
}