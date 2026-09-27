using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;
using static EasyCpu.Assembler.Tests.IstruzioniTests;

namespace EasyCpu.Assembler.Tests;

// Sezione dati simbolica (EQU, DB, DW, DUP, ORG, stringhe, offset) e servizi int 21h selezionati da AH.
public class Fase2Tests
{
    // Compila dati e codice e restituisce il primo messaggio d'errore (o null).
    static string? Errore(string[] dati, string[] codice)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        compiler.CompilaDati(dati.ToList(), ref errori);
        compiler.CompilaCodice(codice.ToList(), ref errori);
        return errori?.First().Msg;
    }

    static readonly string[] DatiBase =
    [
        "N       EQU 3",
        "conta   DW  7",                // 0
        "vet     DW  10, 20, 30",       // 1..3
        "msg     DB  'Ciao$'",          // 4..8
        "buf     DW  N DUP(9)",         // 9..11
        "        DB  -1",               // 12, senza nome
        "        ORG 100",
        "tab     DB  2 DUP(?)",         // 100..101
        "ptr     DW  offset vet",       // 102
    ];

    [Fact]
    public void TabellaSimboli_IndirizziETipi()
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati(DatiBase.ToList(), ref errori);
        Assert.Null(errori);

        var simboli = compiler.Simboli.ToDictionary(s => s.Nome);
        Assert.Equal(["n", "conta", "vet", "msg", "buf", "tab", "ptr"], compiler.Simboli.Select(s => s.Nome));
        Assert.Equal((TipoSimbolo.Equ, 3), (simboli["n"].Tipo, simboli["n"].Valore));
        Assert.Equal("N", simboli["n"].Grafia);
        Assert.Equal((0, 1), (simboli["conta"].Valore, simboli["conta"].Celle));
        Assert.Equal((1, 3), (simboli["vet"].Valore, simboli["vet"].Celle));
        Assert.Equal((TipoSimbolo.Db, 4, 5), (simboli["msg"].Tipo, simboli["msg"].Valore, simboli["msg"].Celle));
        Assert.Equal((9, 3), (simboli["buf"].Valore, simboli["buf"].Celle));
        Assert.Equal((100, 2), (simboli["tab"].Valore, simboli["tab"].Celle));

        Assert.Equal([7, 10, 20, 30, 'C', 'i', 'a', 'o', '$', 9, 9, 9, -1], memoria!.Take(13));
        Assert.Equal(1, memoria[102]);   // ptr = offset vet
    }

    [Fact]
    public async Task OperandiSimbolici_StileMasm()
    {
        var cpu = await Esegui(
        [
            "mov ax, conta",            // contenuto: 7
            "add ax, [conta]",          // 14
            "mov bx, vet+2",            // memoria[3] = 30
            "mov si, offset vet",       // 1
            "mov cx, [vet+si]",         // memoria[2] = 20
            "mov dx, N",                // costante 3
            "add dx, [bp+N]",           // bp = 0: memoria[3] = 30 -> 33
            "mov di, offset msg + 1",   // 5
            "mov [conta], 99",
            "stop",
        ], DatiBase);
        Assert.Equal(14, cpu.AX);
        Assert.Equal(30, cpu.BX);
        Assert.Equal(1, cpu.SI);
        Assert.Equal(20, cpu.CX);
        Assert.Equal(33, cpu.DX);
        Assert.Equal(5, cpu.DI);
        Assert.Equal(99, cpu.LeggiMemoria(0));
    }

    [Fact]
    public async Task VariabileDB_AccessiA8Bit()
    {
        var cpu = await Esegui(["mov al, b", "inc b", "mov bl, [b]", "mov si, 0", "mov cl, [msg+si+1]", "stop"],
            ["w DW 1234h", "b DB 250", "msg DB 'xy'"]);
        Assert.Equal(250, cpu.AX & 0xFF);
        Assert.Equal(251, cpu.LeggiMemoria(1));
        Assert.Equal(251, cpu.BX & 0xFF);
        Assert.Equal('y', cpu.CX & 0xFF);
    }

    [Fact]
    public void ErroriSezioneDati()
    {
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloDuplicato), Errore(["a DW 1", "a DB 2"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.NomeSimboloNonValido), Errore(["ax DW 1"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.NomeSimboloNonValido), Errore(["mov DW 1"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.DatiInAreaStack), Errore(["x DW 241 DUP(0)"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.DatiInAreaStack), Errore(["ORG 239", "y DW 1, 2"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.DatiInAreaStack), Errore(["ORG 240"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), Errore(["b DB 256"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), Errore(["b DB 'a€'"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.Formato), Errore(["w DW 'ab'"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.Formato), Errore(["s DB 'aperta"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloNonDefinito), Errore(["x DW M DUP(0)"], []));
        Assert.Equal(Errori.Msg(CodiceErrore.Sintassi), Errore(["x"], []));
        Assert.Null(Errore(["N EQU 2", "x DW N DUP(?), 'a'", "s DB 'Ciao', 13, 10, '$'", "0: 5"], []));
    }

    [Fact]
    public void ErroriCodiceConSimboli()
    {
        string[] dati = ["N EQU 5", "w DW 1", "b DB 1"];
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloNonDefinito), Errore(dati, ["mov ax, pippo"]));
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloNonDefinito), Errore(dati, ["mov si, offset pippo"]));
        Assert.Equal(Errori.Msg(CodiceErrore.Sintassi), Errore(dati, ["mov si, offset N"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(dati, ["mov ax, b"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(dati, ["mov al, w"]));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), Errore(dati, ["push b"]));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), Errore(dati, ["mov b, 300"]));
        Assert.Equal(Errori.Msg(CodiceErrore.AttesoRegistroIndiretto), Errore(dati, ["mov ax, [w+cx]"]));
        Assert.Equal(Errori.Msg(CodiceErrore.SimboloDuplicato), Errore(dati, ["w: nop"]));
        Assert.Equal(Errori.Msg(CodiceErrore.IntervalloIndirizzoDati), Errore(dati, ["mov ax, [w+300]"]));
        Assert.Null(Errore(dati, ["mov b, 200", "mov ax, N", "shl w, 1"]));
    }

    [Fact]
    public async Task Int21h_AH09_StampaStringaConCrLf()
    {
        string output = "";
        var cpu = await EseguiConConsole(["mov ah, 9", "mov dx, offset msg", "int 21h", "stop"],
            ["msg DB 'Ciao', 13, 10, 'mondo$'"], c => output += c);
        Assert.Equal("Ciao\nmondo", output);
        Assert.True(cpu.stop);
    }

    [Fact]
    public async Task Int21h_AH09_SenzaDollaro_ViolazioneMemoria()
    {
        var ex = await Assert.ThrowsAsync<CpuException>(() =>
            Esegui(["mov ah, 9", "mov dx, 250", "int 21h", "stop"]));
        Assert.Equal(CodiceErrore.ViolazioneMemoria, ex.err);
    }

    [Fact]
    public async Task Int21h_AH0A_LeggeRigaNelBuffer()
    {
        string output = "";
        var cpu = await EseguiConConsole(["mov ah, 0Ah", "mov dx, offset buf", "int 21h", "stop"],
            ["buf DB 5, ?, 5 DUP(?)"], c => output += c, "abcdefg\r");
        Assert.Equal(5, cpu.LeggiMemoria(0));      // massimo invariato
        Assert.Equal(4, cpu.LeggiMemoria(1));      // letti: massimo - 1
        Assert.Equal("abcd", string.Concat(Enumerable.Range(2, 4).Select(i => (char)cpu.LeggiMemoria(i))));
        Assert.Equal(13, cpu.LeggiMemoria(6));
        Assert.Equal("abcd\n", output);
    }

    [Fact]
    public async Task Int21h_AH0A_BackspaceCancellaUltimoCarattere()
    {
        string output = "";
        var cpu = await EseguiConConsole(["mov ah, 0Ah", "mov dx, offset buf", "int 21h", "stop"],
            ["buf DB 5, ?, 5 DUP(?)"], c => output += c, "\bab\bcd\b\b\bxyz\r");
        Assert.Equal(3, cpu.LeggiMemoria(1));      // Backspace a riga vuota ignorato
        Assert.Equal("xyz", string.Concat(Enumerable.Range(2, 3).Select(i => (char)cpu.LeggiMemoria(i))));
        Assert.Equal(13, cpu.LeggiMemoria(5));
        Assert.Equal("ab\bcd\b\b\bxyz\n", output);
    }

    [Fact]
    public async Task Int21h_AH02_UsaSoloDL_E_CrLfUnSoloACapo()
    {
        string output = "";
        await EseguiConConsole(
            ["mov ah, 2", "mov dx, 4141h", "int 21h", "mov dl, 13", "int 21h", "mov dl, 10", "int 21h", "mov dl, 10", "int 21h", "stop"],
            [], c => output += c);
        Assert.Equal("A\n\n", output);
    }

    [Fact]
    public async Task Int21h_AH4C_TerminaProgramma()
    {
        var cpu = await Esegui(["mov ah, 4Ch", "int 21h", "mov bx, 1", "stop"]);
        Assert.True(cpu.stop);
        Assert.Equal(0, cpu.BX);
    }

    [Fact]
    public async Task Int21h_ServizioNonValido_ancheVecchiaSintassiAX()
    {
        var ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ax, 2", "mov dx, 65", "int 21h", "stop"]));
        Assert.Equal(CodiceErrore.ServizioNonValido, ex.err);
        ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ah, 30h", "int 21h", "stop"]));
        Assert.Equal(CodiceErrore.ServizioNonValido, ex.err);
    }

    [Fact]
    public async Task DumpSimboli()
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati(["N EQU 3", "  Vet DW 10, 20 ; commento", "c DB 'A'"], ref errori);
        var codice = compiler.CompilaCodice(["stop"], ref errori);
        var cpu = new Cpu();
        cpu.Init(codice, memoria, true, 1000);
        Ambiente.FormatoDati = FormatoValore.Hex;
        try
        {
            Assert.Equal(["N          EQU 3", "Vet        [0000] DW x2 = 000A", "c          [0002] DB = 0041"],
                cpu.DumpSimboli(compiler.Simboli));
        }
        finally
        {
            Ambiente.FormatoDati = FormatoValore.Dec;
        }
    }

    static async Task<Cpu> EseguiConConsole(string[] codice, string[] dati, Action<char> console, string tasti = "")
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati(dati.ToList(), ref errori);
        var istruzioni = compiler.CompilaCodice(codice.ToList(), ref errori);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        cpu.ScriviSuConsole += console;
        foreach (char c in tasti)
            cpu.InviaCarattereTastiera((short)c);
        await cpu.Run();
        return cpu;
    }
}
