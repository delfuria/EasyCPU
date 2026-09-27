using System.IO;
using EasyCpu.Common;

namespace EasyCpu.Backend.Local;

// Archivio su file: stessi percorsi e formati usati prima dell'introduzione dell'archivio.
public sealed class ArchivioFile : IArchivioImpostazioni
{
    public const string PrefissoBreakpoint = "breakpoint:";
    public const string PrefissoProgramma = "programma:";

    static string Percorso(string chiave)
    {
        if (chiave.StartsWith(PrefissoBreakpoint))
            return chiave.Substring(PrefissoBreakpoint.Length) + ".bkpt";     // accanto al programma
        return chiave switch
        {
            "opzioni" => Ambiente.OpzioniNomeFile,
            "recenti" => Ambiente.RecentiNomeFile,
            "layout" => Path.Combine(Ambiente.EasyCPUPath, "layout.json"),
            _ => Path.Combine(Ambiente.EasyCPUPath, chiave.Replace(':', '_') + ".txt"),
        };
    }

    public string? Leggi(string chiave)
    {
        string percorso = Percorso(chiave);
        return File.Exists(percorso) ? File.ReadAllText(percorso) : null;
    }

    public void Scrivi(string chiave, string testo)
    {
        string percorso = Percorso(chiave);
        string? cartella = Path.GetDirectoryName(percorso);
        if (!string.IsNullOrEmpty(cartella))
            Directory.CreateDirectory(cartella);
        File.WriteAllText(percorso, testo);
    }

    public void Elimina(string chiave)
    {
        string percorso = Percorso(chiave);
        if (File.Exists(percorso))
            File.Delete(percorso);
    }
}
