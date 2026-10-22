using Aiursoft.GitMirrorServer.Services.Monitoring;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.Monitoring;

public sealed class MonitoringViewModel : UiStackLayoutViewModel
{
    public MonitoringViewModel() { PageTitle = "AlertCenter monitoring"; }
    public List<MirrorMonitoringRule> Rules { get; set; } = [];
    public List<MirrorSnapshot> Repositories { get; set; } = [];
    public string Version { get; set; } = string.Empty;
    public bool RulesReadOnly { get; set; }
    public bool AutoMitigateReadOnly { get; set; }
    public bool Enabled { get; set; }
    public bool ApiKeyConfigured { get; set; }
    public bool AutoMitigate { get; set; }
    public bool InvalidRules { get; set; }
}
