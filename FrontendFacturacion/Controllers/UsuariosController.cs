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

        public async Task<IActionResult> Index(string? buscar)
        {
            var usuarios = await _api.ObtenerUsuariosAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                usuarios = usuarios
                    .Where(u =>
                        u.DpiUsuario.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        u.NombreUsuario.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        u.ApellidoUsuario.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        u.CorreoUsuario.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        u.NombreRol.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(usuarios);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _api.ObtenerRolesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UsuarioDto usuario)
        {
            await _api.CrearUsuarioAsync(usuario);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string id)
        {
            ViewBag.Roles = await _api.ObtenerRolesAsync();

            var usuario = await _api.ObtenerUsuarioPorDpiAsync(id);

            if (usuario == null)
                return RedirectToAction("Index");

            return View(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, UsuarioDto usuario)
        {
            await _api.ActualizarUsuarioAsync(id, usuario);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _api.EliminarUsuarioAsync(id);
            return RedirectToAction("Index");
        }
    }
}