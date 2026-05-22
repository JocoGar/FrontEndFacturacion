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

        public async Task<IActionResult> Index(string? buscar)
        {
            var facturas = await _api.ObtenerFacturasAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                facturas = facturas
                    .Where(f =>
                        f.IdFactura.ToString().Contains(buscar) ||
                        f.NumeroFactura.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        f.Cliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        f.Usuario.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        f.EstadoFactura.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(facturas);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Clientes = await _api.ObtenerClientesAsync();
            ViewBag.Productos = await _api.ObtenerProductosAsync();
            ViewBag.Usuarios = await _api.ObtenerUsuariosAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(FacturaCrearViewModel factura)
        {
            await _api.CrearFacturaAsync(factura);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id = 1)
        {
            var detalle = await _api.ObtenerDetalleFacturaAsync(id);
            return View(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> Anular(int id)
        {
            await _api.AnularFacturaAsync(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MarcarPagada(int id)
        {
            await _api.CambiarEstadoFacturaAsync(id, "PAGADA");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarFacturaAsync(id);
            return RedirectToAction("Index");
        }
    }
}