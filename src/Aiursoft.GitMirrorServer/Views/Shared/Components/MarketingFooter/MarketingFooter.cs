using Microsoft.AspNetCore.Mvc;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Views.Shared.Components.MarketingFooter;

public class MarketingFooter(GlobalSettingsService globalSettingsService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingFooterViewModel? model = null)
    {
        model ??= new MarketingFooterViewModel();
        model.BrandName = await globalSettingsService.GetSettingValueAsync("BrandName");
        model.BrandHomeUrl = await globalSettingsService.GetSettingValueAsync("BrandHomeUrl");
        model.Icp = await globalSettingsService.GetSettingValueAsync("Icp");
        return View(model);
    }
}
