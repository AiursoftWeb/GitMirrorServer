using Microsoft.AspNetCore.Mvc;
using Aiursoft.WebTools.Attributes;

namespace Aiursoft.GitMirrorServer.Controllers;

public class HomeController : Controller
{
    [LimitPerMin(10)]
    public IActionResult Index()
    {
        return View();
    }
}
