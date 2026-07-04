using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;
using EasyCpu.Assembler.Parsing;

namespace EasyCPU.ViewModels;

// Wraps CompilerError (which has public fields, not properties) so that
// Avalonia compiled bindings can resolve TipoDisplay/RigaDisplay/Msg.
public class CompilerErrorAdapter
{
    public string TipoDisplay { get; }
    public int    RigaDisplay { get; }  // 1-based for display
    public string Msg         { get; }
    public CompilerError Source { get; }

    public CompilerErrorAdapter(CompilerError err)
    {
        TipoDisplay = err.Tipo == CompilerError.CODICE ? "Codice" : "Dati";
        RigaDisplay = err.Riga + 1;
        Msg         = err.Msg;
        Source      = err;
    }
}

public partial class ErrorsViewModel : Tool
{
    public MainViewModel MainVm { get; }
    public ObservableCollection<CompilerErrorAdapter> Errors { get; } = new();

    public ErrorsViewModel(MainViewModel mainVm)
    {
        MainVm = mainVm;
    }
}
