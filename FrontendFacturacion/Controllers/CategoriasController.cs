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

        public async Task<IActionResult> Index()
        {
            var categorias = await _api.ObtenerCategoriasAsync();
            return View(categorias);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}