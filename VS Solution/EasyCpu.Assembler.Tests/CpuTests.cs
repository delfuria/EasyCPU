using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

public class CpuTests
{
    // Compila e inizializza una CPU pronta a eseguire il codice dato.
    // dati = null significa memoria dati tutta zero.
    static Cpu BuildCpu(string[] codeLines, int loopInfinito = 1000)
    {
        Ambiente.Inizializza();
        var compiler = new Compiler();
        var code = codeLines.ToList();
        List<CompilerError>? errori = null;
        var instructions = compiler.CompilaCodice(code, ref errori);
        Assert.NotNull(instructions);
        Assert.Null(errori);

        var memDati = new int[256].ToList();
        var cpu = new Cpu();
        cpu.Init(instructions, memDati, initRegs: true, loopInfinito);
        return cpu;
    }

    [Fact]
    public void StepInto_SequenzaSemplice_AX6()
    {
        // mov ax,5 / inc ax / stop → dopo 2 StepInto: AX==6
        var cpu = BuildCpu(new[] { "mov ax,5", "inc ax", "stop" });
        cpu.StepInto();  // esegue mov ax,5
        cpu.StepInto();  // esegue inc ax
        Assert.Equal(6, cpu.AX);
        Assert.False(cpu.stop);
    }

    [Fact]
    public void StepOver_Call_SPInvariatoEAX1()
    {
        // step_over_call: SP deve tornare uguale prima/dopo
        // mov ax,0 / call inc_ax / stop
        // inc_ax: inc ax / ret
        var lines = new[]
        {
            "mov ax,0",       // 0: istruzione 0
            "call inc_ax",    // 1: istruzione 1
            "stop",           // 2: istruzione 2
            "inc_ax: inc ax", // 3: istruzione 3 (con etichetta)
            "ret",            // 4: istruzione 4
        };
        var cpu = BuildCpu(lines);
        cpu.StepInto(); // esegue mov ax,0  →  ip punta a call

        short spPrimaCall = cpu.SP;
        cpu.StepOver(); // esegue call + tutta la subroutine + ret → ip punta a stop
        short spDopoCall = cpu.SP;

        Assert.Equal(1, cpu.AX);
        Assert.Equal(spPrimaCall, spDopoCall);
        Assert.False(cpu.stop); // stop non ancora eseguito
    }

    [Fact]
    public void StepOut_DentroSubroutine_SPAumentaDi1()
    {
        // Entra nella subroutine con StepInto sulla call, poi StepOut
        var lines = new[]
        {
            "mov ax,0",       // 0
            "call inc_ax",    // 1
            "stop",           // 2
            "inc_ax: inc ax", // 3
            "ret",            // 4
        };
        var cpu = BuildCpu(lines);
        cpu.StepInto(); // mov ax,0
        short spPrima = cpu.SP;

        cpu.StepInto(); // call → ip salta alla subroutine, sp--
        short spDentro = cpu.SP;
        Assert.Equal(spPrima - 1, spDentro); // sp è diminuito di 1

        cpu.StepOut(); // esegue fino al ret → sp torna a spPrima
        Assert.Equal(spPrima, cpu.SP);
    }

    [Fact]
    public void BreakpointMultipli_RunSiFermaAlPrimoPoiAlSecondo()
    {
        // mov ax,1 / inc ax / inc ax / stop
        // breakpoint su istruzione 1 (inc ax, riga 1) e istruzione 2 (inc ax, riga 2)
        var cpu = BuildCpu(new[] { "mov ax,1", "inc ax", "inc ax", "stop" });
        cpu.Breakpoints.Add(1); // ip=1 = secondo inc
        cpu.Breakpoints.Add(2); // ip=2 = terzo inc

        Assert.Throws<CpuTrapException>(() => cpu.Run());
        Assert.Equal(1, cpu.IP); // fermato PRIMA di eseguire ip=1
        Assert.Equal(1, cpu.AX); // solo mov eseguito

        cpu.StepInto(); // avanza oltre il breakpoint 1
        Assert.Throws<CpuTrapException>(() => cpu.Run());
        Assert.Equal(2, cpu.IP); // fermato PRIMA di ip=2
        Assert.Equal(2, cpu.AX); // inc ax del breakpoint 1 eseguito
    }

    [Fact]
    public void LineToInstrMap_RigheNonEseguibiliMappatoA_Meno1()
    {
        // Sorgente con riga vuota, commento e etichetta su riga propria
        var lines = new[]
        {
            "mov ax,5",           // riga 0 → istruzione 0
            "",                   // riga 1 → -1 (vuota)
            "// commento",        // riga 2 → -1 (solo commento)
            "fine:",              // riga 3 → -1 (solo etichetta)
            "inc ax",             // riga 4 → istruzione 1
            "stop",               // riga 5 → istruzione 2
        };
        Ambiente.Inizializza();
        var compiler = new Compiler();
        var code = lines.ToList();
        List<CompilerError>? errori = null;
        compiler.CompilaCodice(code, ref errori);

        var map = compiler.LineToInstrMap;
        Assert.NotNull(map);
        Assert.Equal(0, map[0]);  // mov ax,5
        Assert.Equal(-1, map[1]); // vuota
        Assert.Equal(-1, map[2]); // commento
        Assert.Equal(-1, map[3]); // solo etichetta
        Assert.Equal(1, map[4]);  // inc ax
        Assert.Equal(2, map[5]);  // stop
    }

    [Fact]
    public void DeterminismoIstanze_DueCpuNonCondivitonoStato()
    {
        // cpu1: mov ax,10 / stop
        // cpu2: mov ax,99 / stop
        // Entrambe eseguono in parallelo senza interferirsi
        var cpu1 = BuildCpu(new[] { "mov ax,10", "stop" });
        var cpu2 = BuildCpu(new[] { "mov ax,99", "stop" });

        var t1 = Task.Run(() => cpu1.Run());
        var t2 = Task.Run(() => cpu2.Run());
        Task.WaitAll(t1, t2);

        Assert.Equal(10, cpu1.AX);
        Assert.Equal(99, cpu2.AX);
    }
}
