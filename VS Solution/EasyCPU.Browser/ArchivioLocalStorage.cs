using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using EasyCpu.Backend.Local;

// Archivio delle impostazioni nel localStorage del browser, tramite wwwroot/persistenza.js.
[SupportedOSPlatform("browser")]
internal sealed partial class ArchivioLocalStorage : IArchivioImpostazioni
{
    public const string Modulo = "persistenza";

    [JSImport("leggi", Modulo)]
    private static partial string? LeggiJs(string chiave);

    [JSImport("scrivi", Modulo)]
    private static partial bool ScriviJs(string chiave, string testo);

    [JSImport("elimina", Modulo)]
    private static partial void EliminaJs(string chiave);

    [JSImport("registraSalvataggioAllUscita", Modulo)]
    public static partial void RegistraSalvataggioAllUscita([JSMarshalAs<JSType.Function>] Action salva);

    public string? Leggi(string chiave) => LeggiJs(chiave);
    public void Scrivi(string chiave, string testo) => ScriviJs(chiave, testo);
    public void Elimina(string chiave) => EliminaJs(chiave);
}
