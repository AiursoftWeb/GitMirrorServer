using Microsoft.AspNetCore.Mvc;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Views.Shared.Components.MarketingNavbar;

public class MarketingNavbar(GlobalSettingsService globalSettingsService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingNavbarViewModel? model = null)
    {
        model ??= new MarketingNavbarViewModel();
        model.ProjectName = await globalSettingsService.GetSettingValueAsync("ProjectName");
        return View(model);
    }
}
