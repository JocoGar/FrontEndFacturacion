using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly FacturacionApiService _api;

        public CategoriasController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            var categorias = await _api.ObtenerCategoriasAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                categorias = categorias
                    .Where(c =>
                        c.IdCategoriaProducto.ToString().Contains(buscar) ||
                        c.NombreCategoriaProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        c.DescripcionCategoriaProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(categorias);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoriaDto categoria)
        {
            await _api.CrearCategoriaAsync(categoria);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await _api.ObtenerCategoriaPorIdAsync(id);

            if (categoria == null)
                return RedirectToAction("Index");

            return View(categoria);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CategoriaDto categoria)
        {
            await _api.ActualizarCategoriaAsync(id, categoria);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarCategoriaAsync(id);
            return RedirectToAction("Index");
        }
    }
}