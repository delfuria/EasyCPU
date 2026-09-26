using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Backend.Local;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

// Test di regressione: compila ed esegue i programmi di esempio in Docs/Subroutines
// e ne verifica lo stato finale.
public class EsempiTests
{
    static string CartellaEsempi()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidato = Path.Combine(dir.FullName, "Docs", "Subroutines");
            if (Directory.Exists(candidato))
                return candidato;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Docs/Subroutines non trovata");
    }

    static async Task<Cpu> Esegui(string nomeFile)
    {
        Ambiente.Inizializza();
        Storage.Apri(Path.Combine(CartellaEsempi(), nomeFile), out var codice, out var dati);

        var compiler = new Compiler();
        List<CompilerError>? errori = null;
        var memoria = compiler.CompilaDati(dati, ref errori);
        var istruzioni = compiler.CompilaCodice(codice, ref errori);
        Assert.Null(errori);
        Assert.NotNull(istruzioni);

        var cpu = new Cpu();
        cpu.Init(istruzioni, memoria, initRegs: true, 1000);
        await cpu.Run();
        Assert.True(cpu.stop);
        return cpu;
    }

    [Fact]
    public async Task Somma()
    {
        var cpu = await Esegui("somma.as");
        Assert.Equal(3, cpu.AX);
        Assert.Equal(2, cpu.BX);
    }

    [Fact]
    public async Task SommaDispari()
    {
        var cpu = await Esegui("somma dispari.as");
        Assert.Equal(1 + 3 + 7, cpu.BX);
        Assert.Equal(0, cpu.CX);
    }

    [Fact]
    public async Task SommaOProdotto()
    {
        var cpu = await Esegui("SommaOProdotto.as");
        Assert.Equal(5, cpu.AX);
        Assert.Equal(5, cpu.LeggiMemoria(15));
    }

    [Fact]
    public async Task SubSommaVettore()
    {
        var cpu = await Esegui("Sub Somma vettore.as");
        Assert.Equal(2 + 3 + 4 + 1, cpu.AX);
        Assert.Equal(256, cpu.SP);
    }

    [Fact]
    public async Task SubSomma()
    {
        var cpu = await Esegui("Sub Somma.as");
        Assert.Equal(4 + 5 + 11 + 12, cpu.AX);
    }

    [Fact]
    public async Task UsoSubroutine()
    {
        var cpu = await Esegui("Uso Subroutine .as");
        Assert.Equal(10, cpu.BX);
        Assert.Equal(256, cpu.SP);
    }

    [Fact]
    public async Task LoopInfinito()
    {
        await Assert.ThrowsAsync<CpuLoopException>(() => Esegui("loopinfinito.as"));
    }
}
