using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Backend.Serializers;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Tests;

// TEMPORANEO: confronto dei risultati di tutti gli esempi prima/dopo un refactoring
public class TmpCampioniTests
{
    [Fact]
    public async Task CompilaTutti()
    {
        var outFile = Environment.GetEnvironmentVariable("CAMPIONI_OUT");
        if (outFile == null) return;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!Directory.Exists(Path.Combine(dir!.FullName, "Docs", "samples"))) dir = dir.Parent;
        var righe = new List<string>();
        foreach (var f in Directory.GetFiles(Path.Combine(dir.FullName, "Docs", "samples"), "*.asj", SearchOption.AllDirectories).OrderBy(x => x))
        {
            Ambiente.Inizializza();
            using var fs = File.OpenRead(f);
            var (c0, d0) = await ISourceSerializer.ForPath(f).LoadAsync(fs);
            var codice = c0.ToList(); var dati = d0.ToList();
            var compiler = new Compiler();
            List<CompilerError>? errori = null;
            var memoria = compiler.CompilaDati(dati, ref errori, codice);
            var istr = compiler.CompilaCodice(codice, ref errori);
            string stato = errori == null ? "OK" : "ERR " + string.Join(" | ", errori.OrderBy(e => e.Riga).Select(e => $"{e.Tipo}:{e.Riga}:{e.Msg}"));
            if (errori == null)
            {
                var cpu = new Cpu();
                cpu.Init(istr, memoria, true, 1000);
                string console = "";
                cpu.ScriviSuConsole += ch => console += ch;
                foreach (char k in "1x\r") cpu.InviaCarattereTastiera((short)k);
                try { var t = cpu.Run(); if (await Task.WhenAny(t, Task.Delay(2000)) == t) await t; else { cpu.Stop(); stato += " (timeout)"; } }
                catch (CpuException e) { stato += " RUNERR " + e.err; }
                catch (Exception e) { stato += " EXC " + e.GetType().Name; }
                stato += $" AX={cpu.AX} BX={cpu.BX} CX={cpu.CX} DX={cpu.DX} SI={cpu.SI} DI={cpu.DI} SP={cpu.SP} IP={cpu.IP}";
                stato += " MEM=" + string.Join(",", cpu.DumpMemoria(0, 256, 16) ?? []);
                stato += " SIM=" + string.Join(";", cpu.DumpSimboli(compiler.Simboli));
                stato += " CON=" + console.Replace("\n", "\\n");
            }
            righe.Add(Path.GetFileName(f) + ": " + stato);
        }
        File.WriteAllLines(outFile, righe);
    }
}
