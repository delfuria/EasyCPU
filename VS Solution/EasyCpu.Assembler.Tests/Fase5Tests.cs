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

    static async Task<Cpu> Esegui(string[] codice, string[]? dati = null, ModelloMemoria? modello = null)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler { Modello = modello ?? ModelloMemoria.Parole };
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati((dati ?? []).ToList(), ref errori, codice.ToList());
        var istruzioni = compiler.CompilaCodice(codice.ToList(), ref errori);
        Assert.Null(errori);
        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000, compiler.Modello);
        await cpu.Run();
        return cpu;
    }

    static string? Errore(string[] codice, string[]? dati = null, ModelloMemoria? modello = null)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler { Modello = modello ?? ModelloMemoria.Parole };
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

    // ── Passo 4: memoria a byte ───────────────────────────────────────────

    static readonly ModelloMemoria B = ModelloMemoria.Byte;

    [Fact]
    public void ModelloByte_512ByteStack64()
    {
        Assert.Equal((512, 448, 2), (B.Dimensione, B.InizioStack, B.PassoParola));
        Assert.Equal("byte", B.NomeFile);
        Assert.Null(ModelloMemoria.Parole.NomeFile);
        Assert.Same(B, ModelloMemoria.DaNomeFile("byte"));
        Assert.Same(ModelloMemoria.Parole, ModelloMemoria.DaNomeFile(null));
    }

    [Fact]
    public void MemoriaAByte_LittleEndianELimiti()
    {
        var mem = MemoriaCpu.Crea(B);
        mem.ScriviParola(10, 0x1234);
        Assert.Equal((0x34, 0x12), (mem.LeggiByte(10), mem.LeggiByte(11)));
        mem.ScriviByte(11, -1);
        Assert.Equal(unchecked((short)0xFF34), mem.LeggiParola(10));
        Assert.Equal(-1, mem.LeggiByte(11));                            // estensione del segno
        mem.ScriviParola(510, 7);                                       // ultima parola intera
        Assert.Equal(CodiceErrore.ViolazioneMemoria, Assert.Throws<CpuException>(() => mem.LeggiParola(511)).err);
        Assert.Equal(CodiceErrore.ViolazioneMemoria, Assert.Throws<CpuException>(() => mem.ScriviByte(512, 0)).err);
    }

    [Fact]
    public async Task ModalitaByte_ParoleSovrapposte()
    {
        var cpu = await Esegui(
            ["mov word ptr [10], 1234h", "mov ax, [10]", "mov bl, [11]", "mov cx, [11]", "stop"], modello: B);
        Assert.Equal(0x1234, cpu.AX);
        Assert.Equal(0x12, cpu.BX);
        Assert.Equal(0x0012, cpu.CX);                                   // byte 11 e 12
        Assert.Equal(0x34, cpu.LeggiByte(10));
    }

    [Fact]
    public async Task ModalitaByte_AllocazioneDati()
    {
        Ambiente.Inizializza();
        var compiler = new Compiler { Modello = B };
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati(["a DB 1, 2", "b DW 1234h", "s DB 'Ok'", "v DW 2 DUP(-1)", "ORG 440", "fine DW 5"], ref errori);
        Assert.Null(errori);
        var simboli = compiler.Simboli.ToDictionary(x => x.Nome, x => x.Valore);
        Assert.Equal((0, 2, 4, 6, 440), (simboli["a"], simboli["b"], simboli["s"], simboli["v"], simboli["fine"]));
        Assert.Equal([1, 2, 0x34, 0x12, 'O', 'k', 0xFF, 0xFF, 0xFF, 0xFF], memoria!.Take(10));
        Assert.Equal(512, memoria!.Count);
        var cpu = await Esegui(["mov ax, b", "mov bl, s+1", "stop"], ["a DB 1, 2", "b DW 1234h", "s DB 'Ok'"], B);
        Assert.Equal(0x1234, cpu.AX);
        Assert.Equal((short)'k', cpu.BX);
    }

    [Fact]
    public void ModalitaByte_DatiNellAreaStack()
    {
        Assert.Null(Errore(["stop"], ["v DW 224 DUP(0)"], B));          // 448 byte: fino a 447
        Assert.Equal(Errori.Msg(CodiceErrore.DatiInAreaStack), Errore(["stop"], ["v DW 224 DUP(0)", "c DB 1"], B));
        Assert.Equal(Errori.Msg(CodiceErrore.DatiInAreaStack), Errore(["stop"], ["ORG 448"], B));
    }

    [Fact]
    public void ModalitaByte_ErroriDiCompilazione()
    {
        string dim = Errori.Msg(CodiceErrore.DimensioneNonSpecificata);
        string memMem = Errori.Msg(CodiceErrore.OperandiMemoriaMemoria);
        Assert.Equal(dim, Errore(["inc [si]"], modello: B));
        Assert.Equal(dim, Errore(["mov [di], 5"], modello: B));
        Assert.Equal(dim, Errore(["mul [bx]"], modello: B));
        Assert.Equal(dim, Errore(["shl [10], 1"], modello: B));
        Assert.Equal(memMem, Errore(["mov a, b"], ["a DW 1", "b DW 2"], B));
        Assert.Equal(memMem, Errore(["add [1], [si]"], modello: B));
        Assert.Equal(memMem, Errore(["mov byte ptr [1], byte ptr [2]"], modello: B));
        Assert.Equal(Errori.Msg(CodiceErrore.IndirizzoValoriInModalitaByte), Errore(["stop"], ["10: 1, 2"], B));
        // ammessi: dimensione determinata, stack, salti indiretti, lea, istruzioni stringa
        Assert.Null(Errore(
            ["inc conta", "inc byte ptr [si]", "mov [di], ax", "shl word ptr [10], 1", "push [10]", "pop [12]",
             "lea si, [bx+2]", "rep movsb", "jmp [tab]", "fine: stop"],
            ["conta DW 0", "tab DW fine"], B));
    }

    [Fact]
    public void ModalitaParole_RegoleInvariate()
    {
        Assert.Null(Errore(["inc [si]", "mov [di], 5", "add [1], [si]", "mov a, b", "stop"], ["a DW 1", "b DW 2", "10: 1, 2"]));
    }

    [Fact]
    public async Task FileAsj_CampoMemoria()
    {
        var ser = new EasyCpu.Backend.Serializers.EasyFileSerializer();
        foreach (var memoria in new[] { "byte", null })
        {
            var ms = new MemoryStream();
            await ser.SaveAsync(ms, ["stop"], ["a DB 1"], memoria);
            string json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            Assert.Equal(memoria != null, json.Contains("\"memoria\""));
            var (code, data, letta) = await ser.LoadAsync(new MemoryStream(ms.ToArray()));
            Assert.Equal(["stop"], code);
            Assert.Equal(["a DB 1"], data);
            Assert.Equal(memoria, letta);
        }
    }
}
