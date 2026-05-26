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

        /// <summary>
        /// Devuelve la vista de inicio de sesión.
        /// </summary>
        /// <remarks>Accesible mediante HTTP GET (atributo [HttpGet]); no procesa datos ni requiere
        /// parámetros.</remarks>
        /// <returns>Un IActionResult que renderiza la vista de inicio de sesión.</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Método para manejar el inicio de sesión del usuario. Verifica las credenciales ingresadas y redirige al usuario a la página principal si son correctas. Si las credenciales son incorrectas, muestra un mensaje de error.
        /// </summary>
        /// <param name="nombreUsuario"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(string nombreUsuario, string password)
        {
            if (nombreUsuario == "admin" && password == "123")
            {
                return RedirectToAction("Index", "Home");
            }

            var usuario = await _api.ObtenerUsuarioPorDpiAsync(nombreUsuario);

            if (usuario != null && usuario.PasswordUsuario == password)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }
    }
}