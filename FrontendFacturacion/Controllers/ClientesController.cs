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

        public async Task<IActionResult> Index(string? buscar)
        {
            var clientes = await _api.ObtenerClientesAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                clientes = clientes
                    .Where(c =>
                        c.IdCliente.ToString().Contains(buscar) ||
                        c.DpiCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        c.NitCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        c.NombreCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        c.ApellidoCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        c.CorreoCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        c.TelefonoCliente.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(clientes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClienteDto cliente)
        {
            await _api.CrearClienteAsync(cliente);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cliente = await _api.ObtenerClientePorIdAsync(id);

            if (cliente == null)
                return RedirectToAction("Index");

            return View(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ClienteDto cliente)
        {
            await _api.ActualizarClienteAsync(id, cliente);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarClienteAsync(id);
            return RedirectToAction("Index");
        }
    }
}