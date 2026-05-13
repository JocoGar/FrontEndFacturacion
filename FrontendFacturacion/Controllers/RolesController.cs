using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class RolesController : Controller
    {
        private readonly FacturacionApiService _api;

        public RolesController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _api.ObtenerRolesAsync();
            return View(roles);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}