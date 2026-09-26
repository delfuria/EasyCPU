using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

// Comportamento delle istruzioni a 16 bit e dei modi di indirizzamento.
public class IstruzioniTests
{
    internal static async Task<Cpu> Esegui(string[] codice, string[]? dati = null)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati((dati ?? []).ToList(), ref errori);
        var istruzioni = compiler.CompilaCodice(codice.ToList(), ref errori);
        Assert.Null(errori);
        Assert.NotNull(istruzioni);

        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        await cpu.Run();
        return cpu;
    }

    // Compila una sola riga di codice e restituisce l'eventuale messaggio d'errore.
    internal static string? ErroreCompilazione(string riga)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        compiler.CompilaCodice([riga], ref errori);
        return errori?.Single().Msg;
    }

    [Fact]
    public async Task ModiIndirizzamento()
    {
        var cpu = await Esegui(
        [
            "mov si, 10", "mov di, 11", "mov bx, 12", "mov bp, 10",
            "mov ax, [si]",         // 100
            "add ax, [di]",         // +200
            "add ax, [bx-1]",       // +200
            "add ax, [bp+2]",       // +300
            "add ax, [13]",         // +400
            "add ax, 0Ah",          // +10
            "add ax, 'A'",          // +65
            "mov [si+5], ax",
            "stop",
        ], ["10: 100, 200, 300, 400"]);
        Assert.Equal(1275, cpu.AX);
        Assert.Equal(1275, cpu.LeggiMemoria(15));
    }

    [Fact]
    public async Task Flag_AddOverflow()
    {
        var cpu = await Esegui(["mov ax, 32767", "add ax, 1", "stop"]);
        Assert.Equal(short.MinValue, cpu.AX);
        Assert.True(cpu.FlagOverflow);
        Assert.True(cpu.FlagSegno);
        Assert.False(cpu.FlagZero);
    }

    [Fact]
    public async Task Flag_CmpUguali()
    {
        var cpu = await Esegui(["mov ax, 5", "cmp ax, 5", "stop"]);
        Assert.True(cpu.FlagZero);
        Assert.False(cpu.FlagSegno);
        Assert.Equal(5, cpu.AX);
    }

    [Fact]
    public async Task LogicheEShift()
    {
        var cpu = await Esegui(
        [
            "mov ax, 0F0h", "and ax, 3Ch",  // 30h
            "or ax, 1",                     // 31h
            "xor ax, 3",                    // 32h
            "shl ax, 2",                    // C8h
            "mov bx, ax", "shr bx, 3",      // 19h
            "mov cx, 5", "not cx",          // -6
            "mov dx, 7", "neg dx",          // -7
            "stop",
        ]);
        Assert.Equal(0xC8, cpu.AX);
        Assert.Equal(0x19, cpu.BX);
        Assert.Equal(-6, cpu.CX);
        Assert.Equal(-7, cpu.DX);
    }

    [Fact]
    public async Task MulDiv16()
    {
        var cpu = await Esegui(["mov ax, 300", "mov bx, 300", "mul bx", "mov cx, ax", "mov si, dx", "mov ax, 47", "mov dx, 0", "div 5", "stop"]);
        Assert.Equal((short)(90000 & 0xFFFF), cpu.CX);
        Assert.Equal(90000 >> 16, cpu.SI);
        Assert.Equal(9, cpu.AX);
        Assert.Equal(2, cpu.DX);
    }

    [Fact]
    public async Task StackEFlag()
    {
        var cpu = await Esegui(["mov ax, 0", "cmp ax, 0", "pushf", "mov bx, 1", "cmp bx, 0", "popf", "push 42", "pop cx", "stop"]);
        Assert.True(cpu.FlagZero);
        Assert.Equal(42, cpu.CX);
        Assert.Equal(256, cpu.SP);
    }

    [Fact]
    public async Task Movs()
    {
        var cpu = await Esegui(["mov si, 1", "mov di, 2", "movs", "mov ax, [2]", "stop"], ["1: 77"]);
        Assert.Equal(77, cpu.AX);
    }

    [Fact]
    public void Errori16()
    {
        Assert.Equal(Errori.Msg(CodiceErrore.DestinazioneCostante), ErroreCompilazione("mov 5, ax"));
        Assert.Equal(Errori.Msg(CodiceErrore.DestinazioneCostante), ErroreCompilazione("inc 5"));
        Assert.Equal(Errori.Msg(CodiceErrore.IntervalloIndirizzoDati), ErroreCompilazione("mov ax, [256]"));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), ErroreCompilazione("mov ax, 40000"));
        Assert.Null(ErroreCompilazione("mov ax, 0FFFFh"));
    }
}
