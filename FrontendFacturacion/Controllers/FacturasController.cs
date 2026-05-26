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

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                facturas = facturas
                    .Where(f =>
                        f.IdFactura.ToString().Contains(buscar) ||
                        f.NumeroFactura.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        f.Cliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        f.Usuario.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        f.NitCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(facturas);
        }

        public async Task<IActionResult> Create()
        {
            await CargarCombosFacturaAsync();
            return View(new FacturaCrearViewModel
            {
                FechaEmisionFactura = DateTime.Today,
                MonedaFactura = "GTQ"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(FacturaCrearViewModel factura)
        {
            factura.Detalles = factura.Detalles
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.CodigoProductoDetalleFactura) ||
                    d.CantidadDetalleFactura > 0 ||
                    d.PrecioUnitarioDetalleFactura > 0)
                .ToList();

            ModelState.Clear();
            TryValidateModel(factura);

            if (!factura.Detalles.Any())
                ModelState.AddModelError("Detalles", "Debe agregar al menos un producto o servicio.");

            if (factura.FechaEmisionFactura == default)
                ModelState.AddModelError(nameof(factura.FechaEmisionFactura), "La fecha de emisión es obligatoria.");

            if (factura.FechaEmisionFactura.Date > DateTime.Today)
                ModelState.AddModelError(nameof(factura.FechaEmisionFactura), "La fecha de emisión no puede ser mayor a la fecha actual.");

            foreach (var detalle in factura.Detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.CodigoProductoDetalleFactura))
                    ModelState.AddModelError("Detalles", "Todos los detalles deben tener un producto seleccionado.");

                if (detalle.CantidadDetalleFactura <= 0)
                    ModelState.AddModelError("Detalles", "Todas las cantidades deben ser mayores a 0.");

                if (detalle.PrecioUnitarioDetalleFactura <= 0)
                    ModelState.AddModelError("Detalles", "Todos los productos deben tener precio mayor a 0.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosFacturaAsync();
                return View(factura);
            }

            var ok = await _api.CrearFacturaAsync(factura);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                await CargarCombosFacturaAsync();
                ViewBag.ApiError = error;

                return View(factura);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var detalle = await _api.ObtenerDetalleFacturaAsync(id);

            if (detalle == null || detalle.Factura.IdFactura == 0)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró la factura solicitada."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            return View(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _api.EliminarFacturaAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar la factura."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }

        private async Task CargarCombosFacturaAsync()
        {
            ViewBag.Clientes = await _api.ObtenerClientesAsync();
            var errorClientes = _api.UltimoErrorApi;

            ViewBag.Productos = await _api.ObtenerProductosAsync();
            var errorProductos = _api.UltimoErrorApi;

            ViewBag.Usuarios = await _api.ObtenerUsuariosAsync();
            var errorUsuarios = _api.UltimoErrorApi;

            var errores = new List<string>();

            if (!string.IsNullOrWhiteSpace(errorClientes))
                errores.Add(errorClientes);

            if (!string.IsNullOrWhiteSpace(errorProductos))
                errores.Add(errorProductos);

            if (!string.IsNullOrWhiteSpace(errorUsuarios))
                errores.Add(errorUsuarios);

            ViewBag.ApiError = errores.FirstOrDefault();
        }
    }
}