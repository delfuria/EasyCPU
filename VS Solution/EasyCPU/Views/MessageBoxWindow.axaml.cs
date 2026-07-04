using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace EasyCPU.Views;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow() : this("") { }

    public MessageBoxWindow(string message)
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TextBlock>("MessageText")!.Text = message;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
