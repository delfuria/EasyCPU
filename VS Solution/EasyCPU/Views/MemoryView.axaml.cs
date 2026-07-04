using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

public partial class MemoryView : UserControl
{
    public MemoryView()
    {
        InitializeComponent();
        var tb = this.FindControl<TextBlock>("DumpText");
        if (tb is null) return;

        var s = SettingsViewModel.Instance;
        ApplyFontSize(tb, s.FontPanelliSize);
        s.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.FontPanelliSize))
                ApplyFontSize(tb, s.FontPanelliSize);
        };
    }

    private static void ApplyFontSize(TextBlock tb, float size)
    {
        if (size > 0) tb.FontSize = size;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
