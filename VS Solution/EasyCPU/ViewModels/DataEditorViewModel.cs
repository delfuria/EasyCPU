using System;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class DataEditorViewModel : Document
{
    public MainViewModel MainVm { get; }

    public string SourceText { get; set; } = "";
    internal Action<string>? SetSourceTextAction;
    internal Action<int>? NavigateToLineAction;

    public DataEditorViewModel(MainViewModel mainVm)
    {
        MainVm = mainVm;
    }
}
