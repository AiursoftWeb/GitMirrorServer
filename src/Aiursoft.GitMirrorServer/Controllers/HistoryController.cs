using Aiursoft.GitMirrorServer.Authorization;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Models.HistoryViewModels;
using Microsoft.AspNetCore.Authorization;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Controllers;

[Authorize]
public class HistoryController(GitMirrorServerDbContext dbContext) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanViewMirrorStatus)]
    [RenderInNavBar(
        NavGroupName = "Mirroring",
        NavGroupOrder = 5,
        CascadedLinksGroupName = "Mirrors",
        CascadedLinksIcon = "git-merge",
        CascadedLinksOrder = 1,
        LinkText = "History",
        LinkOrder = 2)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var model = new IndexViewModel
        {
            History = [],
            TotalCount = await dbContext.MirrorJobExecutions.CountAsync()
        };
        model.CurrentPage = Math.Clamp(page, 1, model.TotalPages);
        model.History = await dbContext.MirrorJobExecutions
            .AsNoTracking()
            .OrderByDescending(j => j.StartTime)
            .ThenByDescending(j => j.Id)
            .Skip((model.CurrentPage - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();
        return this.StackView(model);
    }

    [Authorize(Policy = AppPermissionNames.CanViewMirrorStatus)]
    public async Task<IActionResult> Details(Guid id)
    {
        var job = await dbContext.MirrorJobExecutions
            .Include(j => j.RepoExecutions)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound();
        }

        return this.StackView(new DetailsViewModel { Job = job });
    }
}