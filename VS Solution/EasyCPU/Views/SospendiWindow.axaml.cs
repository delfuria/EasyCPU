using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

public partial class SospendiWindow : Window
{
    public SospendiWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnContinua(object? sender, RoutedEventArgs e) => Close(ModoSospendi.Continua);
    private void OnPausa(object? sender, RoutedEventArgs e)    => Close(ModoSospendi.Pausa);
    private void OnArresta(object? sender, RoutedEventArgs e)  => Close(ModoSospendi.Arresta);
}
