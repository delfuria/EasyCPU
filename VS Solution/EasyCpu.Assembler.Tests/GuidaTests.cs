using EasyCpu.Assembler.Parsing;
using EasyCpu.DocGen;

namespace EasyCpu.Assembler.Tests;

// La guida (F1) è Docs/html/reference.html, generata da Docs/Easy CPU  Assembly Reference.md
// con EasyCpu.DocGen e salvata nel repository.
public class GuidaTests
{
    static string CartellaDocs()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Docs", "html")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "Docs");
    }

    static string Markdown() => File.ReadAllText(Path.Combine(CartellaDocs(), "Easy CPU  Assembly Reference.md"));

    [Fact]
    public void HtmlAggiornatoRispettoAlMarkdown()
    {
        var salvato = File.ReadAllText(Path.Combine(CartellaDocs(), "html", "reference.html")).Replace("\r\n", "\n");
        Assert.True(Generatore.Genera(Markdown()) == salvato,
            "Docs/html/reference.html non corrisponde al .md: rigenerarlo con EasyCpu.DocGen (0.build-all.sh).");
    }

    [Fact]
    public void OgniIstruzioneHaLaSuaAncora()
    {
        var ancore = Generatore.Ancore(Markdown());
        var mancanti = Parser.SetCode.Select(o => o.Nome).Where(n => !ancore.Contains(n)).ToList();
        Assert.Empty(mancanti);
    }
}
