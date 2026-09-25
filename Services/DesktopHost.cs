using ElectronNET.API;
using ElectronNET.API.Entities;

namespace SignageApp.Services;

internal static class DesktopHost
{
    private static readonly SemaphoreSlim WindowGate = new(1, 1);
    private static BrowserWindow? window;

    public static async Task StartAsync(string backendUrl)
    {
        Electron.WindowManager.IsQuitOnWindowAllClosed = false;

        async Task OpenWindowAsync()
        {
            await WindowGate.WaitAsync();
            try
            {
                if (window is not null)
                {
                    window.Restore();
                    window.Show();
                    window.Focus();
                    return;
                }

                var createdWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
                {
                    Show = true,
                    Width = 1600,
                    Height = 1000,
                    AutoHideMenuBar = OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
                }, backendUrl);
                window = createdWindow;
                createdWindow.OnClosed += () =>
                {
                    if (ReferenceEquals(window, createdWindow)) window = null;
                };
            }
            finally
            {
                WindowGate.Release();
            }
        }

        // Event handlers must observe asynchronous failures too.
        async void OpenWindow()
        {
            try { await OpenWindowAsync(); }
            catch (Exception exception) { await ShowStartupErrorAsync(exception.Message); }
        }

        if (!await Electron.App.RequestSingleInstanceLockAsync((_, _) => OpenWindow()))
        {
            Electron.App.Quit();
            return;
        }

        await Electron.Tray.Show(Path.Combine(AppContext.BaseDirectory, "Properties", "TrayIcon.png"),
            new[]
            {
                new MenuItem { Label = "Beheer openen", Click = OpenWindow },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Afsluiten (stopt ook de player)", Click = () => Electron.App.Quit() }
            });
        await Electron.Tray.SetToolTip("Signage Beheer — backend actief");
        await OpenWindowAsync();
    }

    public static async Task ShowStartupErrorAsync(string message)
    {
        await Electron.Dialog.ShowMessageBoxAsync(new MessageBoxOptions(message)
        {
            Type = MessageBoxType.error,
            Title = "Signage Beheer — opstartfout",
            Buttons = new[] { "Afsluiten" }
        });
        Electron.App.Quit();
    }
}
