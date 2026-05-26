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

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

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
            if (!ModelState.IsValid)
                return View(rol);

            var ok = await _api.CrearRolAsync(rol);

            if (!ok)
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                return View(rol);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var rol = await _api.ObtenerRolPorIdAsync(id);

            if (rol == null)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró el rol solicitado."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            return View(rol);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, RolDto rol)
        {
            if (!ModelState.IsValid)
                return View(rol);

            var ok = await _api.ActualizarRolAsync(id, rol);

            if (!ok)
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                return View(rol);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _api.EliminarRolAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar el rol."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }
    }
}