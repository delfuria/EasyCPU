using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

// Proposta A: jmp/call con registro o memoria, etichette usate come valori.
public class SaltiIndirettiTests
{
    // Compila come l'IDE: le etichette del codice sono note già alla sezione dati.
    static (Compiler compiler, List<Instruction>? istruzioni, List<int>? memoria, List<CompilerError>? errori)
        Compila(string[] codice, string[]? dati = null)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati((dati ?? []).ToList(), ref errori, codice.ToList());
        var istruzioni = compiler.CompilaCodice(codice.ToList(), ref errori);
        return (compiler, istruzioni, memoria, errori);
    }

    static async Task<Cpu> Esegui(string[] codice, string[]? dati = null)
    {
        var (_, istruzioni, memoria, errori) = Compila(codice, dati);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        await cpu.Run();
        return cpu;
    }

    static string? Errore(string[] codice, string[]? dati = null) => Compila(codice, dati).errori?.First().Msg;

    [Fact]
    public void PreScansione_IndiciEtichette()
    {
        var (compiler, _, _, errori) = Compila(
        [
            "// commento",
            "inizio: mov ax, 1",
            "",
            "sola:",
            "        rep movsb      ; prefisso",
            "dopo:   nop",
            "fine:",
        ]);
        Assert.Null(errori);
        var etichette = compiler.Simboli.Where(s => s.Tipo == TipoSimbolo.Etichetta).ToDictionary(s => s.Nome, s => s.Valore);
        Assert.Equal(new Dictionary<string, int> { ["inizio"] = 0, ["sola"] = 1, ["dopo"] = 2, ["fine"] = 3 }, etichette);
    }

    [Fact]
    public async Task JmpRegistro_OffsetEtichettaInAvanti()
    {
        var cpu = await Esegui(["mov bx, offset fine", "jmp bx", "mov ax, 1", "fine: mov cx, 2", "stop"]);
        Assert.Equal(0, cpu.AX);
        Assert.Equal(2, cpu.CX);
    }

    [Fact]
    public async Task JmpTabella_EtichetteNellaSezioneDati()
    {
        string[] codice =
        [
            "        mov bx, 2",
            "        jmp [tab+bx]",
            "caso0:  mov ax, 10",
            "        jmp fine",
            "caso1:  mov ax, 11",
            "        jmp fine",
            "caso2:  mov ax, 12",
            "fine:   stop",
        ];
        var cpu = await Esegui(codice, ["tab DW caso0, caso1, caso2"]);
        Assert.Equal(12, cpu.AX);
        Assert.Equal([2, 4, 6], Enumerable.Range(0, 3).Select(i => (int)cpu.LeggiMemoria(i)));
    }

    [Fact]
    public async Task JmpVariabile_StileMasm()
    {
        var cpu = await Esegui(["jmp dest", "mov ax, 1", "qui: mov bx, 5", "stop"], ["dest DW qui"]);
        Assert.Equal(0, cpu.AX);
        Assert.Equal(5, cpu.BX);
    }

    [Fact]
    public async Task CallTabella_RitornoESpRipristinato()
    {
        string[] codice =
        [
            "        mov si, 1",
            "        mov ax, 7",
            "        call [proc+si]",
            "        mov cx, 1",
            "        stop",
            "doppio: add ax, ax",
            "        ret",
            "triplo: mov bx, ax",
            "        add ax, bx",
            "        add ax, bx",
            "        ret",
        ];
        var cpu = await Esegui(codice, ["proc DW doppio, triplo"]);
        Assert.Equal(21, cpu.AX);
        Assert.Equal(1, cpu.CX);
        Assert.Equal(256, cpu.SP);
    }

    [Fact]
    public async Task DestinazioneFuoriProgramma_IPNonValido()
    {
        var ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ax, 50", "jmp ax", "stop"]));
        Assert.Equal(CodiceErrore.IPNonValido, ex.err);
    }

    [Fact]
    public async Task StepOver_CallIndiretta_EseguePiuProcedura()
    {
        var (_, istruzioni, memoria, errori) = Compila(["mov bx, offset proc", "call bx", "stop", "proc: mov ax, 3", "ret"]);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        await cpu.StepInto();
        await cpu.StepOver();
        Assert.Equal(3, cpu.AX);
        Assert.Equal(2, cpu.IP);
    }

    [Fact]
    public void ErroriDiCompilazione()
    {
        Assert.Equal(Errori.Msg(CodiceErrore.EtichettaSenzaOffset), Errore(["mov bx, fine", "fine: stop"]));
        Assert.Equal(Errori.Msg(CodiceErrore.OperandoNonValido), Errore(["jmp 5"]));
        Assert.Equal(Errori.Msg(CodiceErrore.OperandoNonValido), Errore(["jmp offset fine", "fine: stop"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(["jmp al"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(["jmp b"], ["b DB 1"]));
        Assert.Equal(Errori.Msg(CodiceErrore.EtichettaNonValida), Errore(["jmp manca"]));
        Assert.Equal(Errori.Msg(CodiceErrore.EtichettaNonValida), Errore(["je tab"], ["tab DW 0"]));
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloDuplicato), Errore(["uno: nop", "uno: stop"]));
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloDuplicato), Errore(["conta: stop"], ["conta DW 0"]));
    }

    [Fact]
    public void DumpSimboli_EtichetteDopoIDati()
    {
        var (compiler, istruzioni, memoria, errori) = Compila(["inizio: nop", "fine: stop"], ["N EQU 3"]);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        Assert.Equal(["N          EQU 3", "inizio     ETICHETTA 0", "fine       ETICHETTA 1"], cpu.DumpSimboli(compiler.Simboli));
    }
}
