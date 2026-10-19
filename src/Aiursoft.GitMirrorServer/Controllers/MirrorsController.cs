using Aiursoft.Canon.BackgroundJobs;
using Aiursoft.GitMirrorServer.Authorization;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.GitMirrorServer.Models.MirrorsViewModels;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Aiursoft.GitMirrorServer.Controllers;

[Authorize]
public class MirrorsController(
    GitMirrorServerDbContext dbContext,
    BackgroundJobRegistry jobRegistry,
    IStringLocalizer<MirrorsController> localizer) : Controller
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
        return this.StackView(new EditorViewModel("Create Mirror") { IsCreate = true });
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EditorViewModel model)
    {
        ValidatePolicy(model);
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
            TargetOrgName = model.TargetOrgName,
            IncludePublicRepositories = model.IncludePublicRepositories,
            IncludeInternalRepositories = model.IncludeInternalRepositories,
            IncludePrivateRepositories = model.IncludePrivateRepositories,
            IncludeArchivedRepositories = model.IncludeArchivedRepositories,
            NewGitHubRepositoriesPrivate = model.NewGitHubRepositoriesPrivate
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
        return this.StackView(new EditorViewModel("Edit Mirror")
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
            IncludePublicRepositories = mirror.IncludePublicRepositories,
            IncludeInternalRepositories = mirror.IncludeInternalRepositories,
            IncludePrivateRepositories = mirror.IncludePrivateRepositories,
            IncludeArchivedRepositories = mirror.IncludeArchivedRepositories,
            NewGitHubRepositoriesPrivate = mirror.NewGitHubRepositoriesPrivate,
            IsCreate = false
        });
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanManageMirrorTargets)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditorViewModel model)
    {
        ValidatePolicy(model);
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
        mirror.IncludePublicRepositories = model.IncludePublicRepositories;
        mirror.IncludeInternalRepositories = model.IncludeInternalRepositories;
        mirror.IncludePrivateRepositories = model.IncludePrivateRepositories;
        mirror.IncludeArchivedRepositories = model.IncludeArchivedRepositories;
        mirror.NewGitHubRepositoriesPrivate = model.NewGitHubRepositoriesPrivate;

        dbContext.MirrorConfigurations.Update(mirror);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private void ValidatePolicy(EditorViewModel model)
    {
        var gitLabSource = string.Equals(model.FromType, "GitLab", StringComparison.OrdinalIgnoreCase);
        if (!model.IncludePublicRepositories && !model.IncludePrivateRepositories &&
            !(gitLabSource && model.IncludeInternalRepositories))
        {
            ModelState.AddModelError(string.Empty, localizer["Select at least one source repository visibility."]);
        }
        if ((model.IncludePrivateRepositories || (gitLabSource && model.IncludeInternalRepositories)) &&
            string.IsNullOrWhiteSpace(model.FromToken))
        {
            ModelState.AddModelError(nameof(model.FromToken),
                localizer["A source access token is required to include Internal or Private repositories."]);
        }
        if (string.Equals(model.TargetType, "GitHub", StringComparison.OrdinalIgnoreCase) &&
            model.IncludePrivateRepositories && !model.NewGitHubRepositoriesPrivate && !model.ConfirmPrivatePublication)
        {
            ModelState.AddModelError(nameof(model.ConfirmPrivatePublication),
                localizer["Confirm Public publication of Private source repositories, or choose Private for new GitHub repositories."]);
        }
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
        jobRegistry.TriggerNow(nameof(Services.BackgroundJobs.MirrorJob));
        return RedirectToAction("Index", "History");
    }
}
