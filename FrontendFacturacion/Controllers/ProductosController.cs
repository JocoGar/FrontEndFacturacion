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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductoDto producto)
        {
            await _api.CrearProductoAsync(producto);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Categorias = await _api.ObtenerCategoriasAsync();

            var producto = await _api.ObtenerProductoPorIdAsync(id);

            if (producto == null)
                return RedirectToAction("Index");

            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoDto producto)
        {
            await _api.ActualizarProductoAsync(id, producto);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarProductoAsync(id);
            return RedirectToAction("Index");
        }
    }
}