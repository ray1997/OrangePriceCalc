using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Orange.Helper;

namespace Orange.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void CallInitializer(object? sender, RoutedEventArgs e)
    {
        try
        {
            await HttpRequestor.InitializeAPIServer();
        }
        catch
        {
            // ignored
        }
    }
}