using Microsoft.AspNetCore.Mvc;

namespace SistemaVentas.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Error()
    {
        return Content("Ocurrió un error. Intenta nuevamente.");
    }
}
