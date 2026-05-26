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

        public async Task<IActionResult> Index(string? buscar)
        {
            var pagos = await _api.ObtenerPagosAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                pagos = pagos
                    .Where(p =>
                        p.IdPago.ToString().Contains(buscar) ||
                        p.NumeroFactura.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.MetodoPago.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.NumeroReferenciaPago.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.EstadoPago.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(pagos);
        }

        public async Task<IActionResult> Create(int? idFactura)
        {
            ViewBag.Facturas = await _api.ObtenerFacturasAsync();
            ViewBag.IdFacturaSeleccionada = idFactura;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PagoDto pago)
        {
            await _api.CrearPagoAsync(pago);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Facturas = await _api.ObtenerFacturasAsync();

            var pago = await _api.ObtenerPagoPorIdAsync(id);

            if (pago == null)
                return RedirectToAction("Index");

            return View(pago);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, PagoDto pago)
        {
            await _api.ActualizarPagoAsync(id, pago);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarPagoAsync(id);
            return RedirectToAction("Index");
        }
    }
}