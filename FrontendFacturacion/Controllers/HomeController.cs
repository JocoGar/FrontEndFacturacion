using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class HomeController : Controller
    {
        private readonly FacturacionApiService _api;

        public HomeController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var fuenteDatos = await _api.ObtenerFuenteDatosAsync();

            var productos = await _api.ObtenerProductosAsync();
            var clientes = await _api.ObtenerClientesAsync();
            var facturas = await _api.ObtenerFacturasAsync();
            var pagos = await _api.ObtenerPagosAsync();

            var model = new DashboardViewModel
            {
                TotalProductos = productos.Count,
                TotalClientes = clientes.Count,
                TotalFacturas = facturas.Count,
                FacturasPendientes = 0,
                TotalPagos = pagos.Count,
                ApiDisponible = fuenteDatos.Origen == "API_DUMMY",
                UltimasFacturas = facturas.Take(5).ToList()
            };

            return View(model);
        }
    }
}