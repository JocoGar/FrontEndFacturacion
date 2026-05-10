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

        public async Task<IActionResult> Index()
        {
            var productos = await _api.ObtenerProductosAsync();
            return View(productos);
        }
    }
}