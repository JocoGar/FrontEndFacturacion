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
            return _configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 5);
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

        private async Task<T?> GetApiObjectDirectAsync<T>(string endpoint)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                if (!response.IsSuccessStatusCode)
                    return default;

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private async Task<List<T>> GetApiListDirectAsync<T>(string endpoint)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                if (!response.IsSuccessStatusCode)
                    return new List<T>();

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
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

        private async Task<TResponse?> PostApiReturnAsync<TRequest, TResponse>(string endpoint, TRequest datos)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.PostAsJsonAsync(endpoint, datos, cts.Token);

                if (!response.IsSuccessStatusCode)
                    return default;

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
            }
            catch
            {
                return default;
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

        public async Task<FuenteDatosDto> ObtenerFuenteDatosAsync()
        {
            if (UsarMocks())
            {
                return new FuenteDatosDto
                {
                    Origen = "MOCK_JSON",
                    NombreApi = "Mocks locales",
                    Ambiente = "Fallback local",
                    Mensaje = "Datos obtenidos desde archivos JSON locales",
                    FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync("/health", cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return new FuenteDatosDto
                    {
                        Origen = "MOCK_JSON",
                        NombreApi = "Mocks locales",
                        Ambiente = "Fallback local",
                        Mensaje = "La API respondió con error. Se usaron mocks locales.",
                        FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }

                return new FuenteDatosDto
                {
                    Origen = "API_DUMMY",
                    NombreApi = "API Cluster / VIP",
                    Ambiente = "Integración con APIs",
                    Mensaje = "La API respondió correctamente desde /health.",
                    FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch
            {
                return new FuenteDatosDto
                {
                    Origen = "MOCK_JSON",
                    NombreApi = "Mocks locales",
                    Ambiente = "Fallback local",
                    Mensaje = "API no disponible. Se usaron mocks locales.",
                    FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
        }

        public async Task<List<CategoriaDto>> ObtenerCategoriasAsync()
        {
            return await GetListAsync<CategoriaDto>("/categorias", "categorias.json");
        }

        public async Task<CategoriaDto?> ObtenerCategoriaPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                return categorias.FirstOrDefault(c => c.IdCategoriaProducto == id);
            }

            var categoria = await GetApiObjectDirectAsync<CategoriaDto>($"/categorias/{id}");

            if (categoria == null && UsarFallbackSiApiFalla())
            {
                var categorias = await LeerListaMockAsync<CategoriaDto>("categorias.json");
                return categorias.FirstOrDefault(c => c.IdCategoriaProducto == id);
            }

            return categoria;
        }

        public async Task<bool> CrearCategoriaAsync(CategoriaDto categoria)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                categoria.IdCategoriaProducto = categorias.Any() ? categorias.Max(c => c.IdCategoriaProducto) + 1 : 1;
                categorias.Add(categoria);
                await EscribirListaMockAsync("categorias.json", categorias);
                return true;
            }

            return await PostApiAsync("/categorias", categoria);
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

                await EscribirListaMockAsync("categorias.json", categorias);
                return true;
            }

            return await PutApiAsync($"/categorias/{id}", categoria);
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

            return await DeleteApiAsync($"/categorias/{id}");
        }

        public async Task<List<ProductoDto>> ObtenerProductosAsync()
        {
            return await GetListAsync<ProductoDto>("/productos", "productos.json");
        }

        public async Task<ProductoDto?> ObtenerProductoPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                return productos.FirstOrDefault(p => p.IdProducto == id);
            }

            var producto = await GetApiObjectDirectAsync<ProductoDto>($"/productos/{id}");

            if (producto == null && UsarFallbackSiApiFalla())
            {
                var productos = await LeerListaMockAsync<ProductoDto>("productos.json");
                return productos.FirstOrDefault(p => p.IdProducto == id);
            }

            return producto;
        }

        public async Task<ProductoDto?> ObtenerProductoPorCodigoAsync(string codigo)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                return productos.FirstOrDefault(p => p.CodigoProducto == codigo);
            }

            var producto = await GetApiObjectDirectAsync<ProductoDto>($"/productos/codigo/{codigo}");

            if (producto == null && UsarFallbackSiApiFalla())
            {
                var productos = await LeerListaMockAsync<ProductoDto>("productos.json");
                return productos.FirstOrDefault(p => p.CodigoProducto == codigo);
            }

            return producto;
        }

        public async Task<bool> CrearProductoAsync(ProductoDto producto)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                var categorias = await ObtenerCategoriasAsync();

                producto.IdProducto = productos.Any() ? productos.Max(p => p.IdProducto) + 1 : 1;
                producto.NombreCategoriaProducto = categorias
                    .FirstOrDefault(c => c.IdCategoriaProducto == producto.IdCategoriaProducto)?
                    .NombreCategoriaProducto ?? "";

                productos.Add(producto);
                await EscribirListaMockAsync("productos.json", productos);
                return true;
            }

            return await PostApiAsync("/productos", producto);
        }

        public async Task<bool> ActualizarProductoAsync(int id, ProductoDto producto)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                var categorias = await ObtenerCategoriasAsync();
                var actual = productos.FirstOrDefault(p => p.IdProducto == id);

                if (actual == null)
                    return false;

                actual.CodigoProducto = producto.CodigoProducto;
                actual.IdCategoriaProducto = producto.IdCategoriaProducto;
                actual.NombreCategoriaProducto = categorias
                    .FirstOrDefault(c => c.IdCategoriaProducto == producto.IdCategoriaProducto)?
                    .NombreCategoriaProducto ?? "";
                actual.NombreProducto = producto.NombreProducto;
                actual.DescripcionProducto = producto.DescripcionProducto;
                actual.PrecioUnitarioProducto = producto.PrecioUnitarioProducto;

                await EscribirListaMockAsync("productos.json", productos);
                return true;
            }

            return await PutApiAsync($"/productos/{id}", producto);
        }

        public async Task<bool> EliminarProductoAsync(int id)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                productos.RemoveAll(p => p.IdProducto == id);
                await EscribirListaMockAsync("productos.json", productos);
                return true;
            }

            return await DeleteApiAsync($"/productos/{id}");
        }

        public async Task<List<ClienteDto>> ObtenerClientesAsync()
        {
            return await GetListAsync<ClienteDto>("/clientes", "clientes.json");
        }

        public async Task<ClienteDto?> ObtenerClientePorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                return clientes.FirstOrDefault(c => c.IdCliente == id);
            }

            var cliente = await GetApiObjectDirectAsync<ClienteDto>($"/clientes/{id}");

            if (cliente == null && UsarFallbackSiApiFalla())
            {
                var clientes = await LeerListaMockAsync<ClienteDto>("clientes.json");
                return clientes.FirstOrDefault(c => c.IdCliente == id);
            }

            return cliente;
        }

        public async Task<ClienteDto?> ObtenerClientePorDpiAsync(string dpi)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                return clientes.FirstOrDefault(c => c.DpiCliente == dpi);
            }

            var cliente = await GetApiObjectDirectAsync<ClienteDto>($"/clientes/dpi/{dpi}");

            if (cliente == null && UsarFallbackSiApiFalla())
            {
                var clientes = await LeerListaMockAsync<ClienteDto>("clientes.json");
                return clientes.FirstOrDefault(c => c.DpiCliente == dpi);
            }

            return cliente;
        }

        public async Task<bool> CrearClienteAsync(ClienteDto cliente)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                cliente.IdCliente = clientes.Any() ? clientes.Max(c => c.IdCliente) + 1 : 1;
                clientes.Add(cliente);
                await EscribirListaMockAsync("clientes.json", clientes);
                return true;
            }

            return await PostApiAsync("/clientes", cliente);
        }

        public async Task<bool> ActualizarClienteAsync(int id, ClienteDto cliente)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                var actual = clientes.FirstOrDefault(c => c.IdCliente == id);

                if (actual == null)
                    return false;

                actual.DpiCliente = cliente.DpiCliente;
                actual.NitCliente = cliente.NitCliente;
                actual.NombreCliente = cliente.NombreCliente;
                actual.ApellidoCliente = cliente.ApellidoCliente;
                actual.CorreoCliente = cliente.CorreoCliente;
                actual.TelefonoCliente = cliente.TelefonoCliente;

                await EscribirListaMockAsync("clientes.json", clientes);
                return true;
            }

            return await PutApiAsync($"/clientes/{id}", cliente);
        }

        public async Task<bool> EliminarClienteAsync(int id)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                clientes.RemoveAll(c => c.IdCliente == id);
                await EscribirListaMockAsync("clientes.json", clientes);
                return true;
            }

            return await DeleteApiAsync($"/clientes/{id}");
        }

        public async Task<List<RolDto>> ObtenerRolesAsync()
        {
            return await GetListAsync<RolDto>("/roles", "roles.json");
        }

        public async Task<RolDto?> ObtenerRolPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var roles = await ObtenerRolesAsync();
                return roles.FirstOrDefault(r => r.IdRol == id);
            }

            var rol = await GetApiObjectDirectAsync<RolDto>($"/roles/{id}");

            if (rol == null && UsarFallbackSiApiFalla())
            {
                var roles = await LeerListaMockAsync<RolDto>("roles.json");
                return roles.FirstOrDefault(r => r.IdRol == id);
            }

            return rol;
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

            return await PostApiAsync("/roles", rol);
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

            return await PutApiAsync($"/roles/{id}", rol);
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

            return await DeleteApiAsync($"/roles/{id}");
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosAsync()
        {
            return await GetListAsync<UsuarioDto>("/usuarios", "usuarios.json");
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorDpiAsync(string dpi)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                return usuarios.FirstOrDefault(u => u.DpiUsuario == dpi);
            }

            var usuario = await GetApiObjectDirectAsync<UsuarioDto>($"/usuarios/{dpi}");

            if (usuario == null && UsarFallbackSiApiFalla())
            {
                var usuarios = await LeerListaMockAsync<UsuarioDto>("usuarios.json");
                return usuarios.FirstOrDefault(u => u.DpiUsuario == dpi);
            }

            return usuario;
        }

        public async Task<bool> CrearUsuarioAsync(UsuarioDto usuario)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                var roles = await ObtenerRolesAsync();

                usuario.NombreRol = roles.FirstOrDefault(r => r.IdRol == usuario.IdRol)?.NombreRol ?? "";

                usuarios.Add(usuario);
                await EscribirListaMockAsync("usuarios.json", usuarios);
                return true;
            }

            return await PostApiAsync("/usuarios", usuario);
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

                if (!string.IsNullOrWhiteSpace(usuario.PasswordUsuario))
                    actual.PasswordUsuario = usuario.PasswordUsuario;

                await EscribirListaMockAsync("usuarios.json", usuarios);
                return true;
            }

            return await PutApiAsync($"/usuarios/{dpi}", usuario);
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

            return await DeleteApiAsync($"/usuarios/{dpi}");
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync()
        {
            return await GetListAsync<FacturaDto>("/facturas", "facturas.json");
        }

        public async Task<FacturaDetalleViewModel> ObtenerDetalleFacturaAsync(int id)
        {
            if (UsarMocks())
            {
                return await LeerObjetoMockAsync(
                    $"factura-detalle-{id}.json",
                    new FacturaDetalleViewModel()
                );
            }

            var factura = await GetApiObjectDirectAsync<FacturaDto>($"/facturas/{id}");

            if (factura == null || factura.IdFactura == 0)
            {
                if (UsarFallbackSiApiFalla())
                {
                    return await LeerObjetoMockAsync(
                        $"factura-detalle-{id}.json",
                        new FacturaDetalleViewModel()
                    );
                }

                return new FacturaDetalleViewModel();
            }

            var cliente = await GetApiObjectDirectAsync<ClienteDto>($"/clientes/{factura.IdClienteFactura}");
            var usuario = await GetApiObjectDirectAsync<UsuarioDto>($"/usuarios/{factura.DpiUsuarioFactura}");
            var detalles = await GetApiListDirectAsync<DetalleFacturaDto>($"/detalles-factura/factura/{id}");
            var pagos = await GetApiListDirectAsync<PagoDto>($"/pagos/factura/{id}");

            if (cliente != null)
            {
                factura.DpiCliente = cliente.DpiCliente;
                factura.NitCliente = cliente.NitCliente;
                factura.NombreCliente = cliente.NombreCliente;
                factura.ApellidoCliente = cliente.ApellidoCliente;
            }

            if (usuario != null)
            {
                factura.NombreUsuario = usuario.NombreUsuario;
                factura.ApellidoUsuario = usuario.ApellidoUsuario;
            }

            foreach (var detalle in detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.NombreProducto))
                {
                    var producto = await ObtenerProductoPorCodigoAsync(detalle.CodigoProductoDetalleFactura);
                    detalle.NombreProducto = producto?.NombreProducto ?? "";
                }
            }

            foreach (var pago in pagos)
            {
                pago.NumeroFactura = factura.NumeroFactura;
            }

            return new FacturaDetalleViewModel
            {
                Factura = factura,
                Cliente = cliente ?? new ClienteDto(),
                Usuario = usuario ?? new UsuarioDto(),
                Detalles = detalles,
                Pagos = pagos
            };
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

                var cliente = clientes.FirstOrDefault(c => c.IdCliente == model.IdClienteFactura);
                var usuario = usuarios.FirstOrDefault(u => u.DpiUsuario == model.DpiUsuarioFactura);

                var subtotal = model.Detalles.Sum(d => d.CantidadDetalleFactura * d.PrecioUnitarioDetalleFactura);

                var factura = new FacturaDto
                {
                    IdFactura = nuevoId,
                    IdClienteFactura = model.IdClienteFactura,
                    DpiCliente = cliente?.DpiCliente ?? "",
                    NitCliente = cliente?.NitCliente ?? "",
                    NombreCliente = cliente?.NombreCliente ?? "",
                    ApellidoCliente = cliente?.ApellidoCliente ?? "",
                    DpiUsuarioFactura = model.DpiUsuarioFactura,
                    NombreUsuario = usuario?.NombreUsuario ?? "",
                    ApellidoUsuario = usuario?.ApellidoUsuario ?? "",
                    NumeroFactura = model.NumeroFactura,
                    FechaEmisionFactura = model.FechaEmisionFactura,
                    SubtotalFactura = subtotal,
                    TotalFactura = subtotal,
                    MonedaFactura = model.MonedaFactura
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

            var subtotalApi = model.Detalles.Sum(d => d.CantidadDetalleFactura * d.PrecioUnitarioDetalleFactura);

            var facturaApi = new FacturaDto
            {
                IdClienteFactura = model.IdClienteFactura,
                DpiUsuarioFactura = model.DpiUsuarioFactura,
                NumeroFactura = model.NumeroFactura,
                FechaEmisionFactura = model.FechaEmisionFactura,
                SubtotalFactura = subtotalApi,
                TotalFactura = subtotalApi,
                MonedaFactura = model.MonedaFactura
            };

            var facturaCreada = await PostApiReturnAsync<FacturaDto, FacturaDto>("/facturas", facturaApi);

            if (facturaCreada == null || facturaCreada.IdFactura == 0)
                return false;

            foreach (var detalle in model.Detalles)
            {
                var detalleApi = new DetalleFacturaDto
                {
                    IdFacturaDetalleFactura = facturaCreada.IdFactura,
                    CodigoProductoDetalleFactura = detalle.CodigoProductoDetalleFactura,
                    CantidadDetalleFactura = detalle.CantidadDetalleFactura,
                    PrecioUnitarioDetalleFactura = detalle.PrecioUnitarioDetalleFactura
                };

                await PostApiAsync("/detalles-factura", detalleApi);
            }

            return true;
        }

        public async Task<bool> ActualizarFacturaAsync(int id, FacturaDto factura)
        {
            if (UsarMocks())
            {
                var facturas = await ObtenerFacturasAsync();
                var actual = facturas.FirstOrDefault(f => f.IdFactura == id);

                if (actual == null)
                    return false;

                actual.IdClienteFactura = factura.IdClienteFactura;
                actual.DpiUsuarioFactura = factura.DpiUsuarioFactura;
                actual.NumeroFactura = factura.NumeroFactura;
                actual.FechaEmisionFactura = factura.FechaEmisionFactura;
                actual.SubtotalFactura = factura.SubtotalFactura;
                actual.TotalFactura = factura.TotalFactura;
                actual.MonedaFactura = factura.MonedaFactura;

                await EscribirListaMockAsync("facturas.json", facturas);
                return true;
            }

            return await PutApiAsync($"/facturas/{id}", factura);
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

            return await DeleteApiAsync($"/facturas/{id}");
        }

        public async Task<List<DetalleFacturaDto>> ObtenerDetallesFacturaAsync()
        {
            return await GetListAsync<DetalleFacturaDto>("/detalles-factura", "detalles-factura.json");
        }

        public async Task<List<DetalleFacturaDto>> ObtenerDetallesPorFacturaAsync(int idFactura)
        {
            if (UsarMocks())
            {
                var detalle = await LeerObjetoMockAsync(
                    $"factura-detalle-{idFactura}.json",
                    new FacturaDetalleViewModel()
                );

                return detalle.Detalles;
            }

            return await GetApiListDirectAsync<DetalleFacturaDto>($"/detalles-factura/factura/{idFactura}");
        }

        public async Task<DetalleFacturaDto?> ObtenerDetalleFacturaLineaPorIdAsync(int id)
        {
            return await GetApiObjectDirectAsync<DetalleFacturaDto>($"/detalles-factura/{id}");
        }

        public async Task<bool> CrearDetalleFacturaAsync(DetalleFacturaDto detalle)
        {
            return await PostApiAsync("/detalles-factura", detalle);
        }

        public async Task<bool> ActualizarDetalleFacturaAsync(int id, DetalleFacturaDto detalle)
        {
            return await PutApiAsync($"/detalles-factura/{id}", detalle);
        }

        public async Task<bool> EliminarDetalleFacturaAsync(int id)
        {
            return await DeleteApiAsync($"/detalles-factura/{id}");
        }

        public async Task<List<PagoDto>> ObtenerPagosAsync()
        {
            return await GetListAsync<PagoDto>("/pagos", "pagos.json");
        }

        public async Task<List<PagoDto>> ObtenerPagosPorFacturaAsync(int idFactura)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                return pagos.Where(p => p.IdFacturaPago == idFactura).ToList();
            }

            var pagosApi = await GetApiListDirectAsync<PagoDto>($"/pagos/factura/{idFactura}");
            var facturas = await ObtenerFacturasAsync();

            foreach (var pago in pagosApi)
            {
                pago.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";
            }

            return pagosApi;
        }

        public async Task<PagoDto?> ObtenerPagoPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                return pagos.FirstOrDefault(p => p.IdPago == id);
            }

            var pago = await GetApiObjectDirectAsync<PagoDto>($"/pagos/{id}");

            if (pago == null && UsarFallbackSiApiFalla())
            {
                var pagos = await LeerListaMockAsync<PagoDto>("pagos.json");
                return pagos.FirstOrDefault(p => p.IdPago == id);
            }

            if (pago != null)
            {
                var facturas = await ObtenerFacturasAsync();
                pago.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";
            }

            return pago;
        }

        public async Task<bool> CrearPagoAsync(PagoDto pago)
        {
            if (UsarMocks())
            {
                var pagos = await ObtenerPagosAsync();
                var facturas = await ObtenerFacturasAsync();

                pago.IdPago = pagos.Any() ? pagos.Max(p => p.IdPago) + 1 : 1;
                pago.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";

                pagos.Add(pago);
                await EscribirListaMockAsync("pagos.json", pagos);

                return true;
            }

            return await PostApiAsync("/pagos", pago);
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

                await EscribirListaMockAsync("pagos.json", pagos);
                return true;
            }

            return await PutApiAsync($"/pagos/{id}", pago);
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

            return await DeleteApiAsync($"/pagos/{id}");
        }
    }

    public class CategoriaDto
    {
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoriaProducto { get; set; } = "";
        public string DescripcionCategoriaProducto { get; set; } = "";
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
    }

    public class ClienteDto
    {
        public int IdCliente { get; set; }
        public string DpiCliente { get; set; } = "";
        public string NitCliente { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string ApellidoCliente { get; set; } = "";
        public string CorreoCliente { get; set; } = "";
        public string TelefonoCliente { get; set; } = "";
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
    }

    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public int IdClienteFactura { get; set; }
        public string DpiCliente { get; set; } = "";
        public string NitCliente { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string ApellidoCliente { get; set; } = "";
        public string DpiUsuarioFactura { get; set; } = "";
        public string NombreUsuario { get; set; } = "";
        public string ApellidoUsuario { get; set; } = "";
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaEmisionFactura { get; set; }
        public decimal SubtotalFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public string MonedaFactura { get; set; } = "GTQ";

        public string Cliente => $"{NombreCliente} {ApellidoCliente}".Trim();
        public string Usuario => $"{NombreUsuario} {ApellidoUsuario}".Trim();
    }

    public class DetalleFacturaDto
    {
        public int IdDetalleFactura { get; set; }
        public int IdFacturaDetalleFactura { get; set; }
        public string CodigoProductoDetalleFactura { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public decimal CantidadDetalleFactura { get; set; }
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
        public int IdClienteFactura { get; set; }
        public string DpiUsuarioFactura { get; set; } = "";
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaEmisionFactura { get; set; }
        public string MonedaFactura { get; set; } = "GTQ";
        public List<DetalleFacturaCrearDto> Detalles { get; set; } = new();
    }

    public class DetalleFacturaCrearDto
    {
        public string CodigoProductoDetalleFactura { get; set; } = "";
        public decimal CantidadDetalleFactura { get; set; }
        public decimal PrecioUnitarioDetalleFactura { get; set; }
    }

    public class FuenteDatosDto
    {
        public string Origen { get; set; } = "";
        public string NombreApi { get; set; } = "";
        public string Ambiente { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string FechaRespuesta { get; set; } = "";
    }
}