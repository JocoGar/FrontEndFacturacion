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

        public async Task<IActionResult> Index(string? buscar)
        {
            var roles = await _api.ObtenerRolesAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                roles = roles
                    .Where(r =>
                        r.IdRol.ToString().Contains(buscar) ||
                        r.NombreRol.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(roles);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RolDto rol)
        {
            await _api.CrearRolAsync(rol);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var rol = await _api.ObtenerRolPorIdAsync(id);

            if (rol == null)
                return RedirectToAction("Index");

            return View(rol);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, RolDto rol)
        {
            await _api.ActualizarRolAsync(id, rol);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarRolAsync(id);
            return RedirectToAction("Index");
        }
    }
}