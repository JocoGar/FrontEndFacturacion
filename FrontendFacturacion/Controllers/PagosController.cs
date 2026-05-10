using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class PagosController : Controller
    {
        private readonly FacturacionApiService _api;

        public PagosController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var pagos = await _api.ObtenerPagosAsync();
            return View(pagos);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Facturas = await _api.ObtenerFacturasAsync();
            return View();
        }
    }
}