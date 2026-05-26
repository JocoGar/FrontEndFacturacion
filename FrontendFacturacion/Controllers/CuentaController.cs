using Microsoft.AspNetCore.Mvc;
using FrontendFacturacion.Services;

namespace FrontendFacturacion.Controllers
{
    public class CuentaController : Controller
    {
        private readonly FacturacionApiService _api;

        public CuentaController(FacturacionApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string nombreUsuario, string password)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Debe ingresar usuario y contraseña.";
                return View();
            }

            var usuario = await _api.LoginAsync(nombreUsuario, password);

            if (usuario != null)
            {
                HttpContext.Session.SetString("UsuarioDpi", usuario.DpiUsuario);
                HttpContext.Session.SetString("UsuarioNombre", $"{usuario.NombreUsuario} {usuario.ApellidoUsuario}".Trim());
                HttpContext.Session.SetString("UsuarioRol", usuario.NombreRol);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                ? "Usuario o contraseña incorrectos."
                : _api.UltimoErrorApi;

            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Cuenta");
        }
    }
}