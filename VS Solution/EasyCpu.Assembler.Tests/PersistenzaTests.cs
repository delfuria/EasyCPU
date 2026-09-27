using EasyCpu.Backend.Local;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

// Archivio delle impostazioni: Storage con un archivio in memoria (come localStorage nel browser)
// e ArchivioFile con gli stessi percorsi usati prima dell'introduzione dell'archivio.
public class PersistenzaTests
{
    sealed class ArchivioInMemoria : IArchivioImpostazioni
    {
        public readonly Dictionary<string, string> Voci = new();
        public string? Leggi(string chiave) => Voci.TryGetValue(chiave, out var testo) ? testo : null;
        public void Scrivi(string chiave, string testo) => Voci[chiave] = testo;
        public void Elimina(string chiave) => Voci.Remove(chiave);
    }

    // Esegue il test con un archivio dato e ripristina quello precedente (Storage e Ambiente sono statici).
    static void ConArchivio(IArchivioImpostazioni archivio, Action test)
    {
        var precedente = Storage.Archivio;
        Storage.Archivio = archivio;
        try { test(); }
        finally
        {
            Storage.Archivio = precedente;
            Ambiente.Inizializza();
            Ambiente.FileRecenti = [];
        }
    }

    [Fact]
    public void Opzioni_AndataERitorno()
    {
        var archivio = new ArchivioInMemoria();
        ConArchivio(archivio, () =>
        {
            Ambiente.Inizializza();
            Ambiente.FormatoDati = FormatoValore.Hex;
            Ambiente.FontEditorSize = 18;
            Storage.SalvaOpzioni();
            Assert.Contains("\"Hex\"", archivio.Voci["opzioni"]);

            Ambiente.Inizializza();             // come un nuovo avvio
            Assert.Equal(FormatoValore.Dec, Ambiente.FormatoDati);
            Storage.LeggiOpzioni();
            Assert.Equal(FormatoValore.Hex, Ambiente.FormatoDati);
            Assert.Equal(18, Ambiente.FontEditorSize);
        });
    }

    [Fact]
    public void Opzioni_ArchivioVuoto_RestanoIPredefiniti()
    {
        ConArchivio(new ArchivioInMemoria(), () =>
        {
            Ambiente.Inizializza();
            Storage.LeggiOpzioni();
            Assert.Equal(FormatoValore.Dec, Ambiente.FormatoDati);
        });
    }

    [Fact]
    public void Recenti_AndataERitorno()
    {
        ConArchivio(new ArchivioInMemoria(), () =>
        {
            Ambiente.FileRecenti = [];
            Ambiente.AggiungiRecenti("uno.asj");
            Ambiente.AggiungiRecenti("due.asj");
            Storage.SalvaFileRecenti();

            Ambiente.FileRecenti = [];
            Storage.ApriFileRecenti();
            Assert.Equal(["due.asj", "uno.asj"], Ambiente.FileRecenti);
        });
    }

    [Fact]
    public void ArchivioFile_UsaIPercorsiDiPrima()
    {
        string cartella = Path.Combine(Path.GetTempPath(), "easycpu-test-" + Guid.NewGuid().ToString("N"));
        string opzioni = Ambiente.OpzioniNomeFile, recenti = Ambiente.RecentiNomeFile, base_ = Ambiente.EasyCPUPath;
        try
        {
            Ambiente.EasyCPUPath = Path.Combine(cartella, "EasyCPU");
            Ambiente.OpzioniNomeFile = Path.Combine(Ambiente.EasyCPUPath, "opzioni.json");
            Ambiente.RecentiNomeFile = Path.Combine(Ambiente.EasyCPUPath, "recenti.json");
            var archivio = new ArchivioFile();

            archivio.Scrivi("opzioni", "o");
            archivio.Scrivi("recenti", "r");
            archivio.Scrivi("layout", "l");
            string programma = Path.Combine(cartella, "prova.asj");
            archivio.Scrivi(ArchivioFile.PrefissoBreakpoint + programma, "3\n7");

            Assert.Equal("o", File.ReadAllText(Path.Combine(Ambiente.EasyCPUPath, "opzioni.json")));
            Assert.Equal("r", File.ReadAllText(Path.Combine(Ambiente.EasyCPUPath, "recenti.json")));
            Assert.Equal("l", File.ReadAllText(Path.Combine(Ambiente.EasyCPUPath, "layout.json")));
            Assert.Equal("3\n7", File.ReadAllText(programma + ".bkpt"));    // accanto al programma
            Assert.Equal("3\n7", archivio.Leggi(ArchivioFile.PrefissoBreakpoint + programma));

            archivio.Elimina(ArchivioFile.PrefissoBreakpoint + programma);
            Assert.False(File.Exists(programma + ".bkpt"));
            Assert.Null(archivio.Leggi("inesistente"));
        }
        finally
        {
            Ambiente.OpzioniNomeFile = opzioni;
            Ambiente.RecentiNomeFile = recenti;
            Ambiente.EasyCPUPath = base_;
            if (Directory.Exists(cartella)) Directory.Delete(cartella, true);
        }
    }
}
