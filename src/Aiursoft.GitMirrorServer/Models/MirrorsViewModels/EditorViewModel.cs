using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.GitMirrorServer.Models.MirrorsViewModels;

public class EditorViewModel : UiStackLayoutViewModel
{
    public EditorViewModel()
    {
        PageTitle = "Edit Mirror";
    }
    public MirrorConfiguration Mirror { get; set; } = new MirrorConfiguration 
    { 
        FromType = "GitLab", 
        TargetType = "GitHub", 
        OrgOrUser = "Org",
        FromServer = "https://gitlab.com",
        TargetServer = "https://api.github.com",
        FromOrgName = "",
        TargetOrgName = "",
        FromToken = "",
        TargetToken = ""
    };
    public bool IsCreate { get; set; }
}
