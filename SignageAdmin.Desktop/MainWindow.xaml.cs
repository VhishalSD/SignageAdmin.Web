using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace SignageAdmin.Desktop;

public partial class MainWindow : Window
{
    private const string WebAppUrl = "http://localhost:5278";
    private Process? _webProcess;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        StartWebApp();

        var isReady = await WaitForWebAppAsync();

        if (isReady)
        {
            Browser.Source = new Uri(WebAppUrl);
        }
        else
        {
            MessageBox.Show(
                "De beheerapp kon niet worden gestart. Probeer de app opnieuw te openen.",
                "Reliplan Signage",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            Close();
        }
    }

    private void StartWebApp()
    {
        var debugWebAppPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\publish\web\SignageAdmin.Web.exe")
        );

        var publishedWebAppPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\web\SignageAdmin.Web.exe")
        );

        var webAppPath = File.Exists(publishedWebAppPath)
            ? publishedWebAppPath
            : debugWebAppPath;

        var webAppDirectory = Path.GetDirectoryName(webAppPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = webAppPath,
            Arguments = "--urls http://localhost:5278",
            WorkingDirectory = webAppDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _webProcess = Process.Start(startInfo);
    }

    private static async Task<bool> WaitForWebAppAsync()
    {
        using var client = new HttpClient();

        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                var response = await client.GetAsync(WebAppUrl);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // The web app is probably still starting.
            }

            await Task.Delay(500);
        }

        return false;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_webProcess is not null && !_webProcess.HasExited)
        {
            _webProcess.Kill(true);
            _webProcess.Dispose();
        }
    }
}