using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        AddHandler(Button.ClickEvent, OnAnyButtonClicked, RoutingStrategies.Bubble);
    }

    private void OnHamburgerClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Visual v && v.FindAncestorOfType<SplitView>() is { } drawer)
            drawer.IsPaneOpen = !drawer.IsPaneOpen;
    }

    private void OnAnyButtonClicked(object? sender, RoutedEventArgs e)
    {
        // Il bottone hamburger gestisce l'apertura/chiusura per conto proprio: se il click
        // arriva da lui non richiudere subito quello che ha appena aperto/chiuso.
        if (e.Source is Visual sv && sv.FindAncestorOfType<Button>(includeSelf: true) is { } clicked
            && clicked.Classes.Contains("hamburger-button"))
            return;

        if (e.Source is Visual v && v.FindAncestorOfType<SplitView>() is { } drawer)
            drawer.IsPaneOpen = false;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        AttachedToVisualTree -= OnAttachedToVisualTree;

        if (!SettingsViewModel.Instance.PienoSchermo) return;

        // Applicato al frame successivo: su iOS il native ViewController deve
        // aver completato l'apparizione prima che l'aggiornamento della status bar abbia effetto.
        Dispatcher.UIThread.Post(ApplyFullScreen, DispatcherPriority.Background);
    }

    private void ApplyFullScreen()
    {
        var insets = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (insets is null) return;
        insets.IsSystemBarVisible = false;
        insets.DisplayEdgeToEdgePreference = true;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
