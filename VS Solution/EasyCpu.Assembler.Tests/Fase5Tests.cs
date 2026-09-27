using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

// Fase 5: modello di memoria astratto (modalità a parole invariata) e modalità x86 fedele.
public class Fase5Tests
{
    [Fact]
    public void ModelloParole_LimitiAttuali()
    {
        var m = ModelloMemoria.Parole;
        Assert.Equal((256, 240, 1), (m.Dimensione, m.InizioStack, m.PassoParola));
    }

    [Fact]
    public void MemoriaAParole_ByteBassoDellaCella()
    {
        var mem = MemoriaCpu.Crea(ModelloMemoria.Parole);
        mem.ScriviParola(10, 0x12F0);
        Assert.Equal(unchecked((short)0xFFF0), mem.LeggiByte(10));     // estensione del segno
        mem.ScriviByte(10, 0x34);
        Assert.Equal(0x1234, mem.LeggiParola(10));                      // il byte alto resta
        Assert.Equal(0, mem.LeggiParola(11));                           // celle indipendenti
    }

    [Fact]
    public void MemoriaAParole_FuoriLimiti_ViolazioneMemoria()
    {
        var mem = MemoriaCpu.Crea(ModelloMemoria.Parole);
        Assert.Equal(CodiceErrore.ViolazioneMemoria, Assert.Throws<CpuException>(() => mem.LeggiParola(256)).err);
        Assert.Equal(CodiceErrore.ViolazioneMemoria, Assert.Throws<CpuException>(() => mem.ScriviByte(-1, 0)).err);
    }
}
