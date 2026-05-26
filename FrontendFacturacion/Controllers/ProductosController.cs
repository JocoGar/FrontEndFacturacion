using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class ProductosController : Controller
    {
        private readonly FacturacionApiService _api;

        public ProductosController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            var productos = await _api.ObtenerProductosAsync();

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                productos = productos
                    .Where(p =>
                        p.IdProducto.ToString().Contains(buscar) ||
                        p.CodigoProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.NombreProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.NombreCategoriaProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.DescripcionProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(productos);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await _api.ObtenerCategoriasAsync();
            ViewBag.ApiError = _api.UltimoErrorApi;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductoDto producto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _api.ObtenerCategoriasAsync();
                return View(producto);
            }

            var ok = await _api.CrearProductoAsync(producto);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                ViewBag.Categorias = await _api.ObtenerCategoriasAsync();
                ViewBag.ApiError = error;

                return View(producto);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Categorias = await _api.ObtenerCategoriasAsync();

            var producto = await _api.ObtenerProductoPorIdAsync(id);

            if (producto == null)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró el producto o servicio solicitado."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoDto producto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = await _api.ObtenerCategoriasAsync();
                return View(producto);
            }

            var ok = await _api.ActualizarProductoAsync(id, producto);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                ViewBag.Categorias = await _api.ObtenerCategoriasAsync();
                ViewBag.ApiError = error;

                return View(producto);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _api.EliminarProductoAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar el producto o servicio."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }
    }
}