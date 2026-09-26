using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;
using static EasyCpu.Assembler.Tests.IstruzioniTests;

namespace EasyCpu.Assembler.Tests;

// Indirizzamento base + indice, istruzioni stringa, flag DF e prefissi REP.
public class Fase4Tests
{
    [Fact]
    public async Task BaseIndice()
    {
        var cpu = await Esegui(
        [
            "mov bx, 1", "mov si, 2",
            "mov ax, [bx+si]",          // memoria[3] = 40
            "mov cx, [vet+bx+si-1]",    // memoria[2] = 30
            "mov bp, 0", "mov di, 4",
            "mov dx, [bp+di]",          // memoria[4] = 50
            "mov [si+bx+1], 99",        // ordine indifferente: memoria[4] = 99
            "lea di, [bx+si+1]",        // 4
            "stop",
        ], ["vet DW 10, 20, 30, 40, 50"]);
        Assert.Equal(40, cpu.AX);
        Assert.Equal(30, cpu.CX);
        Assert.Equal(50, cpu.DX);
        Assert.Equal(99, cpu.LeggiMemoria(4));
        Assert.Equal(4, cpu.DI);
    }

    [Fact]
    public void BaseIndice_Errori()
    {
        string combinazione = Errori.Msg(CodiceErrore.CombinazioneRegistriNonValida);
        Assert.Equal(combinazione, ErroreCompilazione("mov ax, [si+di]"));
        Assert.Equal(combinazione, ErroreCompilazione("mov ax, [bx+bp]"));
        Assert.Equal(Errori.Msg(CodiceErrore.Sintassi), ErroreCompilazione("mov ax, [bx-si]"));
        Assert.Equal(Errori.Msg(CodiceErrore.Sintassi), ErroreCompilazione("mov ax, [bx+si+di]"));
        Assert.Equal(Errori.Msg(CodiceErrore.AttesoRegistroIndiretto), ErroreCompilazione("mov ax, [ax+si]"));
        Assert.Null(ErroreCompilazione("mov ax, [di+bp+2]"));
    }

    [Fact]
    public async Task Movs_AvanzaSIeDI_MovsbSoloByteBasso()
    {
        var cpu = await Esegui(["mov si, 0", "mov di, 5", "movsw", "movsb", "stop"], ["0: 1234h, 5678h", "5: 0, 0AAAAh"]);
        Assert.Equal(0x1234, cpu.LeggiMemoria(5));
        Assert.Equal(unchecked((short)0xAA78), cpu.LeggiMemoria(6));   // solo il byte basso copiato
        Assert.Equal((2, 7), (cpu.SI, cpu.DI));
    }

    [Fact]
    public async Task RepMovsw_AvantiEIndietro()
    {
        string[] dati = ["src DW 1, 2, 3, 4", "dst DW 4 DUP(0)"];
        var cpu = await Esegui(["cld", "mov si, offset src", "mov di, offset dst", "mov cx, 4", "rep movsw", "stop"], dati);
        Assert.Equal([1, 2, 3, 4], Enumerable.Range(4, 4).Select(i => (int)cpu.LeggiMemoria(i)));
        Assert.Equal((4, 8, 0), (cpu.SI, cpu.DI, cpu.CX));

        cpu = await Esegui(["std", "mov si, 3", "mov di, 7", "mov cx, 4", "rep movsw", "stop"], dati);
        Assert.Equal([1, 2, 3, 4], Enumerable.Range(4, 4).Select(i => (int)cpu.LeggiMemoria(i)));
        Assert.Equal((-1, 3), (cpu.SI, cpu.DI));
        Assert.True(cpu.FlagDirezione);
    }

    [Fact]
    public async Task LodsbStosb_Maiuscolo()
    {
        var cpu = await Esegui(["mov si, offset msg", "mov di, si", "mov cx, 3", "l: lodsb", "sub al, 32", "stosb", "loop l", "stop"],
            ["msg DB 'abc'"]);
        Assert.Equal("ABC", string.Concat(Enumerable.Range(0, 3).Select(i => (char)cpu.LeggiMemoria(i))));
    }

    [Fact]
    public async Task RepStosw_Riempie()
    {
        var cpu = await Esegui(["mov ax, 7", "mov di, 10", "mov cx, 5", "rep stosw", "stop"]);
        Assert.All(Enumerable.Range(10, 5), i => Assert.Equal(7, cpu.LeggiMemoria(i)));
        Assert.Equal(0, cpu.LeggiMemoria(15));
    }

    [Fact]
    public async Task RepneScasb_Ricerca()
    {
        string[] dati = ["msg DB 'hello$'"];
        var cpu = await Esegui(["mov di, offset msg", "mov al, 'l'", "mov cx, 6", "repne scasb", "stop"], dati);
        Assert.Equal((3, 3), (cpu.DI, cpu.CX));      // trovato in posizione 2: DI punta oltre
        Assert.True(cpu.FlagZero);

        cpu = await Esegui(["mov di, offset msg", "mov al, 'z'", "mov cx, 6", "repne scasb", "stop"], dati);
        Assert.Equal(0, cpu.CX);
        Assert.False(cpu.FlagZero);
    }

    [Fact]
    public async Task RepeCmpsb_ConfrontoStringhe()
    {
        string[] dati = ["a DB 'abcd'", "b DB 'abXd'", "c DB 'abcd'"];
        var cpu = await Esegui(["mov si, offset a", "mov di, offset b", "mov cx, 4", "repe cmpsb", "stop"], dati);
        Assert.Equal((1, 3), (cpu.CX, cpu.SI));      // si ferma dopo la differenza in posizione 2
        Assert.False(cpu.FlagZero);

        cpu = await Esegui(["mov si, offset a", "mov di, offset c", "mov cx, 4", "rep cmpsb", "stop"], dati);   // rep = repe
        Assert.Equal(0, cpu.CX);
        Assert.True(cpu.FlagZero);
    }

    [Fact]
    public async Task Rep_ConCXZero_NonEsegue()
    {
        var cpu = await Esegui(["mov cx, 0", "mov si, 5", "mov di, 8", "rep movsw", "stop"]);
        Assert.Equal((5, 8), (cpu.SI, cpu.DI));
    }

    static Cpu Prepara(string[] codice)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati([], ref errori);
        var istruzioni = compiler.CompilaCodice(codice.ToList(), ref errori);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, true, 1000);
        return cpu;
    }

    [Fact]
    public async Task Rep_UnaRipetizionePerStep()
    {
        var cpu = Prepara(["mov cx, 3", "rep stosw", "stop"]);
        await cpu.StepInto();                       // mov cx, 3
        await cpu.StepInto();
        Assert.Equal((1, 2), (cpu.IP, cpu.CX));    // IP resta su rep
        await cpu.StepInto();
        Assert.Equal((1, 1), (cpu.IP, cpu.CX));
        await cpu.StepInto();
        Assert.Equal((2, 0), (cpu.IP, cpu.CX));    // ultima ripetizione: IP avanza
    }

    [Fact]
    public async Task Rep_BreakpointSoloAllArrivo()
    {
        var cpu = Prepara(["mov cx, 3", "mov si, 0", "mov di, 10", "rep movsw", "stop"]);
        cpu.Breakpoints.Add(3);
        await Assert.ThrowsAsync<CpuTrapException>(() => cpu.Run());
        Assert.Equal((3, 3), (cpu.IP, cpu.CX));
        await cpu.StepInto();                       // come l'IDE: supera il breakpoint e riprende
        await cpu.Run();
        Assert.True(cpu.stop);
        Assert.Equal(0, cpu.CX);
    }

    [Fact]
    public async Task FlagDirezione_Bit10()
    {
        var cpu = await Esegui(["std", "pushf", "pop ax", "cld", "stop"]);
        Assert.Equal(0x0400, cpu.AX);
        Assert.False(cpu.FlagDirezione);
    }

    [Fact]
    public void Prefissi_Errori()
    {
        string prefisso = Errori.Msg(CodiceErrore.PrefissoNonValido);
        Assert.Equal(prefisso, ErroreCompilazione("rep mov ax, bx"));
        Assert.Equal(prefisso, ErroreCompilazione("repe movsb"));
        Assert.Equal(prefisso, ErroreCompilazione("repnz stosw"));
        Assert.Equal(prefisso, ErroreCompilazione("rep"));
        Assert.Equal(Errori.Msg(CodiceErrore.NumeroOperandi), ErroreCompilazione("rep movsb ax"));
        Assert.Null(ErroreCompilazione("inizio: repne scasw"));
        Assert.Null(ErroreCompilazione("repz cmpsw"));
        Assert.Null(ErroreCompilazione("rep lodsb"));
    }
}
