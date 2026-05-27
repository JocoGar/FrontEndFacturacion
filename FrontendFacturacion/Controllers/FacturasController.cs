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

        /// <summary>
        /// Obtiene facturas desde el servicio remoto, filtra por número si se proporciona un término de búsqueda y
        /// devuelve la vista con la lista resultante.
        /// </summary>
        /// <remarks>Establece ViewBag.Buscar con el término proporcionado. Utiliza
        /// _api.ObtenerFacturasAsync para recuperar datos; si devuelve null, se informa el error y se retorna una lista
        /// vacía.</remarks>
        /// <param name="buscar">Término opcional para filtrar facturas por NumeroFactura (comparación sin distinción de mayúsculas). Si es
        /// nulo o vacío, no se aplica filtrado.</param>
        /// <returns>Una Task que produce un IActionResult con la vista que muestra la lista de FacturaDto filtrada; si la
        /// obtención de facturas falla, devuelve la vista con una lista vacía y ViewBag.Error configurado.</returns>
        public async Task<IActionResult> Index(string? buscar)
        {
            var facturas = await _api.ObtenerFacturasAsync();

            if (facturas == null)
            {
                ViewBag.Error = "Conexión inestable con el servidor central. Intente nuevamente.";
                return View(new List<FacturaDto>());
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                facturas = facturas.Where(f => f.NumeroFactura.Contains(buscar, StringComparison.OrdinalIgnoreCase)).ToList();
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

        /// <summary>
        /// Crea una factura mediante una llamada a la API externa y redirige a la acción Index si la operación tiene
        /// éxito; en caso contrario muestra la vista con los errores.
        /// </summary>
        /// <remarks>Valida ModelState antes de llamar a la API. Si la API devuelve false añade un error
        /// de modelo genérico y re-muestra la vista.</remarks>
        /// <param name="factura">ViewModel que contiene los datos de la factura a crear.</param>
        /// <returns>Una tarea que produce un IActionResult: devuelve la misma vista con el modelo cuando hay errores de
        /// validación o la API rechaza la solicitud; redirige a Index cuando la creación es exitosa.</returns>
        [HttpPost]
        public async Task<IActionResult> Create(FacturaCrearViewModel factura)
        {
            if(!ModelState.IsValid)
                return View(factura);

            var exito = await _api.CrearFacturaAsync(factura);

            if (!exito)
            {
                ModelState.AddModelError(string.Empty, "La API rechazó la solicitud o el servidor está fuera de línea.");
                return View(factura);
            }

            return RedirectToAction("Index");
        }
        

        public async Task<IActionResult> Details(int id)
        {
            var detalle = await _api.ObtenerDetalleFacturaAsync(id);
            return View(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarFacturaAsync(id);
            return RedirectToAction("Index");
        }
    }
}