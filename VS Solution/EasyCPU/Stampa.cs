#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;

namespace EasyCPU;

/// <summary>
/// Stampa del programma: una pagina HTML (dati con i loro indirizzi, poi codice) stampata dal browser.
/// Desktop e mobile: file temporaneo aperto nel browser predefinito, che avvia da sé la stampa.
/// Browser: la pagina è stampata da un iframe nascosto (EasyCPU.Browser imposta NelBrowser).
/// </summary>
public static class Stampa
{
    // Impostata da EasyCPU.Browser: stampa la pagina senza lasciare l'app.
    public static Action<string>? NelBrowser { get; set; }

    public static Task<bool> ApriAsync(TopLevel? topLevel, string html)
    {
        if (NelBrowser is not null)
        {
            NelBrowser(html);
            return Task.FromResult(true);
        }
        if (topLevel is null) return Task.FromResult(false);

        var cartella = Path.Combine(Path.GetTempPath(), "EasyCPU-stampa");
        Directory.CreateDirectory(cartella);
        var file = Path.Combine(cartella, "stampa.html");
        File.WriteAllText(file, html.Replace("</body>", "<script>onload = () => print();</script></body>"));
        return topLevel.Launcher.LaunchFileInfoAsync(new FileInfo(file));
    }

    /// <param name="celleDati">Per ogni riga dei dati la prima cella e il numero di celle
    /// (Compiler.CelleDati); null se i dati non compilano: la colonna degli indirizzi è omessa.</param>
    /// <param name="evidenziazione">Colori della sintassi (tema chiaro); null per il testo semplice.</param>
    public static string GeneraHtml(string titolo, string sottotitolo,
        IReadOnlyList<string> dati, (int Inizio, int Celle)?[]? celleDati,
        IReadOnlyList<string> codice, IHighlightingDefinition? evidenziazione)
    {
        var sb = new StringBuilder();
        sb.Append($$"""
            <!doctype html>
            <html lang="it">
            <head>
            <meta charset="utf-8">
            <title>{{Html(titolo)}}</title>
            <style>
              @page { margin: 15mm; @bottom-right { content: "pag. " counter(page) " di " counter(pages); font: 9pt sans-serif; } }
              body { font: 10pt sans-serif; color: #000; background: #fff; margin: 0; print-color-adjust: exact; -webkit-print-color-adjust: exact; }
              h1 { font-size: 14pt; margin: 0; }
              .sottotitolo { color: #555; margin: 2pt 0 12pt; }
              h2 { font-size: 11pt; border-bottom: 1px solid #999; margin: 14pt 0 4pt; break-after: avoid; }
              table { border-collapse: collapse; width: 100%; }
              td { font: 9.5pt/1.35 Consolas, "Cascadia Mono", Menlo, "DejaVu Sans Mono", monospace; vertical-align: top; padding: 0 6pt 0 0; }
              td.n { color: #888; text-align: right; width: 1%; white-space: nowrap; }
              td.ind { text-align: right; width: 1%; white-space: nowrap; padding-right: 12pt; }
              td.src { white-space: pre-wrap; overflow-wrap: anywhere; padding-left: 2ch; text-indent: -2ch; }
              th { font-weight: normal; color: #555; text-align: right; padding: 0 12pt 2pt 0; }
              tr { break-inside: avoid; }
            </style>
            </head>
            <body>
            <h1>{{Html(titolo)}}</h1>
            <div class="sottotitolo">{{Html(sottotitolo)}}</div>

            """);

        var righeDati = Evidenzia(dati, evidenziazione);
        if (righeDati.Count > 0)
        {
            sb.Append("<h2>Dati</h2>\n<table>\n");
            if (celleDati is not null)
                sb.Append("<tr><th></th><th>Indirizzo</th><th></th></tr>\n");
        }
        for (int i = 0; i < righeDati.Count; i++)
        {
            sb.Append($"<tr><td class=\"n\">{i + 1}</td>");
            if (celleDati is not null)
                sb.Append($"<td class=\"ind\">{Indirizzi(i < celleDati.Length ? celleDati[i] : null)}</td>");
            sb.Append($"<td class=\"src\">{righeDati[i]}</td></tr>\n");
        }
        if (righeDati.Count > 0)
            sb.Append("</table>\n");

        sb.Append("<h2>Codice</h2>\n<table>\n");
        var righeCodice = Evidenzia(codice, evidenziazione);
        for (int i = 0; i < righeCodice.Count; i++)
            sb.Append($"<tr><td class=\"n\">{i + 1}</td><td class=\"src\">{righeCodice[i]}</td></tr>\n");
        sb.Append("</table>\n</body>\n</html>\n");
        return sb.ToString();
    }

    private static string Indirizzi((int Inizio, int Celle)? celle) => celle switch
    {
        null => "",
        (var inizio, 1) => inizio.ToString(),
        var (inizio, n) => $"{inizio}–{inizio + n - 1}",
    };

    // Righe in HTML, con i colori dell'editor; le righe vuote finali sono omesse.
    private static List<string> Evidenzia(IReadOnlyList<string> righe, IHighlightingDefinition? definizione)
    {
        int n = righe.Count;
        while (n > 0 && string.IsNullOrWhiteSpace(righe[n - 1])) n--;

        var risultato = new List<string>(n);
        if (definizione is null)
        {
            for (int i = 0; i < n; i++) risultato.Add(Html(righe[i]));
        }
        else
        {
            var documento = new TextDocument(string.Join("\n", righe.Take(n)));
            using var evidenziatore = new DocumentHighlighter(documento, definizione);
            var opzioni = new HtmlOptions();
            for (int i = 1; i <= n; i++)
                risultato.Add(evidenziatore.HighlightLine(i).ToHtml(opzioni));
        }
        // una riga vuota deve comunque occupare la sua altezza
        for (int i = 0; i < n; i++)
            if (risultato[i].Length == 0) risultato[i] = "&nbsp;";
        return risultato;
    }

    private static string Html(string testo) => WebUtility.HtmlEncode(testo);
}
