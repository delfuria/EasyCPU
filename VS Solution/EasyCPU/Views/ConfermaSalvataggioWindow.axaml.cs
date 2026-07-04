using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

public partial class ConfermaSalvataggioWindow : Window
{
    public ConfermaSalvataggioWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSalva(object? sender, RoutedEventArgs e)      => Close(RispostaSalvataggio.Salva);
    private void OnNonSalvare(object? sender, RoutedEventArgs e) => Close(RispostaSalvataggio.NonSalvare);
    private void OnAnnulla(object? sender, RoutedEventArgs e)    => Close(RispostaSalvataggio.Annulla);
}
