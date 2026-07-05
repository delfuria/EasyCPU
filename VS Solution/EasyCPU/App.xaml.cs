using System;
using System.Xml;
using EasyCpu.Backend.Local;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        _mainViewModel?.SaveAll();
    }

    private void OnAboutClick(object? sender, EventArgs e)
    {
        var owner = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var about = new AboutWindow();
        if (owner is not null)
            about.ShowDialog(owner);
        else
            about.Show();
    }

    private static void RegisterEasyCpuHighlighting()
    {
        using var stream = typeof(App).Assembly
            .GetManifestResourceStream("EasyCPU.Resources.EasyCPU.xshd");
        if (stream is null) return;
        using var reader = new XmlTextReader(stream);
        var def = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting("EasyCPU", [".as", ".asj"], def);
    }

    public static void ApplyTheme(AppTheme theme)
    {
        if (Current is null) return;

        Current.RequestedThemeVariant = theme == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}
