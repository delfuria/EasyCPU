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

    // Palette per il tema scuro: stessi nomi dei <Color> di EasyCPU.xshd (che contiene quella chiara)
    private static readonly Dictionary<string, string> ColoriTemaScuro = new()
    {
        ["Comment"]         = "#C5E1A5",
        ["Register"]        = "#4EC9B0",
        ["Indirect"]        = "#9CDCFE",
        ["Directive"]       = "#C586C0",
        ["Number"]          = "#B5CEA8",
        ["CharConst"]       = "#CE9178",
        ["Label"]           = "#DCDCAA",
        ["OpcodeMove"]      = "#64B5F6",   // blu chiaro
        ["OpcodeArith"]     = "#FFB74D",   // arancio
        ["OpcodeLogic"]     = "#4DD0E1",   // ciano
        ["OpcodeCmp"]       = "#F48FB1",   // rosa
        ["OpcodeStack"]     = "#B39DDB",   // lavanda
        ["OpcodeJump"]      = "#FF6E6E",   // rosso chiaro
        ["OpcodeMisc"]      = "#B0BEC5",   // grigio-blu chiaro
        ["OpcodeInterrupt"] = "#C5E1A5",   // verde chiaro
    };

    private static void RegisterEasyCpuHighlighting()
    {
        var chiaro = CaricaXshd();
        var scuro = CaricaXshd();
        if (chiaro is null || scuro is null) return;

        scuro.Name = "EasyCPU-Dark";
        foreach (var colore in scuro.Elements.OfType<XshdColor>())
            if (colore.Name is not null && ColoriTemaScuro.TryGetValue(colore.Name, out var hex))
                colore.Foreground = new SimpleHighlightingBrush(Color.Parse(hex));

        var manager = HighlightingManager.Instance;
        manager.RegisterHighlighting("EasyCPU", [".as", ".asj"], HighlightingLoader.Load(chiaro, manager));
        manager.RegisterHighlighting("EasyCPU-Dark", [], HighlightingLoader.Load(scuro, manager));
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

    public static void ApplyTheme(AppTheme theme)
    {
        if (Current is null) return;

        Current.RequestedThemeVariant = theme == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}
