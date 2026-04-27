using ElectronNET;
using ElectronNET.API;
using ElectronNET.API.Entities;
using SignageApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<SlideService>();
builder.Services.AddElectron();

builder.UseElectron(args, async () =>
{
    var options = new BrowserWindowOptions
    {
        Show = false,
        Width = 1600,
        Height = 1000
    };

    if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
    {
        options.AutoHideMenuBar = true;
    }

    var browserWindow = await Electron.WindowManager.CreateWindowAsync(options);
    browserWindow.OnReadyToShow += () => browserWindow.Show();
    browserWindow.OnClosed += () => Electron.App.Quit();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();