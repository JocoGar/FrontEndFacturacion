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


        /// <summary>
        /// Obtiene desde la API la lista de clientes y devuelve una vista con los resultados, filtrando por la cadena
        /// de búsqueda si se proporciona.
        /// </summary>
        /// <remarks>Establece ViewBag.Buscar con el término de búsqueda. El filtrado convierte Id a
        /// cadena y usa StringComparison.OrdinalIgnoreCase para los campos textuales.</remarks>
        /// <param name="buscar">Cadena opcional para filtrar clientes por Id, DPI, NIT, nombre, apellido, correo o teléfono; las
        /// comparaciones de texto son insensibles a mayúsculas.</param>
        /// <returns>Una Task<IActionResult> que renderiza la vista con la colección de ClienteDto; si la API falla se establece
        /// ViewBag.Error y se devuelve una lista vacía.</returns>
        public async Task<IActionResult> Index(string? buscar)
        {
            var clientes = await _api.ObtenerClientesAsync();

            if (clientes == null)
            {
                ViewBag.Error = "No se pudieron obtener los clientes. El servidor está temporalmente inestable.";
                return View(new List<ClienteDto>());
            }

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
            bool exito = await _api.CrearClienteAsync(cliente);

            if (!exito)
            {
                ModelState.AddModelError(string.Empty, "Error al registrar cliente. Verifique la conexión.");
                return View(cliente);
            }

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