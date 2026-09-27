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
        DataContextChanged += (_, _) => { if (DataContext is MainViewModel vm) Scorciatoie.Collega(this, vm); };
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
        // Lo stesso vale per le voci che aprono una sezione del menu (Recenti).
        if (e.Source is Visual sv && sv.FindAncestorOfType<Button>(includeSelf: true) is { } clicked
            && (clicked.Classes.Contains("hamburger-button") || clicked.Classes.Contains("drawer-toggle")))
            return;

        if (e.Source is Visual v && v.FindAncestorOfType<SplitView>() is { } drawer)
            drawer.IsPaneOpen = false;
    }

    private void OnRecentiClick(object? sender, RoutedEventArgs e) =>
        MostraRecenti(!this.FindControl<ItemsControl>("RecentiElenco")!.IsVisible);

    // Alla chiusura del menu l'elenco dei recenti torna chiuso, come un sottomenu.
    private void OnDrawerClosed(object? sender, RoutedEventArgs e) => MostraRecenti(false);

    private void MostraRecenti(bool visibile)
    {
        this.FindControl<ItemsControl>("RecentiElenco")!.IsVisible = visibile;
        this.FindControl<Avalonia.Controls.Shapes.Path>("RecentiFreccia")!.RenderTransform =
            new Avalonia.Media.RotateTransform(visibile ? 90 : 0);
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
