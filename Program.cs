using ElectronNET;
using ElectronNET.API;
using System.Text.Json;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;
using SignageApp.Services;
using Microsoft.Extensions.FileProviders;

var webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var developmentRootFile = Path.Combine(AppContext.BaseDirectory, "SignageAdmin.Web.development-root");
var useProjectData = File.Exists(developmentRootFile);

if (useProjectData)
{
    var projectDirectory = File.ReadAllText(developmentRootFile).Trim();

    if (!Path.IsPathFullyQualified(projectDirectory) || !Directory.Exists(projectDirectory))
    {
        throw new InvalidOperationException(
            $"De development-projectmap in '{developmentRootFile}' bestaat niet of is geen absoluut pad. Bouw het project opnieuw.");
    }

    webRootPath = Path.Combine(projectDirectory, "wwwroot");
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = webRootPath
});

builder.Services.AddRazorPages();
// This app is accessed directly, not through a trusted reverse proxy.
// Also neutralize automatic forwarding enabled through environment configuration.
builder.Services.PostConfigure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.None);
var legacyWebRoot = useProjectData ? webRootPath : builder.Configuration["Storage:LegacyWebRoot"] ?? webRootPath;
var storagePaths = StoragePaths.Create(legacyWebRoot, useProjectData,
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    builder.Configuration["Storage:DataRoot"]);
builder.Services.AddSingleton(storagePaths);
builder.Services.AddSingleton<SlideService>();
builder.Services.AddElectron();
builder.Services.AddSingleton<AdminSessions>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<AdminSessions>());

builder.WebHost.UseElectron(args);

// One fixed port for the Electron host, browser and LAN players.
using var manifest = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, "electron.manifest.json")));
var backendPort = manifest.RootElement.GetProperty("aspCoreBackendPort").GetInt32();
if (backendPort is < 1 or > 65535)
    throw new InvalidOperationException("aspCoreBackendPort moet tussen 1 en 65535 liggen.");
var backendUrl = $"http://localhost:{backendPort}";
builder.WebHost.UseUrls(Socket.OSSupportsIPv6
    ? new[] { $"http://0.0.0.0:{backendPort}", $"http://[::1]:{backendPort}" }
    : new[] { $"http://0.0.0.0:{backendPort}" });

var app = builder.Build();
// Initialize or validate storage before the player can request its JSON directly.
_ = app.Services.GetRequiredService<SlideService>();

// Guard all routes, including static files, before any request can be handled.
app.UseMiddleware<LocalAdminAccess>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

var storageFileProvider = new PhysicalFileProvider(storagePaths.RootPath);
app.Lifetime.ApplicationStopped.Register(storageFileProvider.Dispose);
app.MapWhen(context => context.Request.Path.StartsWithSegments("/data")
    || context.Request.Path.StartsWithSegments("/uploads"), storageApp =>
{
    storageApp.UseStaticFiles(new StaticFileOptions { FileProvider = storageFileProvider });
    storageApp.Run(context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    });
});

app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();
app.MapRazorPages()
    .WithStaticAssets();

app.MapGet("/api/admin/status", (AdminSessions sessions, HttpResponse response) =>
{
    response.Headers.CacheControl = "no-store";
    return Results.Json(new { isActive = sessions.IsActive });
});

app.MapPost("/api/admin/ping", (Guid sessionId, AdminSessions sessions) =>
{
    if (sessionId == Guid.Empty) return Results.BadRequest("Een geldige sessionId is verplicht.");
    sessions.Ping(sessionId);
    return Results.Ok();
});

app.MapPost("/api/admin/clear", (Guid sessionId, AdminSessions sessions) =>
{
    if (sessionId == Guid.Empty) return Results.BadRequest("Een geldige sessionId is verplicht.");
    sessions.Clear(sessionId);
    return Results.Ok();
});

try
{
    await app.StartAsync();
}
catch (IOException exception)
{
    var message = $"Signage Beheer kan {backendUrl} niet starten. Poort {backendPort} is mogelijk al in gebruik. "
        + "Sluit de andere backend of het andere programma en probeer opnieuw. Er wordt geen andere poort gekozen.";
    app.Logger.LogCritical(exception, "{Message}", message);
    Environment.ExitCode = 1;
    if (HybridSupport.IsElectronActive)
    {
        await DesktopHost.ShowStartupErrorAsync(message);
        await app.WaitForShutdownAsync();
    }
    return;
}

if (HybridSupport.IsElectronActive)
{
    try
    {
        await DesktopHost.StartAsync(backendUrl);
    }
    catch (Exception exception)
    {
        app.Logger.LogCritical(exception, "Electron-beheer kon niet starten.");
        await DesktopHost.ShowStartupErrorAsync("Het beheervenster kon niet starten: " + exception.Message);
    }
}
await app.WaitForShutdownAsync();
