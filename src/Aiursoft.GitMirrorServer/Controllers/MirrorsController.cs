using Aiursoft.GitMirrorServer.Authorization;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.GitMirrorServer.Services.BackgroundJobs;
using Aiursoft.GitMirrorServer.Models.MirrorsViewModels;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Controllers;

[Authorize]
public class MirrorsController(
    GitMirrorServerDbContext dbContext,
    BackgroundJobQueue backgroundJobQueue) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanViewMirrorStatus)]
    [RenderInNavBar(
        NavGroupName = "Mirroring",
        NavGroupOrder = 5,
        CascadedLinksGroupName = "Mirrors",
        CascadedLinksIcon = "git-merge",
        CascadedLinksOrder = 1,
        LinkText = "Mirrors List",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var mirrors = await dbContext.MirrorConfigurations.ToListAsync();
        return this.StackView(new IndexViewModel { Mirrors = mirrors });
    }

    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    public IActionResult Create()
    {
        return this.StackView(new EditorViewModel { IsCreate = true, PageTitle = "Create Mirror" });
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EditorViewModel model)
    {
        if (!ModelState.IsValid)
        {
             model.IsCreate = true;
             model.PageTitle = "Create Mirror";
             return this.StackView(model);
        }

        var mirror = new MirrorConfiguration
        {
            FromType = model.FromType,
            FromServer = model.FromServer,
            FromToken = model.FromToken,
            FromOrgName = model.FromOrgName,
            OrgOrUser = model.OrgOrUser,
            TargetType = model.TargetType,
            TargetServer = model.TargetServer,
            TargetToken = model.TargetToken,
            TargetOrgName = model.TargetOrgName
        };

        dbContext.MirrorConfigurations.Add(mirror);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var mirror = await dbContext.MirrorConfigurations.FindAsync(id);
        if (mirror == null)
        {
            return NotFound();
        }
        return this.StackView(new EditorViewModel
        {
            Id = mirror.Id,
            FromType = mirror.FromType,
            FromServer = mirror.FromServer,
            FromToken = mirror.FromToken,
            FromOrgName = mirror.FromOrgName,
            OrgOrUser = mirror.OrgOrUser,
            TargetType = mirror.TargetType,
            TargetServer = mirror.TargetServer,
            TargetToken = mirror.TargetToken,
            TargetOrgName = mirror.TargetOrgName,
            IsCreate = false,
            PageTitle = "Edit Mirror"
        });
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.IsCreate = false;
            model.PageTitle = "Edit Mirror";
            return this.StackView(model);
        }

        var mirror = await dbContext.MirrorConfigurations.FindAsync(model.Id);
        if (mirror == null)
        {
            return NotFound();
        }

        mirror.FromType = model.FromType;
        mirror.FromServer = model.FromServer;
        mirror.FromToken = model.FromToken;
        mirror.FromOrgName = model.FromOrgName;
        mirror.OrgOrUser = model.OrgOrUser;
        mirror.TargetType = model.TargetType;
        mirror.TargetServer = model.TargetServer;
        mirror.TargetToken = model.TargetToken;
        mirror.TargetOrgName = model.TargetOrgName;

        dbContext.MirrorConfigurations.Update(mirror);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var mirror = await dbContext.MirrorConfigurations.FindAsync(id);
        if (mirror != null)
        {
            dbContext.MirrorConfigurations.Remove(mirror);
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    [ValidateAntiForgeryToken]
    public IActionResult Trigger()
    {
        backgroundJobQueue.QueueWithDependency<MirrorService>(
            queueName: "MirrorQueue",
            jobName: "Manually Triggered Mirror Job",
            job: async (service) => await service.RunMirrorAsync()
        );
        return RedirectToAction("Index", "History");
    }
}