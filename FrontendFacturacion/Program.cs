using FrontendFacturacion.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient<FacturacionApiService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://192.168.1.216/api/";

    if (!baseUrl.EndsWith("/"))
        baseUrl += "/";

    client.BaseAddress = new Uri(baseUrl);

    var timeout = configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 5);
    client.Timeout = TimeSpan.FromSeconds(timeout + 2);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuenta}/{action=Login}/{id?}");

app.Run();