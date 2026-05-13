using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class ClientesController : Controller
    {
        private readonly FacturacionApiService _api;

        public ClientesController(FacturacionApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var clientes = await _api.ObtenerClientesAsync();
            return View(clientes);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}