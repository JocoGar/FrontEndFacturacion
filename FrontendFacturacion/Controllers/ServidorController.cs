using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class ServidorController : Controller
    {
        private readonly FacturacionApiService _api;

        public ServidorController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Hostname"] = Environment.MachineName;
            ViewData["FechaServidor"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            ViewData["ApiDisponible"] = await _api.ApiDisponibleAsync();

            return View();
        }
    }
}