using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.GitMirrorServer.Controllers;

public class PingController : ControllerBase
{
    [AiurNoCache]
    public IActionResult Index()
    {
        return Ok(new List<object>());
    }
}
