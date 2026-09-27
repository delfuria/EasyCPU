using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace EasyCPU;

/// <summary>
/// Apre l'Assembly Reference (Docs/html/reference.html, generata da EasyCpu.DocGen) nel browser,
/// eventualmente su un'ancora: le istruzioni hanno ancore col proprio nome (#mov, #jz...).
/// Desktop: copia locale incorporata nell'assembly, funziona anche senza rete.
/// Browser: la copia pubblicata con l'app (wwwroot/help). Mobile: la copia online.
/// </summary>
public static class Guida
{
    // Il browser la sostituisce con l'indirizzo della copia pubblicata accanto all'app (Program.cs).
    public static Uri IndirizzoWeb { get; set; } = new("https://delfuria.github.io/EasyCPU/help/reference.html");

    public static Task<bool> ApriAsync(TopLevel? topLevel, bool desktop, string? ancora)
    {
        if (topLevel is null) return Task.FromResult(false);
        var frammento = string.IsNullOrEmpty(ancora) ? "" : "#" + ancora;
        return desktop
            ? topLevel.Launcher.LaunchFileInfoAsync(new FileInfo(CopiaLocale(frammento)))
            : topLevel.Launcher.LaunchUriAsync(new Uri(IndirizzoWeb + frammento));
    }

    // Scrive la guida in una cartella temporanea e restituisce il file da aprire.
    // Passato al sistema, un file:// perde l'ancora (Windows): per raggiungerla si apre
    // una pagina che reindirizza a reference.html#ancora.
    private static string CopiaLocale(string frammento)
    {
        var cartella = Path.Combine(Path.GetTempPath(), "EasyCPU-guida");
        Directory.CreateDirectory(cartella);
        var guida = Path.Combine(cartella, "reference.html");
        using (var risorsa = typeof(Guida).Assembly.GetManifestResourceStream("reference.html")!)
        using (var file = File.Create(guida))
            risorsa.CopyTo(file);

        if (frammento.Length == 0) return guida;

        var vai = Path.Combine(cartella, "vai.html");
        File.WriteAllText(vai,
            $"<!doctype html><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"0; url=reference.html{frammento}\">");
        return vai;
    }
}
