using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;

namespace FrontendFacturacion.Services
{
    public class FacturacionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly JsonSerializerOptions _mockJsonOptions;
        private readonly JsonSerializerOptions _apiJsonOptions;

        public string UltimoErrorApi { get; private set; } = "";
        public int? UltimoCodigoEstadoApi { get; private set; }

        private void LimpiarErrorApi()
        {
            UltimoErrorApi = "";
            UltimoCodigoEstadoApi = null;
        }

        private async Task<string> LeerMensajeErrorAsync(HttpResponseMessage response, CancellationToken token)
        {
            var contenido = await response.Content.ReadAsStringAsync(token);

            if (string.IsNullOrWhiteSpace(contenido))
                return $"La API respondió {(int)response.StatusCode} {response.ReasonPhrase}.";

            try
            {
                using var doc = JsonDocument.Parse(contenido);

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var message))
                        return message.GetString() ?? contenido;

                    if (doc.RootElement.TryGetProperty("mensaje", out var mensaje))
                        return mensaje.GetString() ?? contenido;

                    if (doc.RootElement.TryGetProperty("error", out var error))
                        return error.GetString() ?? contenido;
                }
            }
            catch
            {
                // Si la API devuelve HTML o texto plano, se muestra el contenido resumido.
            }

            return contenido.Length > 250 ? contenido.Substring(0, 250) : contenido;
        }
        public async Task<UsuarioDto?> LoginAsync(string usuario, string password)
        {
            LimpiarErrorApi();

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                UltimoErrorApi = "Debe ingresar usuario y contraseña.";
                return null;
            }

            if (usuario.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "123")
            {
                return new UsuarioDto
                {
                    DpiUsuario = "admin",
                    NombreUsuario = "Administrador",
                    ApellidoUsuario = "Local",
                    CorreoUsuario = "admin@local",
                    PasswordUsuario = "123",
                    IdRol = 1,
                    NombreRol = "Administrador"
                };
            }

            if (UsarMocks())
            {
                var usuariosMock = await ObtenerUsuariosAsync();

                return usuariosMock.FirstOrDefault(u =>
                    (u.DpiUsuario.Equals(usuario, StringComparison.OrdinalIgnoreCase) ||
                     u.CorreoUsuario.Equals(usuario, StringComparison.OrdinalIgnoreCase)) &&
                    u.PasswordUsuario == password);
            }

            var usuarioApi = await ObtenerUsuarioPorDpiAsync(usuario);

            if (usuarioApi != null)
            {
                if (usuarioApi.PasswordUsuario == password)
                    return usuarioApi;

                UltimoErrorApi = "Usuario o contraseña incorrectos.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(UltimoErrorApi) || UltimoCodigoEstadoApi == 404)
                UltimoErrorApi = "Usuario o contraseña incorrectos.";

            return null;
        }
        private void RegistrarErrorApi(string endpoint, HttpStatusCode statusCode, string mensaje)
        {
            UltimoCodigoEstadoApi = (int)statusCode;

            UltimoErrorApi = statusCode switch
            {
                HttpStatusCode.ServiceUnavailable =>
                    $"La API real no está disponible en este momento. Endpoint: /api/{endpoint}. Código 503.",

                HttpStatusCode.BadGateway =>
                    $"El balanceador no pudo comunicarse con los nodos de API. Endpoint: /api/{endpoint}. Código 502.",

                HttpStatusCode.GatewayTimeout =>
                    $"La API tardó demasiado en responder. Endpoint: /api/{endpoint}. Código 504.",

                HttpStatusCode.NotFound =>
                    $"El recurso solicitado no existe en la API. Endpoint: /api/{endpoint}. Código 404.",

                HttpStatusCode.Unauthorized =>
                    "Usuario o contraseña incorrectos.",

                HttpStatusCode.Conflict =>
                    mensaje,

                HttpStatusCode.BadRequest =>
                    mensaje,

                _ =>
                    $"Error al consumir la API real. Endpoint: /api/{endpoint}. Código {(int)statusCode}. Detalle: {mensaje}"
            };
        }

        private void RegistrarExcepcionApi(string endpoint, Exception ex)
        {
            UltimoCodigoEstadoApi = null;

            UltimoErrorApi = ex switch
            {
                TaskCanceledException =>
                    $"La API no respondió dentro del tiempo configurado. Endpoint: /api/{endpoint}.",

                HttpRequestException =>
                    $"No se pudo establecer comunicación con la API real. Endpoint: /api/{endpoint}. Verifique la VIP, HAProxy o los nodos de API.",

                _ =>
                    $"Error inesperado al consumir la API real. Endpoint: /api/{endpoint}. Detalle: {ex.Message}"
            };
        }

        public FacturacionApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _environment = environment;

            _mockJsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            _apiJsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
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

        private string Endpoint(string endpoint)
        {
            return endpoint.TrimStart('/');
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
            return JsonSerializer.Deserialize<List<T>>(json, _mockJsonOptions) ?? new List<T>();
        }

        private async Task EscribirListaMockAsync<T>(string archivo, List<T> datos)
        {
            var ruta = RutaMock(archivo);
            var carpeta = Path.GetDirectoryName(ruta);

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta!);

            var json = JsonSerializer.Serialize(datos, _mockJsonOptions);
            await File.WriteAllTextAsync(ruta, json);
        }

        private async Task<T> LeerObjetoMockAsync<T>(string archivo, T valorDefault)
        {
            var ruta = RutaMock(archivo);

            if (!File.Exists(ruta))
                return valorDefault;

            var json = await File.ReadAllTextAsync(ruta);
            return JsonSerializer.Deserialize<T>(json, _mockJsonOptions) ?? valorDefault;
        }

        private async Task EscribirObjetoMockAsync<T>(string archivo, T datos)
        {
            var ruta = RutaMock(archivo);
            var carpeta = Path.GetDirectoryName(ruta);

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta!);

            var json = JsonSerializer.Serialize(datos, _mockJsonOptions);
            await File.WriteAllTextAsync(ruta, json);
        }

        private async Task<FacturaDetalleViewModel> LeerDetalleFacturaMockAsync(int id)
        {
            var normal = await LeerObjetoMockAsync(
                $"factura-detalle-{id}.json",
                new FacturaDetalleViewModel()
            );

            if (normal.Factura.IdFactura != 0)
                return normal;

            return await LeerObjetoMockAsync(
                $"factrura-detalle-{id}.json",
                new FacturaDetalleViewModel()
            );
        }

        private async Task<List<TRaw>?> GetApiListAsync<TRaw>(string endpoint, string? arrayProperty = null)
        {
            LimpiarErrorApi();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync(Endpoint(endpoint), cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var mensaje = await LeerMensajeErrorAsync(response, cts.Token);
                    RegistrarErrorApi(endpoint, response.StatusCode, mensaje);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<TRaw>();

                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<TRaw>>(json, _apiJsonOptions) ?? new List<TRaw>();
                }

                if (!string.IsNullOrWhiteSpace(arrayProperty) &&
                    doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty(arrayProperty, out var arrayElement) &&
                    arrayElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<TRaw>>(arrayElement.GetRawText(), _apiJsonOptions) ?? new List<TRaw>();
                }

                UltimoErrorApi = $"La respuesta de /api/{endpoint} no tiene el formato esperado.";
                return null;
            }
            catch (Exception ex)
            {
                RegistrarExcepcionApi(endpoint, ex);
                return null;
            }
        }

        private async Task<TRaw?> GetApiObjectAsync<TRaw>(string endpoint, string? objectProperty = null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync(Endpoint(endpoint), cts.Token);

                if (!response.IsSuccessStatusCode)
                    return default;

                var json = await response.Content.ReadAsStringAsync(cts.Token);

                if (string.IsNullOrWhiteSpace(json))
                    return default;

                using var doc = JsonDocument.Parse(json);

                if (!string.IsNullOrWhiteSpace(objectProperty) &&
                    doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty(objectProperty, out var objectElement))
                {
                    return JsonSerializer.Deserialize<TRaw>(objectElement.GetRawText(), _apiJsonOptions);
                }

                return JsonSerializer.Deserialize<TRaw>(json, _apiJsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private async Task<bool> PostApiAsync<T>(string endpoint, T datos)
        {
            LimpiarErrorApi();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.PostAsJsonAsync(Endpoint(endpoint), datos, _apiJsonOptions, cts.Token);

                if (response.IsSuccessStatusCode)
                    return true;

                var mensaje = await LeerMensajeErrorAsync(response, cts.Token);
                RegistrarErrorApi(endpoint, response.StatusCode, mensaje);
                return false;
            }
            catch (Exception ex)
            {
                RegistrarExcepcionApi(endpoint, ex);
                return false;
            }
        }

        private async Task<bool> PutApiAsync<T>(string endpoint, T datos)
        {
            LimpiarErrorApi();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.PutAsJsonAsync(Endpoint(endpoint), datos, _apiJsonOptions, cts.Token);

                if (response.IsSuccessStatusCode)
                    return true;

                var mensaje = await LeerMensajeErrorAsync(response, cts.Token);
                RegistrarErrorApi(endpoint, response.StatusCode, mensaje);
                return false;
            }
            catch (Exception ex)
            {
                RegistrarExcepcionApi(endpoint, ex);
                return false;
            }
        }

        private async Task<bool> DeleteApiAsync(string endpoint)
        {
            LimpiarErrorApi();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.DeleteAsync(Endpoint(endpoint), cts.Token);

                if (response.IsSuccessStatusCode)
                    return true;

                var mensaje = await LeerMensajeErrorAsync(response, cts.Token);
                RegistrarErrorApi(endpoint, response.StatusCode, mensaje);
                return false;
            }
            catch (Exception ex)
            {
                RegistrarExcepcionApi(endpoint, ex);
                return false;
            }
        }
        private async Task<TRaw?> PostApiObjectAsync<TRequest, TRaw>(string endpoint, TRequest datos, string objectProperty)
        {
            LimpiarErrorApi();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.PostAsJsonAsync(Endpoint(endpoint), datos, _apiJsonOptions, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var mensaje = await LeerMensajeErrorAsync(response, cts.Token);
                    RegistrarErrorApi(endpoint, response.StatusCode, mensaje);
                    return default;
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);

                if (string.IsNullOrWhiteSpace(json))
                    return default;

                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty(objectProperty, out var objectElement))
                {
                    return JsonSerializer.Deserialize<TRaw>(objectElement.GetRawText(), _apiJsonOptions);
                }

                return JsonSerializer.Deserialize<TRaw>(json, _apiJsonOptions);
            }
            catch (Exception ex)
            {
                RegistrarExcepcionApi(endpoint, ex);
                return default;
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
                    Ambiente = "Modo de pruebas",
                    Mensaje = "El frontend está usando archivos JSON locales porque UseMockData está activado.",
                    FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }

            var health = await GetApiObjectAsync<ApiHealthDto>("health");

            if (health != null && health.Status == "ok")
            {
                return new FuenteDatosDto
                {
                    Origen = "API_REAL",
                    NombreApi = "API real de facturación",
                    Ambiente = "Integración real",
                    Mensaje = $"API real disponible vía /api/health. Nodo API: {health.Hostname}.",
                    FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }

            return new FuenteDatosDto
            {
                Origen = "API_CAIDA",
                NombreApi = "API real no disponible",
                Ambiente = "Error controlado",
                Mensaje = string.IsNullOrWhiteSpace(UltimoErrorApi)
                    ? "La API real no respondió correctamente."
                    : UltimoErrorApi,
                FechaRespuesta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public async Task<List<CategoriaDto>> ObtenerCategoriasAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<CategoriaDto>("categorias.json");

            var raw = await GetApiListAsync<RawCategoriaDto>("categorias");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<CategoriaDto>("categorias.json") : new List<CategoriaDto>();

            return raw.Select(MapCategoria)
          .OrderBy(c => c.IdCategoriaProducto)
          .ToList();
        }

        public async Task<CategoriaDto?> ObtenerCategoriaPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var categorias = await ObtenerCategoriasAsync();
                return categorias.FirstOrDefault(c => c.IdCategoriaProducto == id);
            }

            var raw = await GetApiObjectAsync<RawCategoriaDto>($"categorias/{id}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var categorias = await LeerListaMockAsync<CategoriaDto>("categorias.json");
                return categorias.FirstOrDefault(c => c.IdCategoriaProducto == id);
            }

            return MapCategoria(raw);
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

            return await PostApiAsync("categorias", new
            {
                nombre_categoria_producto = categoria.NombreCategoriaProducto,
                descripcion_categoria_producto = categoria.DescripcionCategoriaProducto
            });
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

            return await PutApiAsync($"categorias/{id}", new
            {
                nombre_categoria_producto = categoria.NombreCategoriaProducto,
                descripcion_categoria_producto = categoria.DescripcionCategoriaProducto
            });
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

            return await DeleteApiAsync($"categorias/{id}");
        }

        public async Task<List<ProductoDto>> ObtenerProductosAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<ProductoDto>("productos.json");

            var raw = await GetApiListAsync<RawProductoDto>("productos");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<ProductoDto>("productos.json") : new List<ProductoDto>();

            return raw.Select(MapProducto)
          .OrderBy(p => p.IdProducto)
          .ToList();
        }

        public async Task<ProductoDto?> ObtenerProductoPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                return productos.FirstOrDefault(p => p.IdProducto == id);
            }

            var raw = await GetApiObjectAsync<RawProductoDto>($"productos/{id}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var productos = await LeerListaMockAsync<ProductoDto>("productos.json");
                return productos.FirstOrDefault(p => p.IdProducto == id);
            }

            return MapProducto(raw);
        }

        public async Task<ProductoDto?> ObtenerProductoPorCodigoAsync(string codigo)
        {
            if (UsarMocks())
            {
                var productos = await ObtenerProductosAsync();
                return productos.FirstOrDefault(p => p.CodigoProducto == codigo);
            }

            var raw = await GetApiObjectAsync<RawProductoDto>($"productos/codigo/{codigo}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var productos = await LeerListaMockAsync<ProductoDto>("productos.json");
                return productos.FirstOrDefault(p => p.CodigoProducto == codigo);
            }

            return MapProducto(raw);
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

            return await PostApiAsync("productos", new
            {
                codigo_producto = producto.CodigoProducto,
                id_categoria_producto = producto.IdCategoriaProducto,
                nombre_producto = producto.NombreProducto,
                descripcion_producto = producto.DescripcionProducto,
                precio_unitario_producto = producto.PrecioUnitarioProducto
            });
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

            return await PutApiAsync($"productos/{id}", new
            {
                codigo_producto = producto.CodigoProducto,
                id_categoria_producto = producto.IdCategoriaProducto,
                nombre_producto = producto.NombreProducto,
                descripcion_producto = producto.DescripcionProducto,
                precio_unitario_producto = producto.PrecioUnitarioProducto
            });
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

            return await DeleteApiAsync($"productos/{id}");
        }

        public async Task<List<ClienteDto>> ObtenerClientesAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<ClienteDto>("clientes.json");

            var raw = await GetApiListAsync<RawClienteDto>("clientes");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<ClienteDto>("clientes.json") : new List<ClienteDto>();

            return raw.Select(MapCliente)
          .OrderBy(c => c.IdCliente)
          .ToList();
        }

        public async Task<ClienteDto?> ObtenerClientePorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                return clientes.FirstOrDefault(c => c.IdCliente == id);
            }

            var raw = await GetApiObjectAsync<RawClienteDto>($"clientes/{id}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var clientes = await LeerListaMockAsync<ClienteDto>("clientes.json");
                return clientes.FirstOrDefault(c => c.IdCliente == id);
            }

            return MapCliente(raw);
        }

        public async Task<ClienteDto?> ObtenerClientePorDpiAsync(string dpi)
        {
            if (UsarMocks())
            {
                var clientes = await ObtenerClientesAsync();
                return clientes.FirstOrDefault(c => c.DpiCliente == dpi);
            }

            var raw = await GetApiObjectAsync<RawClienteDto>($"clientes/dpi/{dpi}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var clientes = await LeerListaMockAsync<ClienteDto>("clientes.json");
                return clientes.FirstOrDefault(c => c.DpiCliente == dpi);
            }

            return MapCliente(raw);
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

            return await PostApiAsync("clientes", new
            {
                dpi_cliente = cliente.DpiCliente,
                nit_cliente = cliente.NitCliente,
                nombre_cliente = cliente.NombreCliente,
                apellido_cliente = cliente.ApellidoCliente,
                correo_cliente = cliente.CorreoCliente,
                telefono_cliente = cliente.TelefonoCliente
            });
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

            return await PutApiAsync($"clientes/{id}", new
            {
                dpi_cliente = cliente.DpiCliente,
                nit_cliente = cliente.NitCliente,
                nombre_cliente = cliente.NombreCliente,
                apellido_cliente = cliente.ApellidoCliente,
                correo_cliente = cliente.CorreoCliente,
                telefono_cliente = cliente.TelefonoCliente
            });
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

            return await DeleteApiAsync($"clientes/{id}");
        }

        public async Task<List<RolDto>> ObtenerRolesAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<RolDto>("roles.json");

            var raw = await GetApiListAsync<RawRolDto>("roles");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<RolDto>("roles.json") : new List<RolDto>();

            return raw.Select(MapRol)
          .OrderBy(r => r.IdRol)
          .ToList();
        }

        public async Task<RolDto?> ObtenerRolPorIdAsync(int id)
        {
            if (UsarMocks())
            {
                var roles = await ObtenerRolesAsync();
                return roles.FirstOrDefault(r => r.IdRol == id);
            }

            var raw = await GetApiObjectAsync<RawRolDto>($"roles/{id}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var roles = await LeerListaMockAsync<RolDto>("roles.json");
                return roles.FirstOrDefault(r => r.IdRol == id);
            }

            return MapRol(raw);
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

            return await PostApiAsync("roles", new
            {
                nombre_rol = rol.NombreRol
            });
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

            return await PutApiAsync($"roles/{id}", new
            {
                nombre_rol = rol.NombreRol
            });
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

            return await DeleteApiAsync($"roles/{id}");
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<UsuarioDto>("usuarios.json");

            var raw = await GetApiListAsync<RawUsuarioDto>("usuarios");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<UsuarioDto>("usuarios.json") : new List<UsuarioDto>();

            return raw.Select(MapUsuario)
          .OrderBy(u => u.DpiUsuario)
          .ToList();
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorDpiAsync(string dpi)
        {
            if (UsarMocks())
            {
                var usuarios = await ObtenerUsuariosAsync();
                return usuarios.FirstOrDefault(u => u.DpiUsuario == dpi);
            }

            var raw = await GetApiObjectAsync<RawUsuarioDto>($"usuarios/{dpi}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var usuarios = await LeerListaMockAsync<UsuarioDto>("usuarios.json");
                return usuarios.FirstOrDefault(u => u.DpiUsuario == dpi);
            }

            return MapUsuario(raw);
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

            return await PostApiAsync("usuarios", new
            {
                dpi_usuario = usuario.DpiUsuario,
                nombre_usuario = usuario.NombreUsuario,
                apellido_usuario = usuario.ApellidoUsuario,
                correo_usuario = usuario.CorreoUsuario,
                password_usuario = usuario.PasswordUsuario,
                id_rol = usuario.IdRol
            });
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

            var datos = new Dictionary<string, object?>
            {
                ["nombre_usuario"] = usuario.NombreUsuario,
                ["apellido_usuario"] = usuario.ApellidoUsuario,
                ["correo_usuario"] = usuario.CorreoUsuario,
                ["id_rol"] = usuario.IdRol
            };

            if (!string.IsNullOrWhiteSpace(usuario.PasswordUsuario))
                datos["password_usuario"] = usuario.PasswordUsuario;

            return await PutApiAsync($"usuarios/{dpi}", datos);
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

            return await DeleteApiAsync($"usuarios/{dpi}");
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<FacturaDto>("facturas.json");

            var raw = await GetApiListAsync<RawFacturaDto>("facturas", "facturas");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<FacturaDto>("facturas.json") : new List<FacturaDto>();

            return raw.Select(MapFactura)
          .OrderBy(f => f.IdFactura)
          .ToList();
        }

        public async Task<FacturaDetalleViewModel> ObtenerDetalleFacturaAsync(int id)
        {
            if (UsarMocks())
                return await LeerDetalleFacturaMockAsync(id);

            var rawFactura = await GetApiObjectAsync<RawFacturaDto>($"facturas/{id}", "factura");

            if (rawFactura == null || rawFactura.IdFactura == 0)
            {
                return UsarFallbackSiApiFalla()
                    ? await LeerDetalleFacturaMockAsync(id)
                    : new FacturaDetalleViewModel();
            }

            var factura = MapFactura(rawFactura);
            var cliente = rawFactura.Cliente != null
                ? MapCliente(rawFactura.Cliente)
                : await ObtenerClientePorIdAsync(factura.IdClienteFactura) ?? new ClienteDto();

            var usuario = rawFactura.Usuario != null
                ? MapUsuario(rawFactura.Usuario)
                : await ObtenerUsuarioPorDpiAsync(factura.DpiUsuarioFactura) ?? new UsuarioDto();

            var detalles = rawFactura.Detalles != null && rawFactura.Detalles.Any()
                ? rawFactura.Detalles.Select(MapDetalle).ToList()
                : await ObtenerDetallesPorFacturaAsync(id);

            foreach (var detalle in detalles)
            {
                if (string.IsNullOrWhiteSpace(detalle.NombreProducto))
                {
                    var producto = await ObtenerProductoPorCodigoAsync(detalle.CodigoProductoDetalleFactura);
                    detalle.NombreProducto = producto?.NombreProducto ?? "";
                }
            }

            var pagos = await ObtenerPagosPorFacturaAsync(id);

            return new FacturaDetalleViewModel
            {
                Factura = factura,
                Cliente = cliente,
                Usuario = usuario,
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

            var request = new ApiCrearFacturaRequest
            {
                IdCliente = model.IdClienteFactura,
                DpiUsuario = model.DpiUsuarioFactura,
                Detalles = model.Detalles
                    .Where(d => !string.IsNullOrWhiteSpace(d.CodigoProductoDetalleFactura) && d.CantidadDetalleFactura > 0)
                    .Select(d => new ApiCrearFacturaDetalleRequest
                    {
                        CodigoProducto = d.CodigoProductoDetalleFactura,
                        Cantidad = d.CantidadDetalleFactura
                    })
                    .ToList()
            };

            var facturaCreada = await PostApiObjectAsync<ApiCrearFacturaRequest, RawFacturaDto>(
                "facturas",
                request,
                "factura"
            );

            return facturaCreada != null && facturaCreada.IdFactura > 0;
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

            return await PutApiAsync($"facturas/{id}", new
            {
                id_cliente_factura = factura.IdClienteFactura,
                dpi_usuario_factura = factura.DpiUsuarioFactura,
                fecha_emision_factura = factura.FechaEmisionFactura.ToString("yyyy-MM-dd"),
                moneda_factura = factura.MonedaFactura
            });
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

            return await DeleteApiAsync($"facturas/{id}");
        }

        public async Task<List<DetalleFacturaDto>> ObtenerDetallesFacturaAsync()
        {
            if (UsarMocks())
                return new List<DetalleFacturaDto>();

            var raw = await GetApiListAsync<RawDetalleFacturaDto>("detalles-factura");

            return raw?.Select(MapDetalle).ToList() ?? new List<DetalleFacturaDto>();
        }

        public async Task<List<DetalleFacturaDto>> ObtenerDetallesPorFacturaAsync(int idFactura)
        {
            if (UsarMocks())
            {
                var detalle = await LeerDetalleFacturaMockAsync(idFactura);
                return detalle.Detalles;
            }

            var raw = await GetApiListAsync<RawDetalleFacturaDto>($"detalles-factura/factura/{idFactura}");

            return raw?.Select(MapDetalle).ToList() ?? new List<DetalleFacturaDto>();
        }

        public async Task<DetalleFacturaDto?> ObtenerDetalleFacturaLineaPorIdAsync(int id)
        {
            var raw = await GetApiObjectAsync<RawDetalleFacturaDto>($"detalles-factura/{id}");
            return raw == null ? null : MapDetalle(raw);
        }

        public async Task<bool> CrearDetalleFacturaAsync(DetalleFacturaDto detalle)
        {
            return await PostApiAsync("detalles-factura", new
            {
                id_factura_detalle_factura = detalle.IdFacturaDetalleFactura,
                codigo_producto_detalle_factura = detalle.CodigoProductoDetalleFactura,
                cantidad_detalle_factura = detalle.CantidadDetalleFactura,
                precio_unitario_detalle_factura = detalle.PrecioUnitarioDetalleFactura
            });
        }

        public async Task<bool> ActualizarDetalleFacturaAsync(int id, DetalleFacturaDto detalle)
        {
            return await PutApiAsync($"detalles-factura/{id}", new
            {
                id_factura_detalle_factura = detalle.IdFacturaDetalleFactura,
                codigo_producto_detalle_factura = detalle.CodigoProductoDetalleFactura,
                cantidad_detalle_factura = detalle.CantidadDetalleFactura,
                precio_unitario_detalle_factura = detalle.PrecioUnitarioDetalleFactura
            });
        }

        public async Task<bool> EliminarDetalleFacturaAsync(int id)
        {
            return await DeleteApiAsync($"detalles-factura/{id}");
        }

        public async Task<List<PagoDto>> ObtenerPagosAsync()
        {
            if (UsarMocks())
                return await LeerListaMockAsync<PagoDto>("pagos.json");

            var raw = await GetApiListAsync<RawPagoDto>("pagos");

            if (raw == null)
                return UsarFallbackSiApiFalla() ? await LeerListaMockAsync<PagoDto>("pagos.json") : new List<PagoDto>();

            var pagos = raw.Select(MapPago)
               .OrderBy(p => p.IdPago)
               .ToList();
            var facturas = await ObtenerFacturasAsync();

            foreach (var pago in pagos)
            {
                pago.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";
            }

            return pagos;
        }

        public async Task<List<PagoDto>> ObtenerPagosPorFacturaAsync(int idFactura)
        {
            if (UsarMocks())
            {
                var pagosMock = await ObtenerPagosAsync();
                return pagosMock.Where(p => p.IdFacturaPago == idFactura).ToList();
            }

            var raw = await GetApiListAsync<RawPagoDto>($"pagos/factura/{idFactura}");
            var pagosApi = raw?.Select(MapPago).ToList() ?? new List<PagoDto>();

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

            var raw = await GetApiObjectAsync<RawPagoDto>($"pagos/{id}");

            if (raw == null)
            {
                if (!UsarFallbackSiApiFalla())
                    return null;

                var pagosMock = await LeerListaMockAsync<PagoDto>("pagos.json");
                return pagosMock.FirstOrDefault(p => p.IdPago == id);
            }

            var pago = MapPago(raw);
            var facturas = await ObtenerFacturasAsync();
            pago.NumeroFactura = facturas.FirstOrDefault(f => f.IdFactura == pago.IdFacturaPago)?.NumeroFactura ?? "";

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

            return await PostApiAsync("pagos", new
            {
                id_factura_pago = pago.IdFacturaPago,
                fecha_pago = pago.FechaPago.ToString("yyyy-MM-dd"),
                monto_pago = pago.MontoPago,
                metodo_pago = pago.MetodoPago,
                numero_referencia_pago = pago.NumeroReferenciaPago
            });
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

            return await PutApiAsync($"pagos/{id}", new
            {
                id_factura_pago = pago.IdFacturaPago,
                fecha_pago = pago.FechaPago.ToString("yyyy-MM-dd"),
                monto_pago = pago.MontoPago,
                metodo_pago = pago.MetodoPago,
                numero_referencia_pago = pago.NumeroReferenciaPago
            });
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

            return await DeleteApiAsync($"pagos/{id}");
        }

        private static CategoriaDto MapCategoria(RawCategoriaDto raw)
        {
            return new CategoriaDto
            {
                IdCategoriaProducto = raw.IdCategoriaProducto,
                NombreCategoriaProducto = raw.NombreCategoriaProducto ?? "",
                DescripcionCategoriaProducto = raw.DescripcionCategoriaProducto ?? ""
            };
        }

        private static ProductoDto MapProducto(RawProductoDto raw)
        {
            return new ProductoDto
            {
                IdProducto = raw.IdProducto,
                CodigoProducto = raw.CodigoProducto ?? "",
                IdCategoriaProducto = raw.IdCategoriaProducto,
                NombreCategoriaProducto = raw.Categoria?.NombreCategoriaProducto ?? raw.NombreCategoriaProducto ?? "",
                NombreProducto = raw.NombreProducto ?? "",
                DescripcionProducto = raw.DescripcionProducto ?? "",
                PrecioUnitarioProducto = raw.PrecioUnitarioProducto
            };
        }

        private static ClienteDto MapCliente(RawClienteDto raw)
        {
            return new ClienteDto
            {
                IdCliente = raw.IdCliente,
                DpiCliente = raw.DpiCliente ?? "",
                NitCliente = raw.NitCliente ?? "",
                NombreCliente = raw.NombreCliente ?? "",
                ApellidoCliente = raw.ApellidoCliente ?? "",
                CorreoCliente = raw.CorreoCliente ?? "",
                TelefonoCliente = raw.TelefonoCliente ?? ""
            };
        }

        private static RolDto MapRol(RawRolDto raw)
        {
            return new RolDto
            {
                IdRol = raw.IdRol,
                NombreRol = raw.NombreRol ?? ""
            };
        }

        private static UsuarioDto MapUsuario(RawUsuarioDto raw)
        {
            return new UsuarioDto
            {
                DpiUsuario = raw.DpiUsuario ?? "",
                NombreUsuario = raw.NombreUsuario ?? "",
                ApellidoUsuario = raw.ApellidoUsuario ?? "",
                CorreoUsuario = raw.CorreoUsuario ?? "",
                PasswordUsuario = raw.PasswordUsuario ?? "",
                IdRol = raw.IdRol,
                NombreRol = raw.Rol?.NombreRol ?? raw.NombreRol ?? ""
            };
        }

        private static FacturaDto MapFactura(RawFacturaDto raw)
        {
            return new FacturaDto
            {
                IdFactura = raw.IdFactura,
                IdClienteFactura = raw.IdClienteFactura,
                DpiCliente = raw.Cliente?.DpiCliente ?? raw.DpiCliente ?? "",
                NitCliente = raw.Cliente?.NitCliente ?? raw.NitCliente ?? "",
                NombreCliente = raw.Cliente?.NombreCliente ?? raw.NombreCliente ?? "",
                ApellidoCliente = raw.Cliente?.ApellidoCliente ?? raw.ApellidoCliente ?? "",
                DpiUsuarioFactura = raw.DpiUsuarioFactura ?? "",
                NombreUsuario = raw.Usuario?.NombreUsuario ?? raw.NombreUsuario ?? "",
                ApellidoUsuario = raw.Usuario?.ApellidoUsuario ?? raw.ApellidoUsuario ?? "",
                NumeroFactura = raw.NumeroFactura ?? "",
                FechaEmisionFactura = raw.FechaEmisionFactura,
                SubtotalFactura = raw.SubtotalFactura,
                TotalFactura = raw.TotalFactura,
                MonedaFactura = string.IsNullOrWhiteSpace(raw.MonedaFactura) ? "GTQ" : raw.MonedaFactura
            };
        }

        private static DetalleFacturaDto MapDetalle(RawDetalleFacturaDto raw)
        {
            return new DetalleFacturaDto
            {
                IdDetalleFactura = raw.IdDetalleFactura,
                IdFacturaDetalleFactura = raw.IdFacturaDetalleFactura,
                CodigoProductoDetalleFactura = raw.CodigoProductoDetalleFactura ?? "",
                NombreProducto = raw.Producto?.NombreProducto ?? raw.NombreProducto ?? "",
                CantidadDetalleFactura = raw.CantidadDetalleFactura,
                PrecioUnitarioDetalleFactura = raw.PrecioUnitarioDetalleFactura
            };
        }

        private static PagoDto MapPago(RawPagoDto raw)
        {
            return new PagoDto
            {
                IdPago = raw.IdPago,
                IdFacturaPago = raw.IdFacturaPago,
                NumeroFactura = raw.NumeroFactura ?? "",
                FechaPago = raw.FechaPago,
                MontoPago = raw.MontoPago,
                MetodoPago = raw.MetodoPago ?? "",
                NumeroReferenciaPago = raw.NumeroReferenciaPago ?? ""
            };
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
        public FuenteDatosDto FuenteDatos { get; set; } = new();
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

    internal class ApiHealthDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = "";

        [JsonPropertyName("uptime")]
        public decimal Uptime { get; set; }
    }

    internal class RawCategoriaDto
    {
        [JsonPropertyName("id_categoria_producto")]
        public int IdCategoriaProducto { get; set; }

        [JsonPropertyName("nombre_categoria_producto")]
        public string? NombreCategoriaProducto { get; set; }

        [JsonPropertyName("descripcion_categoria_producto")]
        public string? DescripcionCategoriaProducto { get; set; }
    }

    internal class RawProductoDto
    {
        [JsonPropertyName("id_producto")]
        public int IdProducto { get; set; }

        [JsonPropertyName("codigo_producto")]
        public string? CodigoProducto { get; set; }

        [JsonPropertyName("id_categoria_producto")]
        public int IdCategoriaProducto { get; set; }

        [JsonPropertyName("nombre_categoria_producto")]
        public string? NombreCategoriaProducto { get; set; }

        [JsonPropertyName("nombre_producto")]
        public string? NombreProducto { get; set; }

        [JsonPropertyName("descripcion_producto")]
        public string? DescripcionProducto { get; set; }

        [JsonPropertyName("precio_unitario_producto")]
        public decimal PrecioUnitarioProducto { get; set; }

        [JsonPropertyName("categoria")]
        public RawCategoriaDto? Categoria { get; set; }
    }

    internal class RawClienteDto
    {
        [JsonPropertyName("id_cliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("dpi_cliente")]
        public string? DpiCliente { get; set; }

        [JsonPropertyName("nit_cliente")]
        public string? NitCliente { get; set; }

        [JsonPropertyName("nombre_cliente")]
        public string? NombreCliente { get; set; }

        [JsonPropertyName("apellido_cliente")]
        public string? ApellidoCliente { get; set; }

        [JsonPropertyName("correo_cliente")]
        public string? CorreoCliente { get; set; }

        [JsonPropertyName("telefono_cliente")]
        public string? TelefonoCliente { get; set; }
    }

    internal class RawRolDto
    {
        [JsonPropertyName("id_rol")]
        public int IdRol { get; set; }

        [JsonPropertyName("nombre_rol")]
        public string? NombreRol { get; set; }
    }

    internal class RawUsuarioDto
    {
        [JsonPropertyName("dpi_usuario")]
        public string? DpiUsuario { get; set; }

        [JsonPropertyName("nombre_usuario")]
        public string? NombreUsuario { get; set; }

        [JsonPropertyName("apellido_usuario")]
        public string? ApellidoUsuario { get; set; }

        [JsonPropertyName("correo_usuario")]
        public string? CorreoUsuario { get; set; }

        [JsonPropertyName("password_usuario")]
        public string? PasswordUsuario { get; set; }

        [JsonPropertyName("id_rol")]
        public int IdRol { get; set; }

        [JsonPropertyName("nombre_rol")]
        public string? NombreRol { get; set; }

        [JsonPropertyName("rol")]
        public RawRolDto? Rol { get; set; }
    }

    internal class RawFacturaDto
    {
        [JsonPropertyName("id_factura")]
        public int IdFactura { get; set; }

        [JsonPropertyName("id_cliente_factura")]
        public int IdClienteFactura { get; set; }

        [JsonPropertyName("dpi_cliente")]
        public string? DpiCliente { get; set; }

        [JsonPropertyName("nit_cliente")]
        public string? NitCliente { get; set; }

        [JsonPropertyName("nombre_cliente")]
        public string? NombreCliente { get; set; }

        [JsonPropertyName("apellido_cliente")]
        public string? ApellidoCliente { get; set; }

        [JsonPropertyName("dpi_usuario_factura")]
        public string? DpiUsuarioFactura { get; set; }

        [JsonPropertyName("nombre_usuario")]
        public string? NombreUsuario { get; set; }

        [JsonPropertyName("apellido_usuario")]
        public string? ApellidoUsuario { get; set; }

        [JsonPropertyName("numero_factura")]
        public string? NumeroFactura { get; set; }

        [JsonPropertyName("fecha_emision_factura")]
        public DateTime FechaEmisionFactura { get; set; }

        [JsonPropertyName("subtotal_factura")]
        public decimal SubtotalFactura { get; set; }

        [JsonPropertyName("total_factura")]
        public decimal TotalFactura { get; set; }

        [JsonPropertyName("moneda_factura")]
        public string? MonedaFactura { get; set; }

        [JsonPropertyName("cliente")]
        public RawClienteDto? Cliente { get; set; }

        [JsonPropertyName("usuario")]
        public RawUsuarioDto? Usuario { get; set; }

        [JsonPropertyName("detalles")]
        public List<RawDetalleFacturaDto>? Detalles { get; set; }
    }

    internal class RawDetalleFacturaDto
    {
        [JsonPropertyName("id_detalle_factura")]
        public int IdDetalleFactura { get; set; }

        [JsonPropertyName("id_factura_detalle_factura")]
        public int IdFacturaDetalleFactura { get; set; }

        [JsonPropertyName("codigo_producto_detalle_factura")]
        public string? CodigoProductoDetalleFactura { get; set; }

        [JsonPropertyName("nombre_producto")]
        public string? NombreProducto { get; set; }

        [JsonPropertyName("cantidad_detalle_factura")]
        public decimal CantidadDetalleFactura { get; set; }

        [JsonPropertyName("precio_unitario_detalle_factura")]
        public decimal PrecioUnitarioDetalleFactura { get; set; }

        [JsonPropertyName("producto")]
        public RawProductoDto? Producto { get; set; }
    }

    internal class RawPagoDto
    {
        [JsonPropertyName("id_pago")]
        public int IdPago { get; set; }

        [JsonPropertyName("id_factura_pago")]
        public int IdFacturaPago { get; set; }

        [JsonPropertyName("numero_factura")]
        public string? NumeroFactura { get; set; }

        [JsonPropertyName("fecha_pago")]
        public DateTime FechaPago { get; set; }

        [JsonPropertyName("monto_pago")]
        public decimal MontoPago { get; set; }

        [JsonPropertyName("metodo_pago")]
        public string? MetodoPago { get; set; }

        [JsonPropertyName("numero_referencia_pago")]
        public string? NumeroReferenciaPago { get; set; }
    }

    internal class ApiCrearFacturaRequest
    {
        [JsonPropertyName("id_cliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("dpi_usuario")]
        public string DpiUsuario { get; set; } = "";

        [JsonPropertyName("detalles")]
        public List<ApiCrearFacturaDetalleRequest> Detalles { get; set; } = new();
    }

    internal class ApiCrearFacturaDetalleRequest
    {
        [JsonPropertyName("codigo_producto")]
        public string CodigoProducto { get; set; } = "";

        [JsonPropertyName("cantidad")]
        public decimal Cantidad { get; set; }
    }
}