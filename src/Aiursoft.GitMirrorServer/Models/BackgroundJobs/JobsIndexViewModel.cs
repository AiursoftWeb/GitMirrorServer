using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    [Display(Name = "All recent jobs")]
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
