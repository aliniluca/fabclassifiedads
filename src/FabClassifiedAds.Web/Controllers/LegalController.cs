using Microsoft.AspNetCore.Mvc;

namespace FabClassifiedAds.Web.Controllers;

public class LegalController : Controller
{
    [HttpGet("/privacy")]
    [HttpGet("/confidentialitate")]
    public IActionResult Privacy() => View();

    [HttpGet("/terms")]
    [HttpGet("/termeni")]
    public IActionResult Terms() => View();

    [HttpGet("/cookies")]
    public IActionResult Cookies() => View();
}
