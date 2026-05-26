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

            ViewBag.ApiError = TempData["ApiError"] as string;

            if (string.IsNullOrWhiteSpace(ViewBag.ApiError as string))
                ViewBag.ApiError = _api.UltimoErrorApi;

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
            if (!ModelState.IsValid)
                return View(cliente);

            var ok = await _api.CrearClienteAsync(cliente);

            if (!ok)
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                return View(cliente);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cliente = await _api.ObtenerClientePorIdAsync(id);

            if (cliente == null)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se encontró el cliente solicitado."
                    : _api.UltimoErrorApi;

                return RedirectToAction("Index");
            }

            return View(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ClienteDto cliente)
        {
            if (!ModelState.IsValid)
                return View(cliente);

            var ok = await _api.ActualizarClienteAsync(id, cliente);

            if (!ok)
            {
                ViewBag.ApiError = _api.UltimoErrorApi;
                return View(cliente);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _api.EliminarClienteAsync(id);

            if (!ok)
            {
                TempData["ApiError"] = string.IsNullOrWhiteSpace(_api.UltimoErrorApi)
                    ? "No se pudo eliminar el cliente."
                    : _api.UltimoErrorApi;
            }

            return RedirectToAction("Index");
        }
    }
}