using System;
using System.Windows;

namespace SignageAdmin.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Browser.Source = new Uri("http://localhost:5278");
    }
}