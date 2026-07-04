using System;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyCpu.Common;

namespace EasyCPU.ViewModels;

public partial class OpzioniViewModel : ObservableObject
{
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

    public FormatoValore[] FormatoValoreOptions { get; } =
        (FormatoValore[])Enum.GetValues(typeof(FormatoValore));

    public int[] ColonneStackOptions { get; } = [1, 2];

    public OpzioniViewModel(SettingsViewModel src)
    {
        _formatoDati       = src.FormatoDati;
        _formatoCarZero    = src.FormatoCarZero;
        _maxNumErrori      = src.MaxNumErrori;
        _colonneStack      = src.ColonneStack;
        _inizializzaRegistri = src.InizializzaRegistri;
        _loopInfinito      = src.LoopInfinito;
        _margineSinistro   = src.MargineSinistro;
        _mostraMemoria     = src.MostraMemoria;
        _pienoSchermo      = src.PienoSchermo;
        _fontEditorNome    = src.FontEditorNome;
        _fontEditorSize    = src.FontEditorSize;
        _fontEditorStyle   = src.FontEditorStyle;
        _fontPanelliSize   = src.FontPanelliSize;
    }

    public void ApplyTo(SettingsViewModel dst)
    {
        dst.FormatoDati        = FormatoDati;
        dst.FormatoCarZero     = FormatoCarZero;
        dst.MaxNumErrori       = MaxNumErrori;
        dst.ColonneStack       = ColonneStack;
        dst.InizializzaRegistri = InizializzaRegistri;
        dst.LoopInfinito       = LoopInfinito;
        dst.MargineSinistro    = MargineSinistro;
        dst.MostraMemoria      = MostraMemoria;
        dst.PienoSchermo       = PienoSchermo;
        dst.FontEditorNome     = FontEditorNome;
        dst.FontEditorSize     = FontEditorSize;
        dst.FontEditorStyle    = FontEditorStyle;
        dst.FontPanelliSize    = FontPanelliSize;
    }
}
