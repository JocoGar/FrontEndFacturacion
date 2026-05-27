using FrontendFacturacion.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Configuración de Data Protection para persistir las claves en el sistema de archivos
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/opt/facturacion/keys"))
    .SetApplicationName("FacturadorApp");

// Configuración de autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
// Configuración del HttpClient para FacturacionApiService
builder.Services.AddHttpClient<FacturacionApiService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiSettings:BaseUrl"] ?? throw new ArgumentNullException("BaseUrl no configurada");

    // Asegurarse de que la URL base termine con una barra para evitar problemas al concatenar rutas
    client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(config.GetValue<int>("ApiSettings:TimeoutSeconds", 5));
});

var app = builder.Build();

// Configuración para Forwarded Headers, importante para que el sistema de autenticación funcione correctamente detrás de un proxy inverso
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuenta}/{action=Login}/{id?}");

app.Run();