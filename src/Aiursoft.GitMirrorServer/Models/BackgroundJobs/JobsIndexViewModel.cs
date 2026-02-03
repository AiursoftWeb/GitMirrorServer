using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
