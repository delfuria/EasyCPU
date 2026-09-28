using System.Xml;
using Avalonia.Media;
using AvaloniaEdit.Highlighting.Xshd;
using EasyCPU;

namespace EasyCpu.Assembler.Tests;

// EasyCPU.xshd: palette chiara nei colori "X", palette scura nei colori "Dark.X".
public class EvidenziazioneTests
{
    static List<XshdColor> Colori(XshdSyntaxDefinition definizione) =>
        definizione.Elements.OfType<XshdColor>().ToList();

    static XshdSyntaxDefinition Xshd()
    {
        using var stream = typeof(App).Assembly.GetManifestResourceStream("EasyCPU.Resources.EasyCPU.xshd")!;
        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.LoadXshd(reader);
    }

    static Color? Colore(XshdSyntaxDefinition definizione, string nome) =>
        Colori(definizione).Single(c => c.Name == nome).Foreground.GetColor(null);

    [Fact]
    public void OgniColoreScuro_HaIlCorrispondenteChiaro()
    {
        var nomi = Colori(Xshd()).Select(c => c.Name).ToHashSet();
        var orfani = nomi.Where(n => n!.StartsWith(App.PrefissoTemaScuro))
                         .Where(n => !nomi.Contains(n![App.PrefissoTemaScuro.Length..]));
        Assert.Empty(orfani);
    }

    [Fact]
    public void OgniColoreChiaro_HaIlCorrispondenteScuro()
    {
        var nomi = Colori(Xshd()).Select(c => c.Name).ToHashSet();
        var senzaScuro = nomi.Where(n => !n!.StartsWith(App.PrefissoTemaScuro))
                             .Where(n => !nomi.Contains(App.PrefissoTemaScuro + n));
        Assert.Empty(senzaScuro);
    }

    [Fact]
    public void TemaScuro_SostituisceColoreEMantieneGrassetto()
    {
        var definizione = Xshd();
        Assert.Equal(Color.Parse("#800080"), Colore(definizione, "Directive"));

        App.ApplicaTemaScuro(definizione);

        var direttiva = Colori(definizione).Single(c => c.Name == "Directive");
        Assert.Equal(Color.Parse("#C586C0"), direttiva.Foreground.GetColor(null));
        Assert.Equal(FontWeight.Bold, direttiva.FontWeight);
    }
}
