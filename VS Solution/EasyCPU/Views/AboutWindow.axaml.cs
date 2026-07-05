using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace EasyCPU.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
