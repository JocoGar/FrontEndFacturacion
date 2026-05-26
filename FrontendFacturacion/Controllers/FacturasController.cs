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
            ViewBag.ApiErrorEsConexion = TempData["ApiErrorEsConexion"] is bool esConexion && esConexion;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                ViewBag.ApiErrorEsConexion = _api.UltimoErrorEsConexion;
            }

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
                MonedaFactura = "GTQ",
                Detalles = new List<DetalleFacturaCrearDto>
                {
                    new DetalleFacturaCrearDto
                    {
                        CantidadDetalleFactura = 1,
                        PrecioUnitarioDetalleFactura = 0
                    }
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(FacturaCrearViewModel factura)
        {
            factura.DpiUsuarioFactura = (factura.DpiUsuarioFactura ?? "").Trim();
            factura.MonedaFactura = string.IsNullOrWhiteSpace(factura.MonedaFactura)
                ? "GTQ"
                : factura.MonedaFactura.Trim().ToUpper();

            factura.Detalles = factura.Detalles?
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.CodigoProductoDetalleFactura) ||
                    d.CantidadDetalleFactura > 0 ||
                    d.PrecioUnitarioDetalleFactura > 0)
                .ToList() ?? new List<DetalleFacturaCrearDto>();

            ModelState.Clear();

            if (factura.IdClienteFactura <= 0)
                ModelState.AddModelError(nameof(factura.IdClienteFactura), "Debe seleccionar un cliente.");

            if (string.IsNullOrWhiteSpace(factura.DpiUsuarioFactura))
                ModelState.AddModelError(nameof(factura.DpiUsuarioFactura), "Debe seleccionar un usuario vendedor.");

            if (factura.FechaEmisionFactura == default)
                ModelState.AddModelError(nameof(factura.FechaEmisionFactura), "La fecha de emisión es obligatoria.");

            if (factura.FechaEmisionFactura.Date > DateTime.Today)
                ModelState.AddModelError(nameof(factura.FechaEmisionFactura), "La fecha de emisión no puede ser mayor a la fecha actual.");

            if (factura.MonedaFactura != "GTQ" && factura.MonedaFactura != "USD")
                ModelState.AddModelError(nameof(factura.MonedaFactura), "La moneda solo puede ser GTQ o USD.");

            if (!factura.Detalles.Any())
                ModelState.AddModelError("Detalles", "Debe agregar al menos un producto o servicio.");

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
                var error = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo guardar la factura."
                    : _api.UltimoErrorApi;

                var esErrorConexion = _api.UltimoErrorEsConexion;

                ModelState.Clear();

                await CargarCombosFacturaAsync();

                ViewBag.ApiError = error;
                ViewBag.ApiErrorEsConexion = esErrorConexion;

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

                TempData["ApiErrorEsConexion"] = _api.UltimoErrorEsConexion;

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

                TempData["ApiErrorEsConexion"] = _api.UltimoErrorEsConexion;
            }

            return RedirectToAction("Index");
        }

        private async Task CargarCombosFacturaAsync()
        {
            var errores = new List<string>();
            var hayErrorConexion = false;

            ViewBag.Clientes = await _api.ObtenerClientesAsync();
            if (!string.IsNullOrWhiteSpace(_api.UltimoErrorApi))
            {
                errores.Add(_api.UltimoErrorApi);
                hayErrorConexion = hayErrorConexion || _api.UltimoErrorEsConexion;
            }

            ViewBag.Productos = await _api.ObtenerProductosAsync();
            if (!string.IsNullOrWhiteSpace(_api.UltimoErrorApi))
            {
                errores.Add(_api.UltimoErrorApi);
                hayErrorConexion = hayErrorConexion || _api.UltimoErrorEsConexion;
            }

            ViewBag.Usuarios = await _api.ObtenerUsuariosAsync();
            if (!string.IsNullOrWhiteSpace(_api.UltimoErrorApi))
            {
                errores.Add(_api.UltimoErrorApi);
                hayErrorConexion = hayErrorConexion || _api.UltimoErrorEsConexion;
            }

            ViewBag.ApiError = errores.FirstOrDefault();
            ViewBag.ApiErrorEsConexion = hayErrorConexion;
        }
    }
}