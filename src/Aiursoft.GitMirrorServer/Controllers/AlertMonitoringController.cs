using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aiursoft.GitMirrorServer.Authorization;
using Aiursoft.GitMirrorServer.Configuration;
using Aiursoft.GitMirrorServer.Models.Monitoring;
using Aiursoft.GitMirrorServer.Services;
using Aiursoft.GitMirrorServer.Services.Monitoring;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Aiursoft.GitMirrorServer.Controllers;

[Authorize(Policy = AppPermissionNames.CanManageAlertCenterIntegration)]
[LimitPerMin]
public sealed class AlertMonitoringController(GlobalSettingsService settings, MirrorMonitor monitor,
    IStringLocalizer<AlertMonitoringController> localizer) : Controller
{
    private static string Version(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    [RenderInNavBar(NavGroupName = "Administration", NavGroupOrder = 9999, CascadedLinksGroupName = "Monitoring",
        CascadedLinksIcon = "bell", CascadedLinksOrder = 2, LinkText = "AlertCenter monitoring", LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var json = await settings.GetSettingValueAsync(SettingsMap.AlertCenterRules);
        var model = new MonitoringViewModel
        {
            Version = Version(json), RulesReadOnly = settings.IsOverriddenByConfig(SettingsMap.AlertCenterRules),
            AutoMitigateReadOnly = settings.IsOverriddenByConfig(SettingsMap.AlertCenterAutoMitigate),
            Enabled = await settings.GetBoolSettingAsync(SettingsMap.AlertCenterEnabled),
            ApiKeyConfigured = !string.IsNullOrWhiteSpace(await settings.GetSettingValueAsync(SettingsMap.AlertCenterApiKey)),
            AutoMitigate = await settings.GetBoolSettingAsync(SettingsMap.AlertCenterAutoMitigate)
        };
        try { model.Rules = MirrorMonitoringRule.Parse(json); }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { model.InvalidRules = true; }
        model.Repositories = monitor.Snapshot().OrderBy(state => state.Source).ThenBy(state => state.Repository).ToList();
        return this.StackView(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRule(string version, Guid? id, int failureCount, string severity, bool isEnabled)
    {
        var candidate = new MirrorMonitoringRule { FailureCount = failureCount, Severity = severity, IsEnabled = isEnabled };
        if (!ModelState.IsValid || !candidate.IsValid())
            return BadRequest(localizer["Select a positive failed-round threshold from 1 to 1000000 and sev0 to sev4."].Value);
        return await UpdateRules(version, rules =>
        {
            if (id.HasValue)
            {
                var index = rules.FindIndex(rule => rule.Id == id);
                if (index < 0) return false;
                candidate.Id = id.Value;
                rules[index] = candidate;
            }
            else rules.Add(candidate);
            return true;
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteRule(string version, Guid id) => UpdateRules(version, rules => rules.RemoveAll(rule => rule.Id == id) > 0);

    private async Task<IActionResult> UpdateRules(string version, Func<List<MirrorMonitoringRule>, bool> update)
    {
        await monitor.SyncGate.WaitAsync();
        try
        {
            var json = await settings.GetSettingValueAsync(SettingsMap.AlertCenterRules);
            if (Version(json) != version) return Conflict(localizer["The monitoring rules changed. Refresh before saving."].Value);
            var rules = MirrorMonitoringRule.Parse(json);
            if (!update(rules)) return NotFound();
            var updated = JsonSerializer.Serialize(rules);
            MirrorMonitoringRule.Parse(updated);
            await settings.UpdateSettingAsync(SettingsMap.AlertCenterRules, updated);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return Conflict(localizer["The rules are invalid or overridden by configuration. Check Global Settings."].Value);
        }
        finally { monitor.SyncGate.Release(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRecovery(bool autoMitigate)
    {
        if (!ModelState.IsValid) return BadRequest();
        try { await settings.UpdateSettingAsync(SettingsMap.AlertCenterAutoMitigate, autoMitigate.ToString()); }
        catch (InvalidOperationException) { return Conflict(localizer["This setting is overridden by configuration."].Value); }
        return RedirectToAction(nameof(Index));
    }
}
