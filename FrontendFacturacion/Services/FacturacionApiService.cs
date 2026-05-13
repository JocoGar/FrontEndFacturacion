using System.Net.Http.Json;
using System.Text.Json;

namespace FrontendFacturacion.Services
{
    public class FacturacionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly JsonSerializerOptions _jsonOptions;

        public FacturacionApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _environment = environment;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        private bool UsarMocks()
        {
            return _configuration.GetValue<bool>("ApiSettings:UseMockData");
        }

        private bool UsarFallbackSiApiFalla()
        {
            return _configuration.GetValue<bool>("ApiSettings:UseFallbackMockWhenApiFails", true);
        }

        private int ObtenerTimeout()
        {
            return _configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 2);
        }

        private string RutaMock(string archivo)
        {
            return Path.Combine(_environment.WebRootPath, "mocks", archivo);
        }

        private async Task<List<T>> LeerListaMockAsync<T>(string archivo)
        {
            var ruta = RutaMock(archivo);

            if (!File.Exists(ruta))
                return new List<T>();

            var json = await File.ReadAllTextAsync(ruta);
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }

        private async Task EscribirListaMockAsync<T>(string archivo, List<T> datos)
        {
            var ruta = RutaMock(archivo);
            var carpeta = Path.GetDirectoryName(ruta);

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta!);

            var json = JsonSerializer.Serialize(datos, _jsonOptions);
            await File.WriteAllTextAsync(ruta, json);
        }

        private async Task<T> LeerObjetoMockAsync<T>(string archivo, T valorDefault)
        {
            var ruta = RutaMock(archivo);

            if (!File.Exists(ruta))
                return valorDefault;

            var json = await File.ReadAllTextAsync(ruta);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? valorDefault;
        }

        private async Task EscribirObjetoMockAsync<T>(string archivo, T datos)
        {
            var ruta = RutaMock(archivo);
            var carpeta = Path.GetDirectoryName(ruta);

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta!);

            var json = JsonSerializer.Serialize(datos, _jsonOptions);
            await File.WriteAllTextAsync(ruta, json);
        }

        private async Task<List<T>> GetListAsync<T>(string endpoint, string mockFile)
        {
            if (UsarMocks())
                return await LeerListaMockAsync<T>(mockFile);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    if (UsarFallbackSiApiFalla())
                        return await LeerListaMockAsync<T>(mockFile);

                    return new List<T>();
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
            }
            catch
            {
                if (UsarFallbackSiApiFalla())
                    return await LeerListaMockAsync<T>(mockFile);

                return new List<T>();
            }
        }

        private async Task<T> GetObjectAsync<T>(string endpoint, string mockFile, T valorDefault)
        {
            if (UsarMocks())
                return await LeerObjetoMockAsync(mockFile, valorDefault);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    if (UsarFallbackSiApiFalla())
                        return await LeerObjetoMockAsync(mockFile, valorDefault);

                    return valorDefault;
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? valorDefault;
            }
            catch
            {
                if (UsarFallbackSiApiFalla())
                    return await LeerObjetoMockAsync(mockFile, valorDefault);

                return valorDefault;
            }
        }

        private async Task<bool> PostApiAsync<T>(string endpoint, T datos)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.PostAsJsonAsync(endpoint, datos, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> PutApiAsync<T>(string endpoint, T datos)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.PutAsJsonAsync(endpoint, datos, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> DeleteApiAsync(string endpoint)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.DeleteAsync(endpoint, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> ApiDisponibleAsync()
        {
            if (UsarMocks())
                return false;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));

                var response = await _httpClient.GetAsync("/api/categorias", cts.Token);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<CategoriaDto>> ObtenerCategoriasAsync()
        {
            return await GetListAsync<CategoriaDto>("/api/categorias", "categorias.json");
        }

        public async Task<CategoriaDto?> ObtenerCategoriaPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                return categorias.FirstOrDefault(c => c.IdCategoriaProducto == id);
            }

            return await GetObjectAsync<CategoriaDto?>($"/api/categorias/{id}", "categoria-vacia.json", null);
        }

        public async Task<bool> CrearCategoriaAsync(CategoriaDto categoria)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                categoria.IdCategoriaProducto = categorias.Any() ? categorias.Max(c => c.IdCategoriaProducto) + 1 : 1;
                categoria.FechaCreacionCategoriaProducto = DateTime.Now;
                categoria.FechaActualizacionCategoriaProducto = DateTime.Now;
                categorias.Add(categoria);

                await EscribirListaMockAsync("categorias.json", categorias);
                return true;
            }

            return await PostApiAsync("/api/categorias", categoria);
        }

        public async Task<bool> ActualizarCategoriaAsync(int id, CategoriaDto categoria)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                var actual = categorias.FirstOrDefault(c => c.IdCategoriaProducto == id);

                if (actual == null)
                    return false;

                actual.NombreCategoriaProducto = categoria.NombreCategoriaProducto;
                actual.DescripcionCategoriaProducto = categoria.DescripcionCategoriaProducto;
                actual.FechaActualizacionCategoriaProducto = DateTime.Now;

                await EscribirListaMockAsync("categorias.json", categorias);
                return true;
            }

            return await PutApiAsync($"/api/categorias/{id}", categoria);
        }

        public async Task<bool> EliminarCategoriaAsync(int id)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                categorias.RemoveAll(c => c.IdCategoriaProducto == id);
                await EscribirListaMockAsync("categorias.json", categorias);
                return true;
            }

            return await DeleteApiAsync($"/api/categorias/{id}");
        }

        public async Task<List<ProductoDto>> ObtenerProductosAsync()
        {
            return await GetListAsync<ProductoDto>("/api/productos", "productos.json");
        }

        public async Task<ProductoDto?> ObtenerProductoPorCodigoAsync(string codigo)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                return productos.FirstOrDefault(p => p.CodigoProducto == codigo);
            }

            return await GetObjectAsync<ProductoDto?>($"/api/productos/{codigo}", "producto-vacio.json", null);
        }

        public async Task<bool> CrearProductoAsync(ProductoDto producto)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                var categorias = await ObtenerCategoriasAsync();

                producto.IdProducto = productos.Any() ? productos.Max(p => p.IdProducto) + 1 : 1;
                producto.FechaCreacionProducto = DateTime.Now;
                producto.FechaActualizacionProducto = DateTime.Now;
                producto.NombreCategoriaProducto = categorias
                    .FirstOrDefault(c => c.IdCategoriaProducto == producto.IdCategoriaProducto)?
                    .NombreCategoriaProducto ?? "";

                productos.Add(producto);
                await EscribirListaMockAsync("productos.json", productos);
                return true;
            }

            return await PostApiAsync("/api/productos", producto);
        }

        public async Task<bool> ActualizarProductoAsync(string codigo, ProductoDto producto)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                var categorias = await ObtenerCategoriasAsync();
                var actual = productos.FirstOrDefault(p => p.CodigoProducto == codigo);

                if (actual == null)
                    return false;

                actual.IdCategoriaProducto = producto.IdCategoriaProducto;
                actual.NombreCategoriaProducto = categorias
                    .FirstOrDefault(c => c.IdCategoriaProducto == producto.IdCategoriaProducto)?
                    .NombreCategoriaProducto ?? "";
                actual.NombreProducto = producto.NombreProducto;
                actual.DescripcionProducto = producto.DescripcionProducto;
                actual.PrecioUnitarioProducto = producto.PrecioUnitarioProducto;
                actual.PrecioCostoProducto = producto.PrecioCostoProducto;
                actual.FechaActualizacionProducto = DateTime.Now;

                await EscribirListaMockAsync("productos.json", productos);
                return true;
            }

            return await PutApiAsync($"/api/productos/{codigo}", producto);
        }

        public async Task<bool> EliminarProductoAsync(string codigo)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                productos.RemoveAll(p => p.CodigoProducto == codigo);
                await EscribirListaMockAsync("productos.json", productos);
                return true;
            }

            return await DeleteApiAsync($"/api/productos/{codigo}");
        }

        public async Task<List<ClienteDto>> ObtenerClientesAsync()
        {
            return await GetListAsync<ClienteDto>("/api/clientes", "clientes.json");
        }

        public async Task<ClienteDto?> ObtenerClientePorDpiAsync(string dpi)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                return clientes.FirstOrDefault(c => c.DpiCliente == dpi);
            }

            return await GetObjectAsync<ClienteDto?>($"/api/clientes/{dpi}", "cliente-vacio.json", null);
        }

        public async Task<bool> CrearClienteAsync(ClienteDto cliente)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                cliente.FechaCreacionCliente = DateTime.Now;
                cliente.FechaActualizacionCliente = DateTime.Now;
                clientes.Add(cliente);

                await EscribirListaMockAsync("clientes.json", clientes);
                return true;
            }

            return await PostApiAsync("/api/clientes", cliente);
        }

        public async Task<bool> ActualizarClienteAsync(string dpi, ClienteDto cliente)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                var actual = clientes.FirstOrDefault(c => c.DpiCliente == dpi);

                if (actual == null)
                    return false;

                actual.NombreCliente = cliente.NombreCliente;
                actual.ApellidoCliente = cliente.ApellidoCliente;
                actual.CorreoCliente = cliente.CorreoCliente;
                actual.TelefonoCliente = cliente.TelefonoCliente;
                actual.FechaActualizacionCliente = DateTime.Now;

                await EscribirListaMockAsync("clientes.json", clientes);
                return true;
            }

            return await PutApiAsync($"/api/clientes/{dpi}", cliente);
        }

        public async Task<bool> EliminarClienteAsync(string dpi)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                clientes.RemoveAll(c => c.DpiCliente == dpi);
                await EscribirListaMockAsync("clientes.json", clientes);
                return true;
            }

            return await DeleteApiAsync($"/api/clientes/{dpi}");
        }

        public async Task<List<RolDto>> ObtenerRolesAsync()
        {
            return await GetListAsync<RolDto>("/api/roles", "roles.json");
        }

        public async Task<RolDto?> ObtenerRolPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var roles = await ObtenerRolesAsync();
                return roles.FirstOrDefault(r => r.IdRol == id);
            }

            return await GetObjectAsync<RolDto?>($"/api/roles/{id}", "rol-vacio.json", null);
        }

        public async Task<bool> CrearRolAsync(RolDto rol)
        {
            if (UsarMocks())
            {
                var roles = await ObtenerRolesAsync();
                rol.IdRol = roles.Any() ? roles.Max(r => r.IdRol) + 1 : 1;
                roles.Add(rol);

                await EscribirListaMockAsync("roles.json", roles);
                return true;
            }

            return await PostApiAsync("/api/roles", rol);
        }

        public async Task<bool> ActualizarRolAsync(int id, RolDto rol)
        {
            if (UsarMocks())
            {
                var roles = await ObtenerRolesAsync();
                var actual = roles.FirstOrDefault(r => r.IdRol == id);

                if (actual == null)
                    return false;

                actual.NombreRol = rol.NombreRol;

                await EscribirListaMockAsync("roles.json", roles);
                return true;
            }

            return await PutApiAsync($"/api/roles/{id}", rol);
        }

        public async Task<bool> EliminarRolAsync(int id)
        {
            if (UsarMocks())
            {
                var roles = await ObtenerRolesAsync();
                roles.RemoveAll(r => r.IdRol == id);
                await EscribirListaMockAsync("roles.json", roles);
                return true;
            }

            return await DeleteApiAsync($"/api/roles/{id}");
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosAsync()
        {
            return await GetListAsync<UsuarioDto>("/api/usuarios", "usuarios.json");
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorDpiAsync(string dpi)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                return usuarios.FirstOrDefault(u => u.DpiUsuario == dpi);
            }

            return await GetObjectAsync<UsuarioDto?>($"/api/usuarios/{dpi}", "usuario-vacio.json", null);
        }

        public async Task<bool> CrearUsuarioAsync(UsuarioDto usuario)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                var roles = await ObtenerRolesAsync();

                usuario.NombreRol = roles.FirstOrDefault(r => r.IdRol == usuario.IdRol)?.NombreRol ?? "";
                usuario.FechaCreacionUsuario = DateTime.Now;
                usuario.FechaActualizacionUsuario = DateTime.Now;

                usuarios.Add(usuario);
                await EscribirListaMockAsync("usuarios.json", usuarios);
                return true;
            }

            return await PostApiAsync("/api/usuarios", usuario);
        }

        public async Task<bool> ActualizarUsuarioAsync(string dpi, UsuarioDto usuario)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                var roles = await ObtenerRolesAsync();
                var actual = usuarios.FirstOrDefault(u => u.DpiUsuario == dpi);

                if (actual == null)
                    return false;

                actual.NombreUsuario = usuario.NombreUsuario;
                actual.ApellidoUsuario = usuario.ApellidoUsuario;
                actual.CorreoUsuario = usuario.CorreoUsuario;
                actual.IdRol = usuario.IdRol;
                actual.NombreRol = roles.FirstOrDefault(r => r.IdRol == usuario.IdRol)?.NombreRol ?? "";
                actual.FechaActualizacionUsuario = DateTime.Now;

                await EscribirListaMockAsync("usuarios.json", usuarios);
                return true;
            }

            return await PutApiAsync($"/api/usuarios/{dpi}", usuario);
        }

        public async Task<bool> EliminarUsuarioAsync(string dpi)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                usuarios.RemoveAll(u => u.DpiUsuario == dpi);
                await EscribirListaMockAsync("usuarios.json", usuarios);
                return true;
            }

            return await DeleteApiAsync($"/api/usuarios/{dpi}");
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync()
        {
            return await GetListAsync<FacturaDto>("/api/facturas", "facturas.json");
        }

        public async Task<FacturaDetalleViewModel> ObtenerDetalleFacturaAsync(int id)
        {
            return await GetObjectAsync(
                $"/api/facturas/{id}",
                $"factura-detalle-{id}.json",
                new FacturaDetalleViewModel()
            );
        }

        public async Task<bool> CrearFacturaAsync(FacturaCrearViewModel model)
        {
            if (UsarMocks())
            {
                var facturas = await ObtenerFacturasAsync();
                var clientes = await ObtenerClientesAsync();
                var usuarios = await ObtenerUsuariosAsync();
                var productos = await ObtenerProductosAsync();

                var nuevoId = facturas.Any() ? facturas.Max(f => f.IdFactura) + 1 : 1;

                var cliente = clientes.FirstOrDefault(c => c.DpiCliente == model.DpiClienteFactura);
                var usuario = usuarios.FirstOrDefault(u => u.DpiUsuario == model.DpiUsuarioFactura);

                var subtotal = model.Detalles.Sum(d => d.CantidadDetalleFactura * d.PrecioUnitarioDetalleFactura);

                var factura = new FacturaDto
                {
                    IdFactura = nuevoId,
                    DpiClienteFactura = model.DpiClienteFactura,
                    NombreCliente = cliente?.NombreCliente ?? "",
                    ApellidoCliente = cliente?.ApellidoCliente ?? "",
                    DpiUsuarioFactura = model.DpiUsuarioFactura,
                    NombreUsuario = usuario?.NombreUsuario ?? "",
                    ApellidoUsuario = usuario?.ApellidoUsuario ?? "",
                    NumeroFactura = model.NumeroFactura,
                    FechaEmisionFactura = model.FechaEmisionFactura,
                    FechaVencimientoFactura = model.FechaVencimientoFactura,
                    EstadoFactura = model.EstadoFactura,
                    SubtotalFactura = subtotal,
                    TotalFactura = subtotal,
                    MonedaFactura = model.MonedaFactura,
                    FechaCreacionFactura = DateTime.Now,
                    FechaActualizacionFactura = DateTime.Now
                };

                facturas.Add(factura);
                await EscribirListaMockAsync("facturas.json", facturas);

                var detalles = model.Detalles.Select((d, index) =>
                {
                    var producto = productos.FirstOrDefault(p => p.CodigoProducto == d.CodigoProductoDetalleFactura);

                    return new DetalleFacturaDto
                    {
                        IdDetalleFactura = index + 1,
                        IdFacturaDetalleFactura = nuevoId,
                        CodigoProductoDetalleFactura = d.CodigoProductoDetalleFactura,
                        NombreProducto = producto?.NombreProducto ?? "",
                        CantidadDetalleFactura = d.CantidadDetalleFactura,
                        PrecioUnitarioDetalleFactura = d.PrecioUnitarioDetalleFactura
                    };
                }).ToList();

                var detalleFactura = new FacturaDetalleViewModel
                {
                    Factura = factura,
                    Cliente = cliente ?? new ClienteDto(),
                    Usuario = usuario ?? new UsuarioDto(),
                    Detalles = detalles,
                    Pagos = new List<PagoDto>()
                };

                await EscribirObjetoMockAsync($"factura-detalle-{nuevoId}.json", detalleFactura);
                return true;
            }

            return await PostApiAsync("/api/facturas", model);
        }

        public async Task<bool> CambiarEstadoFacturaAsync(int id, string estado)
        {
            if (UsarMocks())
            {
                var facturas = await ObtenerFacturasAsync();
                var actual = facturas.FirstOrDefault(f => f.IdFactura == id);

                if (actual == null)
                    return false;

                actual.EstadoFactura = estado;
                actual.FechaActualizacionFactura = DateTime.Now;

                await EscribirListaMockAsync("facturas.json", facturas);

                var detalle = await ObtenerDetalleFacturaAsync(id);
                detalle.Factura.EstadoFactura = estado;
                detalle.Factura.FechaActualizacionFactura = DateTime.Now;

                await EscribirObjetoMockAsync($"factura-detalle-{id}.json", detalle);
                return true;
            }

            return await PutApiAsync($"/api/facturas/{id}/estado", new { estadoFactura = estado });
        }

        public async Task<bool> EliminarFacturaAsync(int id)
        {
            if (UsarMocks())
            {
                var facturas = await ObtenerFacturasAsync();
                facturas.RemoveAll(f => f.IdFactura == id);
                await EscribirListaMockAsync("facturas.json", facturas);

                var rutaDetalle = RutaMock($"factura-detalle-{id}.json");

                if (File.Exists(rutaDetalle))
                    File.Delete(rutaDetalle);

                return true;
            }

            return await DeleteApiAsync($"/api/facturas/{id}");
        }

        public async Task<List<PagoDto>> ObtenerPagosAsync()
        {
            return await GetListAsync<PagoDto>("/api/pagos", "pagos.json");
        }

        public async Task<PagoDto?> ObtenerPagoPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                return pagos.FirstOrDefault(p => p.IdPago == id);
            }

            return await GetObjectAsync<PagoDto?>($"/api/pagos/{id}", "pago-vacio.json", null);
        }

        public async Task<bool> CrearPagoAsync(PagoDto pago)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                var facturas = await ObtenerFacturasAsync();

                pago.IdPago = pagos.Any() ? pagos.Max(p => p.IdPago) + 1 : 1;
                pago.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";
                pago.FechaCreacionPago = DateTime.Now;

                pagos.Add(pago);
                await EscribirListaMockAsync("pagos.json", pagos);

                await CambiarEstadoFacturaAsync(pago.IdFacturaPago, "PAGADA");

                return true;
            }

            return await PostApiAsync("/api/pagos", pago);
        }

        public async Task<bool> ActualizarPagoAsync(int id, PagoDto pago)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                var facturas = await ObtenerFacturasAsync();
                var actual = pagos.FirstOrDefault(p => p.IdPago == id);

                if (actual == null)
                    return false;

                actual.IdFacturaPago = pago.IdFacturaPago;
                actual.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";
                actual.FechaPago = pago.FechaPago;
                actual.MontoPago = pago.MontoPago;
                actual.MetodoPago = pago.MetodoPago;
                actual.NumeroReferenciaPago = pago.NumeroReferenciaPago;
                actual.EstadoPago = pago.EstadoPago;

                await EscribirListaMockAsync("pagos.json", pagos);
                return true;
            }

            return await PutApiAsync($"/api/pagos/{id}", pago);
        }

        public async Task<bool> EliminarPagoAsync(int id)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                pagos.RemoveAll(p => p.IdPago == id);
                await EscribirListaMockAsync("pagos.json", pagos);
                return true;
            }

            return await DeleteApiAsync($"/api/pagos/{id}");
        }
    }

    public class CategoriaDto
    {
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoriaProducto { get; set; } = "";
        public string DescripcionCategoriaProducto { get; set; } = "";
        public DateTime FechaCreacionCategoriaProducto { get; set; }
        public DateTime? FechaActualizacionCategoriaProducto { get; set; }
    }

    public class ProductoDto
    {
        public int IdProducto { get; set; }
        public string CodigoProducto { get; set; } = "";
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoriaProducto { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public string DescripcionProducto { get; set; } = "";
        public decimal PrecioUnitarioProducto { get; set; }
        public decimal PrecioCostoProducto { get; set; }
        public DateTime FechaCreacionProducto { get; set; }
        public DateTime? FechaActualizacionProducto { get; set; }
    }

    public class ClienteDto
    {
        public string DpiCliente { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string ApellidoCliente { get; set; } = "";
        public string CorreoCliente { get; set; } = "";
        public string TelefonoCliente { get; set; } = "";
        public DateTime FechaCreacionCliente { get; set; }
        public DateTime? FechaActualizacionCliente { get; set; }
    }

    public class RolDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = "";
    }

    public class UsuarioDto
    {
        public string DpiUsuario { get; set; } = "";
        public string NombreUsuario { get; set; } = "";
        public string ApellidoUsuario { get; set; } = "";
        public string CorreoUsuario { get; set; } = "";
        public string PasswordUsuario { get; set; } = "";
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = "";
        public DateTime FechaCreacionUsuario { get; set; }
        public DateTime? FechaActualizacionUsuario { get; set; }
    }

    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public string DpiClienteFactura { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string ApellidoCliente { get; set; } = "";
        public string DpiUsuarioFactura { get; set; } = "";
        public string NombreUsuario { get; set; } = "";
        public string ApellidoUsuario { get; set; } = "";
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaEmisionFactura { get; set; }
        public DateTime? FechaVencimientoFactura { get; set; }
        public string EstadoFactura { get; set; } = "";
        public decimal SubtotalFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public string MonedaFactura { get; set; } = "GTQ";
        public DateTime FechaCreacionFactura { get; set; }
        public DateTime? FechaActualizacionFactura { get; set; }

        public string Cliente => $"{NombreCliente} {ApellidoCliente}".Trim();
        public string Usuario => $"{NombreUsuario} {ApellidoUsuario}".Trim();
    }

    public class DetalleFacturaDto
    {
        public int IdDetalleFactura { get; set; }
        public int IdFacturaDetalleFactura { get; set; }
        public string CodigoProductoDetalleFactura { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public int CantidadDetalleFactura { get; set; }
        public decimal PrecioUnitarioDetalleFactura { get; set; }
        public decimal Subtotal => CantidadDetalleFactura * PrecioUnitarioDetalleFactura;
    }

    public class PagoDto
    {
        public int IdPago { get; set; }
        public int IdFacturaPago { get; set; }
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaPago { get; set; }
        public decimal MontoPago { get; set; }
        public string MetodoPago { get; set; } = "";
        public string NumeroReferenciaPago { get; set; } = "";
        public string EstadoPago { get; set; } = "";
        public DateTime FechaCreacionPago { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalProductos { get; set; }
        public int TotalClientes { get; set; }
        public int TotalFacturas { get; set; }
        public int FacturasPendientes { get; set; }
        public int TotalPagos { get; set; }
        public bool ApiDisponible { get; set; }
        public List<FacturaDto> UltimasFacturas { get; set; } = new();
    }

    public class FacturaDetalleViewModel
    {
        public FacturaDto Factura { get; set; } = new();
        public ClienteDto Cliente { get; set; } = new();
        public UsuarioDto Usuario { get; set; } = new();
        public List<DetalleFacturaDto> Detalles { get; set; } = new();
        public List<PagoDto> Pagos { get; set; } = new();
    }

    public class FacturaCrearViewModel
    {
        public string DpiClienteFactura { get; set; } = "";
        public string DpiUsuarioFactura { get; set; } = "";
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaEmisionFactura { get; set; }
        public DateTime? FechaVencimientoFactura { get; set; }
        public string EstadoFactura { get; set; } = "PENDIENTE";
        public string MonedaFactura { get; set; } = "GTQ";
        public List<DetalleFacturaCrearDto> Detalles { get; set; } = new();
    }

    public class DetalleFacturaCrearDto
    {
        public string CodigoProductoDetalleFactura { get; set; } = "";
        public int CantidadDetalleFactura { get; set; }
        public decimal PrecioUnitarioDetalleFactura { get; set; }
    }
}