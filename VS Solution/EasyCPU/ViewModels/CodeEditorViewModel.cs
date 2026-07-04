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

    public CodeEditorViewModel(MainViewModel mainVm)
    {
        MainVm = mainVm;
    }
}
