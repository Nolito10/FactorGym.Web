using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FactorGym.Web.Models;

using Microsoft.AspNetCore.Authorization;

namespace FactorGym.Web.Controllers;

[Authorize(Roles = "Administrador,Empleado")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
