using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class FacturasController : Controller
    {
        private readonly FacturacionApiService _api;

        public FacturasController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var facturas = await _api.ObtenerFacturasAsync();
            return View(facturas);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Clientes = await _api.ObtenerClientesAsync();
            ViewBag.Productos = await _api.ObtenerProductosAsync();
            ViewBag.Usuarios = await _api.ObtenerUsuariosAsync();

            return View();
        }

        public async Task<IActionResult> Details(int id = 1)
        {
            var detalle = await _api.ObtenerDetalleFacturaAsync(id);
            return View(detalle);
        }
    }
}