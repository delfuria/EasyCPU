using EasyCpu.Assembler.Processore;

namespace EasyCpu.Assembler.Tests;

// Evidenziazione dei valori cambiati nei pannelli Registri, Memoria e Stack
public class DiffPannelloTests
{
    // I segmenti cambiati racchiusi fra « »
    static string[] Marca(string[]? vecchie, string[] nuove) =>
        DiffPannello.Confronta(vecchie, nuove)
            .Select(r => string.Concat(r.Select(s => s.Cambiato ? "«" + s.Testo + "»" : s.Testo)))
            .ToArray();

    [Fact]
    public void SenzaPrecedente_NienteEvidenziato()
    {
        var righe = DiffPannello.Confronta(null, ["0000: 0001 0002 "]);
        Assert.Equal([new Segmento("0000: 0001 0002 ", false)], righe.Single());
    }

    [Fact]
    public void RigaUguale_UnSoloSegmento()
    {
        var righe = DiffPannello.Confronta(["0000: 0001 0002 "], ["0000: 0001 0002 "]);
        Assert.Equal([new Segmento("0000: 0001 0002 ", false)], righe.Single());
    }

    [Fact]
    public void MemoriaHex_SoloLaCellaCambiata()
    {
        Assert.Equal(["0000: 0000 «0007» 0000 "], Marca(["0000: 0000 0005 0000 "], ["0000: 0000 0007 0000 "]));
    }

    [Fact]
    public void MemoriaByte_PiuCelle()
    {
        Assert.Equal(["0000: «41» 00 «FF» 00 "], Marca(["0000: 00 00 00 00 "], ["0000: 41 00 FF 00 "]));
    }

    [Theory]
    [InlineData("    5", "   12", "   «12»")]     // il numero si allunga
    [InlineData("   12", "    5", "    «5»")]     // si accorcia: la cifra rimasta
    [InlineData("   -1", "    1", "    «1»")]     // cambia solo il segno
    public void Decimale_AllineatoADestra(string prima, string dopo, string atteso)
    {
        Assert.Equal(["   0: " + atteso + " "], Marca(["   0: " + prima + " "], ["   0: " + dopo + " "]));
    }

    [Fact]
    public void Registro_ValoreEByte()
    {
        Assert.Equal(["AX = «0141» [AH=«01» AL=41]"],
            Marca(["AX = 0041 [AH=00 AL=41]"], ["AX = 0141 [AH=01 AL=41]"]));
    }

    [Fact]
    public void Flag_SoloLaCifra()
    {
        Assert.Equal(["C=«1»  Z=0  S=0  O=0  D=0"],
            Marca(["C=0  Z=0  S=0  O=0  D=0"], ["C=1  Z=0  S=0  O=0  D=0"]));
    }

    [Fact]
    public void Simbolo_Valore()
    {
        Assert.Equal(["vet        [000A] DW x5 = «3»"],
            Marca(["vet        [000A] DW x5 = 1"], ["vet        [000A] DW x5 = 3"]));
    }

    [Fact]
    public void Caratteri_SpazioIsolatoEvidenziato()
    {
        Assert.Equal(["   0:     A    « » "], Marca(["   0:     A    B "], ["   0:     A      "]));
    }

    [Fact]
    public void RigaNuova_NonEvidenziata()
    {
        Assert.Equal(["AX = «0001»", "Simboli:"], Marca(["AX = 0000"], ["AX = 0001", "Simboli:"]));
    }
}
