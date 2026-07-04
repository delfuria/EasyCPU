using CommunityToolkit.Mvvm.ComponentModel;
using EasyCpu.Common;

namespace EasyCPU.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public static SettingsViewModel Instance { get; } = new();

    [ObservableProperty] private AppTheme _theme = AppTheme.Light;
    [ObservableProperty] private FormatoValore _formatoDati;
    [ObservableProperty] private string _formatoCarZero = "";
    [ObservableProperty] private int _maxNumErrori;
    [ObservableProperty] private int _colonneStack;
    [ObservableProperty] private bool _inizializzaRegistri;
    [ObservableProperty] private int _loopInfinito;
    [ObservableProperty] private int _margineSinistro;
    [ObservableProperty] private bool _mostraMemoria;
    [ObservableProperty] private bool _pienoSchermo;
    [ObservableProperty] private string _fontEditorNome = "";
    [ObservableProperty] private float _fontEditorSize;
    [ObservableProperty] private int _fontEditorStyle;
    [ObservableProperty] private float _fontPanelliSize;

    private SettingsViewModel()
    {
        _formatoDati         = Ambiente.FormatoDati;
        _formatoCarZero      = Ambiente.FormatoCarZero ?? "\\0";
        _maxNumErrori        = Ambiente.MaxNumErrori;
        _colonneStack        = Ambiente.ColonneStack;
        _inizializzaRegistri = Ambiente.InizializzaRegistri;
        _loopInfinito        = Ambiente.LoopInfinito;
        _margineSinistro     = Ambiente.MargineSinistro;
        _mostraMemoria       = Ambiente.MostraMemoria;
        _pienoSchermo        = Ambiente.PienoSchermo;
        _fontEditorNome      = Ambiente.FontEditorNome ?? "Courier New";
        _fontEditorSize      = Ambiente.FontEditorSize;
        _fontEditorStyle     = Ambiente.FontEditorStyle;
        _fontPanelliSize     = Ambiente.FontPanelliSize;
    }

    partial void OnThemeChanged(AppTheme value)              => App.ApplyTheme(value);
    partial void OnFormatoDatiChanged(FormatoValore value)   => Ambiente.FormatoDati = value;
    partial void OnFormatoCarZeroChanged(string value)       => Ambiente.FormatoCarZero = value;
    partial void OnMaxNumErroriChanged(int value)            => Ambiente.MaxNumErrori = value;
    partial void OnColonneStackChanged(int value)            => Ambiente.ColonneStack = value;
    partial void OnInizializzaRegistriChanged(bool value)    => Ambiente.InizializzaRegistri = value;
    partial void OnLoopInfinitoChanged(int value)            => Ambiente.LoopInfinito = value;
    partial void OnMargineSinistroChanged(int value)         => Ambiente.MargineSinistro = value;
    partial void OnMostraMemoriaChanged(bool value)          => Ambiente.MostraMemoria = value;
    partial void OnPienoSchermoChanged(bool value)           => Ambiente.PienoSchermo = value;
    partial void OnFontEditorNomeChanged(string value)       => Ambiente.FontEditorNome = value;
    partial void OnFontEditorSizeChanged(float value)        => Ambiente.FontEditorSize = value;
    partial void OnFontEditorStyleChanged(int value)         => Ambiente.FontEditorStyle = value;
    partial void OnFontPanelliSizeChanged(float value)       => Ambiente.FontPanelliSize = value;
}
