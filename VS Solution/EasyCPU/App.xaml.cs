using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using EasyCpu.Backend.Local;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using EasyCpu.Common;
using EasyCPU.ViewModels;
using EasyCPU.Views;

namespace EasyCPU;

public class App : Application
{
    private MainViewModel? _mainViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ApplicaColorePrimario();
        RegisterEasyCpuHighlighting();
        Ambiente.Inizializza();
        Storage.LeggiOpzioni();
        Storage.ApriFileRecenti();
        ApplyTheme(SettingsViewModel.Instance.Theme);
        _mainViewModel = new MainViewModel(SettingsViewModel.Instance);
        _mainViewModel.LoadLayout();
        _mainViewModel.RefreshRecentFileItems();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = _mainViewModel };
            desktop.Exit += OnDesktopExit;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = new MainView { DataContext = _mainViewModel };

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        SalvaTutto();
    }

    // Salva layout, breakpoint e file recenti. Sul Desktop all'uscita; nel browser quando
    // la pagina viene nascosta o chiusa (chiamato da EasyCPU.Browser).
    public void SalvaTutto() => _mainViewModel?.SaveAll();

    private void OnAboutClick(object? sender, EventArgs e)
    {
        var owner = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var about = new AboutWindow();
        if (owner is not null)
            about.ShowDialog(owner);
        else
            about.Show();
    }

    // Prefisso dei colori del tema scuro in EasyCPU.xshd: "Dark.X" sostituisce il colore "X"
    public const string PrefissoTemaScuro = "Dark.";

    private static void RegisterEasyCpuHighlighting()
    {
        var chiaro = CaricaXshd();
        var scuro = CaricaXshd();
        if (chiaro is null || scuro is null) return;

        scuro.Name = "EasyCPU-Dark";
        ApplicaTemaScuro(scuro);

        var manager = HighlightingManager.Instance;
        manager.RegisterHighlighting("EasyCPU", [".as", ".asj"], HighlightingLoader.Load(chiaro, manager));
        manager.RegisterHighlighting("EasyCPU-Dark", [], HighlightingLoader.Load(scuro, manager));
    }

    // Copia su ogni colore "X" gli attributi impostati in "Dark.X"; quelli assenti restano i chiari
    public static void ApplicaTemaScuro(XshdSyntaxDefinition definizione)
    {
        var colori = definizione.Elements.OfType<XshdColor>().Where(c => c.Name is not null).ToList();
        var scuri = colori.Where(c => c.Name!.StartsWith(PrefissoTemaScuro))
                          .ToDictionary(c => c.Name![PrefissoTemaScuro.Length..]);
        foreach (var colore in colori)
        {
            if (!scuri.TryGetValue(colore.Name!, out var scuro)) continue;
            colore.Foreground = scuro.Foreground ?? colore.Foreground;
            colore.Background = scuro.Background ?? colore.Background;
            colore.FontWeight = scuro.FontWeight ?? colore.FontWeight;
            colore.FontStyle = scuro.FontStyle ?? colore.FontStyle;
        }
    }

    private static XshdSyntaxDefinition? CaricaXshd()
    {
        using var stream = typeof(App).Assembly
            .GetManifestResourceStream("EasyCPU.Resources.EasyCPU.xshd");
        if (stream is null) return null;
        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.LoadXshd(reader);
    }

    // Definizione di evidenziazione adatta al tema effettivo del controllo
    public static IHighlightingDefinition? EasyCpuHighlighting(ThemeVariant tema) =>
        HighlightingManager.Instance.GetDefinition(tema == ThemeVariant.Dark ? "EasyCPU-Dark" : "EasyCPU");

    // Colore primario di Semi (blu) sostituito col teal dell'icona, preso dalla palette Teal di Semi.
    // I temi Semi leggono questi brush con StaticResource: un override nelle risorse dell'app non
    // li raggiungerebbe, quindi si ricolorano le istanze condivise.
    private static readonly Dictionary<string, int> GradiTealChiaro = new()
    {
        ["SemiColorPrimary"] = 6, ["SemiColorPrimaryPointerover"] = 7, ["SemiColorPrimaryActive"] = 8,
        ["SemiColorPrimaryDisabled"] = 2, ["SemiColorPrimaryLight"] = 0,
        ["SemiColorPrimaryLightPointerover"] = 1, ["SemiColorPrimaryLightActive"] = 2,
        ["SemiColorLink"] = 6, ["SemiColorLinkPointerover"] = 7, ["SemiColorLinkActive"] = 8,
        ["SemiColorLinkVisited"] = 6, ["SemiColorFocusBorder"] = 6,
    };

    // Nel tema scuro i brush "Light" sono il primario con opacità, già impostata da Semi
    private static readonly Dictionary<string, int> GradiTealScuro = new()
    {
        ["SemiColorPrimary"] = 5, ["SemiColorPrimaryPointerover"] = 6, ["SemiColorPrimaryActive"] = 7,
        ["SemiColorPrimaryDisabled"] = 2, ["SemiColorPrimaryLight"] = 5,
        ["SemiColorPrimaryLightPointerover"] = 5, ["SemiColorPrimaryLightActive"] = 5,
        ["SemiColorLink"] = 5, ["SemiColorLinkPointerover"] = 6, ["SemiColorLinkActive"] = 7,
        ["SemiColorLinkVisited"] = 5, ["SemiColorFocusBorder"] = 5,
    };

    private void ApplicaColorePrimario()
    {
        ApplicaColorePrimario(ThemeVariant.Default, GradiTealChiaro);
        ApplicaColorePrimario(ThemeVariant.Light, GradiTealChiaro);
        ApplicaColorePrimario(ThemeVariant.Dark, GradiTealScuro);
    }

    private void ApplicaColorePrimario(ThemeVariant tema, Dictionary<string, int> gradi)
    {
        foreach (var (chiave, grado) in gradi)
            if (TryGetResource(chiave, tema, out var brush) && brush is SolidColorBrush solido
                && TryGetResource($"SemiTeal{grado}Color", tema, out var colore) && colore is Color teal)
                solido.Color = teal;
    }

    public static void ApplyTheme(AppTheme theme)
    {
        if (Current is null) return;

        Current.RequestedThemeVariant = theme == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}
