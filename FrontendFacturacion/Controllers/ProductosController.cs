using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendFacturacion.Controllers
{
    public class ProductosController : Controller
    {
        private readonly FacturacionApiService _api;

        public ProductosController(FacturacionApiService api)
        {
            _api = api;
        }

        /// <summary>
        /// Muestra en la vista la lista de productos obtenida desde la API y aplica un filtro opcional por término de
        /// búsqueda.
        /// </summary>
        /// <remarks>Obtiene los productos de forma asíncrona mediante _api.ObtenerProductosAsync(). Si
        /// 'buscar' tiene contenido, filtra la colección usando Contains sobre los campos relevantes.</remarks>
        /// <param name="buscar">Término de búsqueda opcional para filtrar productos por Id (comparado como texto), Código, Nombre, Categoría
        /// o Descripción; las comparaciones de texto son insensibles a mayúsculas.</param>
        /// <returns>IActionResult que renderiza la vista con la lista de productos; si la obtención falla devuelve la vista con
        /// una lista vacía y establece ViewBag.Error con un mensaje de conexión.</returns>
        public async Task<IActionResult> Index(string? buscar)
        {
            var productos = await _api.ObtenerProductosAsync();

            if (productos == null)
            {
                ViewBag.Error = "Conexión inestable con el servidor. No se pudieron cargar los productos.";
                return View(new List<ProductoDto>());
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                productos = productos
                    .Where(p =>
                        p.IdProducto.ToString().Contains(buscar) ||
                        p.CodigoProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.NombreProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.NombreCategoriaProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                        p.DescripcionProducto.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.Buscar = buscar;
            return View(productos);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await _api.ObtenerCategoriasAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductoDto producto)
        {
            await _api.CrearProductoAsync(producto);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Muestra la vista de edición del producto especificado.
        /// </summary>
        /// <remarks>Obtiene las categorías y el producto desde la API de forma asíncrona y puebla
        /// ViewBag.Categorias.</remarks>
        /// <param name="id">Identificador del producto a editar.</param>
        /// <returns>Resultado asincrónico que renderiza la vista de edición del producto o redirige a la acción Index si no se
        /// encuentra.</returns>
        public async Task<IActionResult> Edit(int id)
        {
            var categorias = await _api.ObtenerCategoriasAsync();
            ViewBag.Categorias = categorias ?? new List<CategoriaDto>();

            var producto = await _api.ObtenerProductoPorIdAsync(id);

            if (producto == null)
                return RedirectToAction("Index");

            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductoDto producto)
        {
            await _api.ActualizarProductoAsync(id, producto);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _api.EliminarProductoAsync(id);
            return RedirectToAction("Index");
        }
    }
}