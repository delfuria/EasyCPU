using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using EasyCpu.Assembler.Processore;

namespace EasyCPU.ViewModels;

// Pannello di debug (Registri, Memoria, Stack): righe del dump divise in segmenti,
// con i valori cambiati dall'ultima esecuzione evidenziati
public abstract partial class PannelloDumpViewModel : Tool
{
    private IReadOnlyList<string>? _precedenti;

    [ObservableProperty] private List<List<Segmento>> _righe = [];

    // evidenzia: confronta con le righe mostrate prima (dopo un passo o un'esecuzione);
    // altrimenti le nuove righe diventano solo la base del prossimo confronto
    public void Aggiorna(IReadOnlyList<string> righe, bool evidenzia)
    {
        Righe = DiffPannello.Confronta(evidenzia ? _precedenti : null, righe);
        _precedenti = righe;
    }

    public void Svuota() => Aggiorna([], false);
}
