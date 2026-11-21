using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

public class HomeController(DashboardService dashboard) : Controller
{
    public async Task<IActionResult> Index() => View(await dashboard.LayAsync());

    [AllowAnonymous]
    public IActionResult Error() => View(model: Activity.Current?.Id ?? HttpContext.TraceIdentifier);
}
