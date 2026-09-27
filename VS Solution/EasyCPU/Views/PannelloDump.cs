using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using EasyCPU.ViewModels;

namespace EasyCPU.Views;

// Mostra le righe di un pannello di debug nel suo TextBlock: una Run per segmento,
// con la classe "cambiato" (sfondo SfondoValoreCambiato) per i valori modificati
internal static class PannelloDump
{
    public static void Collega(UserControl view, TextBlock tb)
    {
        PannelloDumpViewModel? vm = null;

        void Mostra()
        {
            var inlines = new InlineCollection();
            var righe = vm?.Righe ?? [];
            for (int r = 0; r < righe.Count; r++)
            {
                if (r > 0) inlines.Add(new LineBreak());
                foreach (var s in righe[r])
                {
                    if (s.Testo.Length == 0) continue;
                    var run = new Run(s.Testo);
                    if (s.Cambiato) run.Classes.Add("cambiato");
                    inlines.Add(run);
                }
            }
            tb.Inlines = inlines;
        }

        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PannelloDumpViewModel.Righe)) Mostra();
        }

        view.DataContextChanged += (_, _) =>
        {
            if (vm is not null) vm.PropertyChanged -= OnPropertyChanged;
            vm = view.DataContext as PannelloDumpViewModel;
            if (vm is not null) vm.PropertyChanged += OnPropertyChanged;
            Mostra();
        };
    }
}
