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

            var servicioDisponible = fuenteDatos.Origen == "DISPONIBLE";

            var productos = servicioDisponible
                ? await _api.ObtenerProductosAsync()
                : new List<ProductoDto>();

            var clientes = servicioDisponible
                ? await _api.ObtenerClientesAsync()
                : new List<ClienteDto>();

            var facturas = servicioDisponible
                ? await _api.ObtenerFacturasAsync()
                : new List<FacturaDto>();

            var pagos = servicioDisponible
                ? await _api.ObtenerPagosAsync()
                : new List<PagoDto>();

            var model = new DashboardViewModel
            {
                TotalProductos = productos.Count,
                TotalClientes = clientes.Count,
                TotalFacturas = facturas.Count,
                FacturasPendientes = 0,
                TotalPagos = pagos.Count,
                ApiDisponible = servicioDisponible,
                FuenteDatos = fuenteDatos,
                UltimasFacturas = facturas
                    .OrderByDescending(f => f.FechaEmisionFactura)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
    }
}