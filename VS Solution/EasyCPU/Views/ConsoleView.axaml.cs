using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();

        var tb = this.FindControl<TextBlock>("OutputText");
        if (tb is not null)
        {
            var s = SettingsViewModel.Instance;
            ApplyFontSize(tb, s.FontPanelliSize);
            s.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SettingsViewModel.FontPanelliSize))
                    ApplyFontSize(tb, s.FontPanelliSize);
            };
        }

        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private static void ApplyFontSize(TextBlock tb, float size)
    {
        if (size > 0) tb.FontSize = size;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Caratteri stampabili: risolti da Avalonia tenendo conto di layout di tastiera/maiuscole.
    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (DataContext is not ConsoleViewModel vm || e.Text is null) return;
        foreach (var c in e.Text)
            vm.NotificaCarattere(c);
    }

    // Invio (Enter) e Backspace non generano TextInputEvent in Avalonia: vanno intercettati
    // qui e tradotti nei codici CR (13) e BS (8), attesi dai programmi asm che leggono righe.
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ConsoleViewModel vm) return;
        if (e.Key == Key.Enter)
            vm.NotificaCarattere('\r');
        else if (e.Key == Key.Back)
            vm.NotificaCarattere('\b');
    }
}
