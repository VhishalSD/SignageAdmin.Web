using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SignageAdmin.Desktop;

public partial class MainWindow : Window
{
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

        await Task.Delay(3000);

        Browser.Source = new Uri("http://localhost:5278");
    }

    private void StartWebApp()
    {
        var webProjectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\SignageAdmin.Web.csproj")
        );

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{webProjectPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _webProcess = Process.Start(startInfo);
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