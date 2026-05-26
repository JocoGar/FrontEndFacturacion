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
            var fuenteDatos = await _api.ObtenerFuenteDatosAsync();

            ViewData["Hostname"] = Environment.MachineName;
            ViewData["FechaServidor"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            ViewData["ApiDisponible"] = fuenteDatos.Origen == "DISPONIBLE";
            ViewBag.FuenteDatos = fuenteDatos;

            return View();
        }
    }
}