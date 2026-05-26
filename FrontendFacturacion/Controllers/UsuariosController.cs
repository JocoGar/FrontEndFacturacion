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

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

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
            ViewBag.ApiError = _api.UltimoErrorApi;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UsuarioDto usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.PasswordUsuario))
                ModelState.AddModelError(nameof(usuario.PasswordUsuario), "La contraseña es obligatoria.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _api.ObtenerRolesAsync();
                return View(usuario);
            }

            var ok = await _api.CrearUsuarioAsync(usuario);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                ViewBag.Roles = await _api.ObtenerRolesAsync();
                ViewBag.ApiError = error;

                return View(usuario);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string id)
        {
            ViewBag.Roles = await _api.ObtenerRolesAsync();

            var usuario = await _api.ObtenerUsuarioPorDpiAsync(id);

            if (usuario == null)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró el usuario solicitado."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            usuario.PasswordUsuario = "";

            return View(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, UsuarioDto usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.PasswordUsuario))
                ModelState.Remove(nameof(usuario.PasswordUsuario));

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _api.ObtenerRolesAsync();
                return View(usuario);
            }

            var ok = await _api.ActualizarUsuarioAsync(id, usuario);

            if (!ok)
            {
                var error = _api.UltimoErrorApi;

                ViewBag.Roles = await _api.ObtenerRolesAsync();
                ViewBag.ApiError = error;

                return View(usuario);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _api.EliminarUsuarioAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar el usuario."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }
    }
}