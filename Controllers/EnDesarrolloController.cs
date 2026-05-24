using Microsoft.AspNetCore.Mvc;

public class EnDesarrolloController : Controller
{
    public IActionResult Rastreo()
    {
        ViewBag.Modulo = "Rastreo en tiempo real";
        return View("~/Views/Shared/EnDesarrollo.cshtml");
    }

    public IActionResult Finanzas()
    {
        ViewBag.Modulo = "Módulo de Finanzas";
        return View("~/Views/Shared/EnDesarrollo.cshtml");
    }
}