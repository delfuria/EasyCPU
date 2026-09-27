using System;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class CodeEditorViewModel : Document
{
    public MainViewModel MainVm { get; }

    // Updated by the view code-behind when the document text changes
    public string SourceText { get; set; } = "";

    // Updated by the view code-behind when the caret moves (1-based)
    public int CurrentLine { get; set; } = 1;

    // Populated by CodeEditorView.axaml.cs; invoked by MainViewModel commands
    internal Action? UndoAction;
    internal Action? RedoAction;
    internal Action? CutAction;
    internal Action? CopyAction;
    internal Action? PasteAction;
    internal Action? SelectAllAction;
    internal Action? FindAction;
    internal Action<string>? SetSourceTextAction;
    internal Action<int>? NavigateToLineAction;
    // Riga richiesta mentre la view non era nell'albero visivo: al cambio di tab Dock
    // crea una nuova view, che la raggiunge quando si collega (0 = nessuna)
    internal int PendingNavigateLine;

    public CodeEditorViewModel(MainViewModel mainVm)
    {
        MainVm = mainVm;
    }
}
