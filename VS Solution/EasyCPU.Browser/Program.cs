using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasyCPU;

internal sealed partial class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
        .WithInterFont()
        .AfterSetup(_ => InputElement.KeyDownEvent.AddClassHandler<TopLevel>(TranslateCommandKey, RoutingStrategies.Tunnel))
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();

    // In the browser, shortcuts are bound to Ctrl only and Cmd+letter is typed as plain text on macOS.
    // Re-raise Cmd+<key> on the focused element as Ctrl+<key> so copy/paste/undo/select-all work.
    private static void TranslateCommandKey(TopLevel topLevel, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        if (topLevel.FocusManager?.GetFocusedElement() is not Interactive target) return;

        e.Handled = true;
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = e.Key,
            KeyModifiers = (e.KeyModifiers & ~KeyModifiers.Meta) | KeyModifiers.Control,
            PhysicalKey = e.PhysicalKey,
            KeySymbol = e.KeySymbol,
            KeyDeviceType = e.KeyDeviceType
        });
    }
}
