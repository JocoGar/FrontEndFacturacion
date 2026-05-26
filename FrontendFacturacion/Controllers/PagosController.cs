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

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                pagos = pagos
                    .Where(p =>
                        p.IdPago.ToString().Contains(buscar) ||
                        p.IdFacturaPago.ToString().Contains(buscar) ||
                        p.NumeroFactura.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.MetodoPago.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.NumeroReferenciaPago.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(pagos);
        }

        public async Task<IActionResult> Create(int? idFactura)
        {
            ViewBag.Facturas = await _api.ObtenerFacturasAsync();
            ViewBag.IdFacturaSeleccionada = idFactura;
            ViewBag.ApiError = _api.UltimoErrorApi;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PagoDto pago)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Facturas = await _api.ObtenerFacturasAsync();
                return View(pago);
            }

            var ok = await _api.CrearPagoAsync(pago);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                ViewBag.Facturas = await _api.ObtenerFacturasAsync();
                ViewBag.ApiError = error;

                return View(pago);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Facturas = await _api.ObtenerFacturasAsync();

            var pago = await _api.ObtenerPagoPorIdAsync(id);

            if (pago == null)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró el pago solicitado."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            return View(pago);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, PagoDto pago)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Facturas = await _api.ObtenerFacturasAsync();
                return View(pago);
            }

            var ok = await _api.ActualizarPagoAsync(id, pago);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                ViewBag.Facturas = await _api.ObtenerFacturasAsync();
                ViewBag.ApiError = error;

                return View(pago);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _api.EliminarPagoAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar el pago."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }
    }
}