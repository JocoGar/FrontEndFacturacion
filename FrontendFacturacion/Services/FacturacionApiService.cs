using System.Text.Json;

namespace FrontendFacturacion.Services
{
    public class FacturacionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;

        public FacturacionApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private bool UsarDatosDemo()
        {
            return _configuration.GetValue<bool>("ApiSettings:UseDemoData");
        }

        private async Task<List<T>> GetListOrDemoAsync<T>(string endpoint, List<T> demoData)
        {
            if (UsarDatosDemo())
                return demoData;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(
                    _configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 1)
                ));

                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                if (!response.IsSuccessStatusCode)
                    return demoData;

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                var data = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);

                return data ?? demoData;
            }
            catch
            {
                return demoData;
            }
        }

        private async Task<T> GetOneOrDemoAsync<T>(string endpoint, T demoData)
        {
            if (UsarDatosDemo())
                return demoData;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(
                    _configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 1)
                ));

                var response = await _httpClient.GetAsync(endpoint, cts.Token);

                if (!response.IsSuccessStatusCode)
                    return demoData;

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                var data = JsonSerializer.Deserialize<T>(json, _jsonOptions);

                return data ?? demoData;
            }
            catch
            {
                return demoData;
            }
        }

        public async Task<bool> ApiDisponibleAsync()
        {
            if (UsarDatosDemo())
                return false;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var response = await _httpClient.GetAsync("/api/health", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ProductoDto>> ObtenerProductosAsync()
        {
            return await GetListOrDemoAsync("/api/productos", DatosDemo.Productos);
        }

        public async Task<List<CategoriaDto>> ObtenerCategoriasAsync()
        {
            return await GetListOrDemoAsync("/api/categorias", DatosDemo.Categorias);
        }

        public async Task<List<ClienteDto>> ObtenerClientesAsync()
        {
            return await GetListOrDemoAsync("/api/clientes", DatosDemo.Clientes);
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync()
        {
            return await GetListOrDemoAsync("/api/facturas", DatosDemo.Facturas);
        }

        public async Task<FacturaDetalleViewModel> ObtenerDetalleFacturaAsync(int id)
        {
            return await GetOneOrDemoAsync($"/api/facturas/{id}", DatosDemo.DetalleFactura);
        }

        public async Task<List<PagoDto>> ObtenerPagosAsync()
        {
            return await GetListOrDemoAsync("/api/pagos", DatosDemo.Pagos);
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosAsync()
        {
            return await GetListOrDemoAsync("/api/usuarios", DatosDemo.Usuarios);
        }

        public async Task<List<RolDto>> ObtenerRolesAsync()
        {
            return await GetListOrDemoAsync("/api/roles", DatosDemo.Roles);
        }
    }

    public class ProductoDto
    {
        public string CodigoProducto { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string DescripcionProducto { get; set; } = "";
        public decimal PrecioUnitarioProducto { get; set; }
        public decimal PrecioCostoProducto { get; set; }
    }

    public class CategoriaDto
    {
        public int IdCategoriaProducto { get; set; }
        public string NombreCategoriaProducto { get; set; } = "";
        public string DescripcionCategoriaProducto { get; set; } = "";
        public DateTime FechaCreacionCategoriaProducto { get; set; }
    }

    public class ClienteDto
    {
        public string DpiCliente { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string ApellidoCliente { get; set; } = "";
        public string CorreoCliente { get; set; } = "";
        public string TelefonoCliente { get; set; } = "";
    }

    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public string NumeroFactura { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Usuario { get; set; } = "";
        public DateTime FechaEmisionFactura { get; set; }
        public string EstadoFactura { get; set; } = "";
        public decimal SubtotalFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public string MonedaFactura { get; set; } = "GTQ";
    }

    public class DetalleFacturaDto
    {
        public string CodigoProducto { get; set; } = "";
        public string NombreProducto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }

    public class PagoDto
    {
        public int IdPago { get; set; }
        public string NumeroFactura { get; set; } = "";
        public DateTime FechaPago { get; set; }
        public decimal MontoPago { get; set; }
        public string MetodoPago { get; set; } = "";
        public string NumeroReferenciaPago { get; set; } = "";
        public string EstadoPago { get; set; } = "";
    }

    public class UsuarioDto
    {
        public string DpiUsuario { get; set; } = "";
        public string NombreUsuario { get; set; } = "";
        public string ApellidoUsuario { get; set; } = "";
        public string CorreoUsuario { get; set; } = "";
        public string Rol { get; set; } = "";
    }

    public class RolDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = "";
        public string Descripcion { get; set; } = "";
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

    public static class DatosDemo
    {
        public static List<CategoriaDto> Categorias = new()
        {
            new CategoriaDto { IdCategoriaProducto = 1, NombreCategoriaProducto = "Hardware", DescripcionCategoriaProducto = "Componentes físicos y equipos electrónicos.", FechaCreacionCategoriaProducto = DateTime.Now.AddDays(-10) },
            new CategoriaDto { IdCategoriaProducto = 2, NombreCategoriaProducto = "Software", DescripcionCategoriaProducto = "Licencias, programas y suscripciones.", FechaCreacionCategoriaProducto = DateTime.Now.AddDays(-8) },
            new CategoriaDto { IdCategoriaProducto = 3, NombreCategoriaProducto = "Servicios IT", DescripcionCategoriaProducto = "Soporte técnico, mantenimiento y consultoría.", FechaCreacionCategoriaProducto = DateTime.Now.AddDays(-5) }
        };

        public static List<ProductoDto> Productos = new()
{
    new ProductoDto
    {
        CodigoProducto = "PRD-001",
        NombreProducto = "Servicio de soporte técnico",
        Categoria = "Servicios IT",
        DescripcionProducto = "Atención técnica y mantenimiento preventivo.",
        PrecioUnitarioProducto = 150,
        PrecioCostoProducto = 80
    },
    new ProductoDto
    {
        CodigoProducto = "PRD-002",
        NombreProducto = "Licencia de software",
        Categoria = "Software",
        DescripcionProducto = "Licencia anual de sistema administrativo.",
        PrecioUnitarioProducto = 300,
        PrecioCostoProducto = 180
    },
    new ProductoDto
    {
        CodigoProducto = "PRD-003",
        NombreProducto = "Implementación de sistema",
        Categoria = "Servicios IT",
        DescripcionProducto = "Configuración inicial y capacitación básica.",
        PrecioUnitarioProducto = 500,
        PrecioCostoProducto = 250
    }
};

        public static List<ClienteDto> Clientes = new()
        {
            new ClienteDto { DpiCliente = "2548963210101", NombreCliente = "Juan Carlos", ApellidoCliente = "Pérez Gómez", CorreoCliente = "jperez@empresa.com", TelefonoCliente = "5555-1234" },
            new ClienteDto { DpiCliente = "1874521470101", NombreCliente = "María Fernanda", ApellidoCliente = "López Ruiz", CorreoCliente = "mlopez@empresa.com", TelefonoCliente = "4444-9876" },
            new ClienteDto { DpiCliente = "3210654870301", NombreCliente = "Roberto Antonio", ApellidoCliente = "García Méndez", CorreoCliente = "roberto@empresa.com", TelefonoCliente = "3333-5678" }
        };

        public static List<FacturaDto> Facturas = new()
        {
            new FacturaDto { IdFactura = 1, NumeroFactura = "F-001", Cliente = "Juan Pérez", Usuario = "Admin", FechaEmisionFactura = DateTime.Now, EstadoFactura = "PAGADA", SubtotalFactura = 150, TotalFactura = 150 },
            new FacturaDto { IdFactura = 2, NumeroFactura = "F-002", Cliente = "Empresa ABC", Usuario = "Admin", FechaEmisionFactura = DateTime.Now, EstadoFactura = "PENDIENTE", SubtotalFactura = 1250, TotalFactura = 1250 },
            new FacturaDto { IdFactura = 3, NumeroFactura = "F-003", Cliente = "Carlos López", Usuario = "Admin", FechaEmisionFactura = DateTime.Now.AddDays(-1), EstadoFactura = "VENCIDA", SubtotalFactura = 300, TotalFactura = 300 }
        };

        public static List<PagoDto> Pagos = new()
        {
            new PagoDto { IdPago = 1, NumeroFactura = "F-001", FechaPago = DateTime.Now, MontoPago = 150, MetodoPago = "EFECTIVO", NumeroReferenciaPago = "CAJA-01", EstadoPago = "REGISTRADO" },
            new PagoDto { IdPago = 2, NumeroFactura = "F-002", FechaPago = DateTime.Now, MontoPago = 1250, MetodoPago = "TRANSFERENCIA", NumeroReferenciaPago = "TRX-001", EstadoPago = "PENDIENTE" }
        };

        public static List<UsuarioDto> Usuarios = new()
        {
            new UsuarioDto { DpiUsuario = "2948302910101", NombreUsuario = "Carlos Arturo", ApellidoUsuario = "Méndez López", CorreoUsuario = "carlos@empresa.com", Rol = "Administrador" },
            new UsuarioDto { DpiUsuario = "1029485760101", NombreUsuario = "María Elena", ApellidoUsuario = "Gómez Ruiz", CorreoUsuario = "maria@empresa.com", Rol = "Vendedor" }
        };

        public static List<RolDto> Roles = new()
        {
            new RolDto { IdRol = 1, NombreRol = "Administrador", Descripcion = "Acceso completo al sistema." },
            new RolDto { IdRol = 2, NombreRol = "Vendedor", Descripcion = "Puede registrar clientes, facturas y pagos." },
            new RolDto { IdRol = 3, NombreRol = "Consulta", Descripcion = "Solo puede visualizar información." }
        };

        public static FacturaDetalleViewModel DetalleFactura = new()
        {
            Factura = new FacturaDto
            {
                IdFactura = 1,
                NumeroFactura = "F-001",
                Cliente = "Juan Pérez",
                Usuario = "Admin",
                FechaEmisionFactura = DateTime.Now,
                EstadoFactura = "PAGADA",
                SubtotalFactura = 150,
                TotalFactura = 150
            },
            Cliente = new ClienteDto
            {
                DpiCliente = "2548963210101",
                NombreCliente = "Juan Carlos",
                ApellidoCliente = "Pérez Gómez",
                CorreoCliente = "jperez@empresa.com",
                TelefonoCliente = "5555-1234"
            },
            Usuario = new UsuarioDto
            {
                DpiUsuario = "2948302910101",
                NombreUsuario = "Admin",
                ApellidoUsuario = "Sistema",
                CorreoUsuario = "admin@empresa.com",
                Rol = "Administrador"
            },
            Detalles = new List<DetalleFacturaDto>
            {
                new DetalleFacturaDto { CodigoProducto = "PRD-001", NombreProducto = "Monitor Dell 27\"", Cantidad = 1, PrecioUnitario = 150 }
            },
            Pagos = new List<PagoDto>
            {
                new PagoDto { IdPago = 1, NumeroFactura = "F-001", FechaPago = DateTime.Now, MontoPago = 150, MetodoPago = "EFECTIVO", NumeroReferenciaPago = "CAJA-01", EstadoPago = "REGISTRADO" }
            }
        };
    }
}