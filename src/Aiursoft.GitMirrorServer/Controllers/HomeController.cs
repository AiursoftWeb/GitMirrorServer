using Microsoft.AspNetCore.Mvc;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace Aiursoft.GitMirrorServer.Controllers;

[Authorize]
public class HomeController : Controller
{
    [LimitPerMin(100)]
    public IActionResult Index()
    {
        return View();
    }
}
