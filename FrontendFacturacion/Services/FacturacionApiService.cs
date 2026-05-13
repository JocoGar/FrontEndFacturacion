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
                PropertyNameCaseInsensitive = true
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

        private string ObtenerRutaMock(string archivo)
        {
            return Path.Combine(_environment.WebRootPath, "mocks", archivo);
        }

        private async Task<List<T>> LeerListaMockAsync<T>(string archivo)
        {
            var ruta = ObtenerRutaMock(archivo);

            if (!File.Exists(ruta))
                return new List<T>();

            var json = await File.ReadAllTextAsync(ruta);
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }

        private async Task<T> LeerObjetoMockAsync<T>(string archivo, T valorDefault)
        {
            var ruta = ObtenerRutaMock(archivo);

            if (!File.Exists(ruta))
                return valorDefault;

            var json = await File.ReadAllTextAsync(ruta);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? valorDefault;
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

        public async Task<bool> ApiDisponibleAsync()
        {
            if (UsarMocks())
                return false;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ObtenerTimeout()));
                var response = await _httpClient.GetAsync("/api/health", cts.Token);
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

        public async Task<List<ProductoDto>> ObtenerProductosAsync()
        {
            return await GetListAsync<ProductoDto>("/api/productos", "productos.json");
        }

        public async Task<List<ClienteDto>> ObtenerClientesAsync()
        {
            return await GetListAsync<ClienteDto>("/api/clientes", "clientes.json");
        }

        public async Task<List<RolDto>> ObtenerRolesAsync()
        {
            return await GetListAsync<RolDto>("/api/roles", "roles.json");
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosAsync()
        {
            return await GetListAsync<UsuarioDto>("/api/usuarios", "usuarios.json");
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync()
        {
            return await GetListAsync<FacturaDto>("/api/facturas", "facturas.json");
        }

        public async Task<FacturaDetalleViewModel> ObtenerDetalleFacturaAsync(int id)
        {
            var archivoMock = $"factura-detalle-{id}.json";

            return await GetObjectAsync(
                $"/api/facturas/{id}",
                archivoMock,
                new FacturaDetalleViewModel()
            );
        }

        public async Task<List<PagoDto>> ObtenerPagosAsync()
        {
            return await GetListAsync<PagoDto>("/api/pagos", "pagos.json");
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

        public string Categoria => NombreCategoriaProducto;
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
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = "";
        public DateTime FechaCreacionUsuario { get; set; }
        public DateTime? FechaActualizacionUsuario { get; set; }

        public string Rol => NombreRol;
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

        public string CodigoProducto => CodigoProductoDetalleFactura;
        public int Cantidad => CantidadDetalleFactura;
        public decimal PrecioUnitario => PrecioUnitarioDetalleFactura;
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
}