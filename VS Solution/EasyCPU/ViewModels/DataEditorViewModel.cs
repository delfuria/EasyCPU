using System;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class DataEditorViewModel : Document
{
    public MainViewModel MainVm { get; }

    public string SourceText { get; set; } = "";
    internal Action<string>? SetSourceTextAction;
    internal Action<int>? NavigateToLineAction;
    // Riga richiesta mentre la view non era nell'albero visivo: al cambio di tab Dock
    // crea una nuova view, che la raggiunge quando si collega (0 = nessuna)
    internal int PendingNavigateLine;

    public DataEditorViewModel(MainViewModel mainVm)
    {
        MainVm = mainVm;
    }
}
