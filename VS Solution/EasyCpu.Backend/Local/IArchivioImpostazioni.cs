namespace EasyCpu.Backend.Local;

// Archivio delle impostazioni dell'IDE: testo associato a una chiave logica.
// Chiavi usate: "opzioni", "recenti", "layout", "breakpoint:<file>", "programma:<nome>".
// Implementazioni: ArchivioFile (Desktop, file in EasyCPUPath) e, nel progetto Browser, localStorage.
public interface IArchivioImpostazioni
{
    string? Leggi(string chiave);           // null se la chiave non esiste
    void Scrivi(string chiave, string testo);
    void Elimina(string chiave);
}
