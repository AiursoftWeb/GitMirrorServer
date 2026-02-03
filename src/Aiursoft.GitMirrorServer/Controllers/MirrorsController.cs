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
        // Manual validation because model.Mirror might not be fully populated if we bound to EditorViewModel
        // Actually, if we use <input asp-for="Mirror.FromServer" />, it binds to model.Mirror.FromServer.
        // But validation errors might be tricky.
        
        // Let's re-bind or check validity.
        // If we bind to EditorViewModel, the Mirror property is populated.
        
        if (!ModelState.IsValid)
        {
             model.IsCreate = true;
             model.PageTitle = "Create Mirror";
             return this.StackView(model);
        }

        // We need to ensure Mirror properties are valid.
        // Manually trigger validation on Mirror object if needed, or trust ModelState if recursion works.
        // DataAnnotations on MirrorConfiguration should work.

        dbContext.MirrorConfigurations.Add(model.Mirror);
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
        return this.StackView(new EditorViewModel { Mirror = mirror, IsCreate = false, PageTitle = "Edit Mirror" });
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

        dbContext.MirrorConfigurations.Update(model.Mirror);
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