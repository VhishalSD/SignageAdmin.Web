using ElectronNET;
using ElectronNET.API;
using ElectronNET.API.Entities;
using SignageApp.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory()
});

builder.Services.AddRazorPages();
builder.Services.AddSingleton<SlideService>();
builder.Services.AddElectron();

builder.WebHost.UseElectron(args);

var app = builder.Build();

var adminLock = new object();
var adminLastSeen = DateTimeOffset.MinValue;
var adminTimeout = TimeSpan.FromSeconds(20);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();
app.MapRazorPages()
    .WithStaticAssets();

if (HybridSupport.IsElectronActive)
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        _ = Task.Run(async () =>
        {
            var options = new BrowserWindowOptions
            {
                Show = true,
                Width = 1600,
                Height = 1000
            };

            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                options.AutoHideMenuBar = true;
            }

            var browserWindow = await Electron.WindowManager.CreateWindowAsync(options, "http://localhost:8001");
            browserWindow.OnClosed += () => Electron.App.Quit();
        });
    });
}

app.MapGet("/api/admin/status", () =>
{
    lock (adminLock)
    {
        bool isActive = DateTimeOffset.UtcNow - adminLastSeen < adminTimeout;
        return Results.Json(new { isActive });
    }
});

app.MapPost("/api/admin/ping", () =>
{
    lock (adminLock)
    {
        adminLastSeen = DateTimeOffset.UtcNow;
    }

    return Results.Ok();
});

app.MapPost("/api/admin/clear", () =>
{
    lock (adminLock)
    {
        adminLastSeen = DateTimeOffset.MinValue;
    }

    return Results.Ok();
});

app.Run("http://localhost:8001");