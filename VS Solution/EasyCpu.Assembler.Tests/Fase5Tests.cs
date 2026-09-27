using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Parsing;
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

    // ── Passo 3: byte ptr / word ptr (modalità a parole) ───────────────────

    static async Task<Cpu> Esegui(string[] codice, string[]? dati = null)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati((dati ?? []).ToList(), ref errori, codice.ToList());
        var istruzioni = compiler.CompilaCodice(codice.ToList(), ref errori);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        await cpu.Run();
        return cpu;
    }

    static string? Errore(string[] codice, string[]? dati = null)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        compiler.CompilaDati((dati ?? []).ToList(), ref errori, codice.ToList());
        compiler.CompilaCodice(codice.ToList(), ref errori);
        return errori?.First().Msg;
    }

    [Fact]
    public async Task BytePtr_AccedeAlByteBassoDellaCella()
    {
        var cpu = await Esegui(
        [
            "mov [10], 12FFh",
            "mov si, 10",
            "inc byte ptr [si]",            // FFh + 1 = 00h: il byte alto resta 12h
            "mov al, byte ptr [10]",
            "mov byte ptr [11], 'A'",
            "cmp byte ptr [si], 0",
            "stop",
        ]);
        Assert.Equal(0x1200, cpu.LeggiMemoria(10));
        Assert.Equal(0, cpu.AX & 0xFF);
        Assert.Equal((short)'A', cpu.LeggiMemoria(11));
        Assert.True(cpu.FlagZero);
    }

    [Fact]
    public async Task Ptr_PrecedenzaSulTipoDellaVariabile()
    {
        var cpu = await Esegui(
            ["mov al, byte ptr conta", "mov bx, word ptr car", "mov word ptr car, 1234h", "stop"],
            ["conta DW 0ABCDh", "car DB 'x'"]);
        Assert.Equal(0xCD, cpu.AX & 0xFF);
        Assert.Equal((short)'x', cpu.BX);
        Assert.Equal(0x1234, cpu.LeggiMemoria(1));
    }

    [Fact]
    public async Task JmpWordPtr_E_NomiByteWordComeVariabili()
    {
        var cpu = await Esegui(
            ["mov bx, offset tab", "jmp word ptr [bx]", "mov cx, 1", "qui: mov ax, word", "stop"],
            ["tab DW qui", "word DW 7"]);
        Assert.Equal(0, cpu.CX);
        Assert.Equal(7, cpu.AX);
    }

    [Fact]
    public void Ptr_Errori()
    {
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(["mov al, word ptr [si]"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(["mov ax, byte ptr [si]"]));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), Errore(["mov byte ptr [si], 300"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(["push byte ptr [1]"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(["jmp byte ptr [bx]"]));
        Assert.Equal(Errori.Msg(CodiceErrore.OperandoNonValido), Errore(["inc byte ptr ax"]));
        Assert.Equal(Errori.Msg(CodiceErrore.OperandoNonValido), Errore(["mov ax, word ptr 5"]));
    }
}
