using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly FacturacionApiService _api;

        public UsuariosController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _api.ObtenerUsuariosAsync();
            return View(usuarios);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _api.ObtenerRolesAsync();
            return View();
        }
    }
}