using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace EasyCPU.Views;

public partial class OpzioniWindow : Window
{
    public OpzioniWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnOk(object? sender, RoutedEventArgs e)      => Close(true);
    private void OnAnnulla(object? sender, RoutedEventArgs e) => Close(false);
}
