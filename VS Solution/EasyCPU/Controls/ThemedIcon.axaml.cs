using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace EasyCPU.Controls;

/// <summary>
/// Icona monocromatica (PNG con canale alpha) colorata dinamicamente in base
/// al tema attivo (Chiaro/Scuro/Blue), tramite OpacityMask su un Border il cui
/// Background segue la risorsa di tema "SystemControlForegroundBaseHighBrush".
/// </summary>
public partial class ThemedIcon : UserControl
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ThemedIcon, IImage?>(nameof(Source));

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ThemedIcon()
    {
        InitializeComponent();
    }
}
