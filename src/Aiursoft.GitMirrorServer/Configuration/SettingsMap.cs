using Aiursoft.GitMirrorServer.Models;

namespace Aiursoft.GitMirrorServer.Configuration;

public class SettingsMap
{
    public const string AlertCenterEnabled = "AlertCenter_Enabled";
    public const string AlertCenterEndpoint = "AlertCenter_Endpoint";
    public const string AlertCenterApiKey = "AlertCenter_ApiKey";
    public const string AlertCenterPublicBaseUrl = "AlertCenter_PublicBaseUrl";
    public const string AlertCenterRules = "AlertCenter_Rules";
    public const string AlertCenterAutoMitigate = "AlertCenter_AutoMitigate";

    public const string ProjectName = "ProjectName";
    public const string BrandName = "BrandName";
    public const string BrandHomeUrl = "BrandHomeUrl";
    public const string ProjectLogo = "ProjectLogo";
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string Icp = "Icp";

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = AlertCenterEnabled, Name = Localizer["Enable AlertCenter integration"],
            Description = Localizer["Synchronize repository mirror incidents with AlertCenter."],
            Type = SettingType.Bool, DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterEndpoint, Name = Localizer["AlertCenter endpoint"],
            Description = Localizer["The HTTP(S) base URL of AlertCenter."],
            Type = SettingType.Text, DefaultValue = "https://alertcenter.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterApiKey, Name = Localizer["AlertCenter application API key"],
            Description = Localizer["A dedicated application key with query, create, change severity and mitigate permissions."],
            Type = SettingType.Secret, DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterPublicBaseUrl, Name = Localizer["GitMirrorServer public URL"],
            Description = Localizer["The public HTTP(S) base URL used in incident descriptions."],
            Type = SettingType.Text, DefaultValue = "https://gitmirror.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterRules, Name = Localizer["Mirror monitoring rules"],
            Description = Localizer["JSON monitoring rules. Use the AlertCenter monitoring page to edit them."],
            Type = SettingType.Text, DefaultValue = Services.Monitoring.MirrorMonitoringRule.DefaultJson
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterAutoMitigate, Name = Localizer["Automatically mitigate recovered repositories"],
            Description = Localizer["Mitigate after a successful mirror round. Final resolution remains manual."],
            Type = SettingType.Bool, DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = ProjectName,
            Name = Localizer["Project Name"],
            Description = Localizer["The name of the project displayed in the frontend."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft GitMirrorServer"
        },
        new GlobalSettingDefinition
        {
            Key = BrandName,
            Name = Localizer["Brand Name"],
            Description = Localizer["The brand name displayed in the footer."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft"
        },
        new GlobalSettingDefinition
        {
            Key = BrandHomeUrl,
            Name = Localizer["Brand Home URL"],
            Description = Localizer[" The link to the brand's home page."],
            Type = SettingType.Text,
            DefaultValue = "https://www.aiursoft.com/"
        },
        new GlobalSettingDefinition
        {
            Key = ProjectLogo,
            Name = Localizer["Project Logo"],
            Description = Localizer["The logo of the project displayed in the navbar and footer. Support jpg, png, svg."],
            Type = SettingType.File,
            DefaultValue = "",
            Subfolder = "project-logo",
            AllowedExtensions = "jpg png svg",
            MaxSizeInMb = 5
        },
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Name = Localizer["Allow User Adjust Nickname"],
            Description = Localizer["Allow users to adjust their nickname in the profile management page."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = Icp,
            Name = Localizer["ICP Number"],
            Description = Localizer["The ICP license number for China mainland users. Leave empty to hide."],
            Type = SettingType.Text,
            DefaultValue = ""
        },
    };
}
