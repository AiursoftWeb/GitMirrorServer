using Aiursoft.GitMirrorServer.Authorization;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Models.DashboardViewModels;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Controllers;

[LimitPerMin]
[Authorize(Policy = AppPermissionNames.CanViewMirrorStatus)]
public class DashboardController(GitMirrorServerDbContext dbContext) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Index",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel
        {
            TotalMirrors = await dbContext.MirrorConfigurations.CountAsync(),
            TotalHistoryCount = await dbContext.MirrorJobExecutions.CountAsync()
        };

        var lastRun = await dbContext.MirrorJobExecutions
            .OrderByDescending(j => j.StartTime)
            .FirstOrDefaultAsync();

        if (lastRun != null)
        {
            model.LastRunTime = lastRun.StartTime;
            if (lastRun.EndTime.HasValue)
            {
                model.LastRunDuration = lastRun.EndTime.Value - lastRun.StartTime;
            }
            model.LastRunSuccessCount = lastRun.SuccessCount;
            model.LastRunFailureCount = lastRun.FailureCount;
        }

        return this.StackView(model);
    }
}
