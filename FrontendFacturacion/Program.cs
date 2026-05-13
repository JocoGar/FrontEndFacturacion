using FrontendFacturacion.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<FacturacionApiService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
    var timeoutSeconds = builder.Configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 2);

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();