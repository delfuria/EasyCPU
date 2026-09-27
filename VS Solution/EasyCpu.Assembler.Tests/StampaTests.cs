using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Parsing;
using EasyCPU;

namespace EasyCpu.Assembler.Tests;

// Stampa (menu File > Stampa): pagina HTML con dati (e indirizzi) e codice.
public class StampaTests
{
    static readonly List<string> Dati =
    [
        "DIM     EQU 4",
        "tab     DW DIM DUP(7)",
        "vuoto   DB 3 DUP(?)",
        "        ORG 20",
        "dopo    DW 1, 2",
        "ptr     DW offset dopo",
        "10: 99",
    ];

    static (int, int)?[] CelleDati(List<string> dati, ModelloMemoria modello)
    {
        var compilatore = new Compiler { Modello = modello };
        List<CompilerError> errori = null!;
        compilatore.CompilaDati(dati, ref errori);
        Assert.Null(errori);
        return compilatore.CelleDati;
    }

    static IHighlightingDefinition Evidenziazione()
    {
        using var stream = typeof(Stampa).Assembly.GetManifestResourceStream("EasyCPU.Resources.EasyCPU.xshd")!;
        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.Load(HighlightingLoader.LoadXshd(reader), HighlightingManager.Instance);
    }

    [Fact]
    public void CelleDati_IndirizzoDiOgniRiga()
    {
        var celle = CelleDati(Dati, ModelloMemoria.Parole);
        Assert.Equal<(int, int)?[]>([null, (0, 4), (4, 3), null, (20, 2), (22, 1), (10, 1)], celle);
    }

    [Fact]
    public void CelleDati_MemoriaAByte_ContaIByte()
    {
        var celle = CelleDati(["a DB 1, 2", "b DW 5, 6"], ModelloMemoria.Byte);
        Assert.Equal<(int, int)?[]>([(0, 2), (2, 4)], celle);
    }

    [Fact]
    public void Html_ColonnaIndirizzi()
    {
        var html = Stampa.GeneraHtml("prova.asj", "", Dati, CelleDati(Dati, ModelloMemoria.Parole), ["stop"], null);
        Assert.Contains("<th>Indirizzo</th>", html);
        Assert.Contains("<td class=\"ind\">0–3</td>", html);
        Assert.Contains("<td class=\"ind\">22</td>", html);
        Assert.Contains("<td class=\"n\">4</td><td class=\"ind\"></td>", html);     // ORG: nessuna cella
    }

    [Fact]
    public void Html_DatiNonCompilati_SenzaColonnaIndirizzi()
    {
        var html = Stampa.GeneraHtml("prova.asj", "", ["x DQ 1"], null, ["stop"], null);
        Assert.DoesNotContain("Indirizzo", html);
        Assert.DoesNotContain("class=\"ind\"", html);
        Assert.Contains("x DQ 1", html);
    }

    [Fact]
    public void Html_SenzaDati_SoloCodice()
    {
        var html = Stampa.GeneraHtml("prova.asj", "", [""], [null], ["mov ax, 1", "stop", "", ""], null);
        Assert.DoesNotContain("<h2>Dati</h2>", html);
        Assert.Contains("<h2>Codice</h2>", html);
        Assert.Contains("<td class=\"n\">2</td>", html);
        Assert.DoesNotContain("<td class=\"n\">3</td>", html);        // righe vuote finali omesse
    }

    [Fact]
    public void Html_TestoCodificato()
    {
        var html = Stampa.GeneraHtml("a<b>.asj", "", [], null, ["mov al, '<'   // a & b"], null);
        Assert.Contains("<title>a&lt;b&gt;.asj</title>", html);
        Assert.Contains("&lt;", html);
        Assert.Contains("a &amp; b", html);
        Assert.DoesNotContain("'<'", html);
    }

    [Fact]
    public void Html_ColoriDellaSintassi()
    {
        var html = Stampa.GeneraHtml("prova.asj", "", [], null, ["ciclo:  mov ax, 5   // commento"], Evidenziazione());
        Assert.Contains("color: #1565C0", html, StringComparison.OrdinalIgnoreCase);     // mov: OpcodeMove
        Assert.Contains("color: #008000", html, StringComparison.OrdinalIgnoreCase);     // commento
        Assert.Contains("commento", html);
    }
}
