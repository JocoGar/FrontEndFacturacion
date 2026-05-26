using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;
using FrontendFacturacion.Filters;

namespace FrontendFacturacion.Controllers
{
    using FrontendFacturacion.Services;
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

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

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
            if (!ModelState.IsValid)
                return View(categoria);

            var ok = await _api.CrearCategoriaAsync(categoria);

            if (!ok)
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                return View(categoria);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var categoria = await _api.ObtenerCategoriaPorIdAsync(id);

            if (categoria == null)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró la categoría solicitada."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            return View(categoria);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CategoriaDto categoria)
        {
            if (!ModelState.IsValid)
                return View(categoria);

            var ok = await _api.ActualizarCategoriaAsync(id, categoria);

            if (!ok)
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                return View(categoria);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _api.EliminarCategoriaAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar la categoría."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }
    }
}