using EasyCpu.Assembler.Processore;
using EasyCpu.Common;
using static EasyCpu.Assembler.Tests.IstruzioniTests;

namespace EasyCpu.Assembler.Tests;

// Carry flag, aritmetica senza segno (mul/div) e con segno (imul/idiv), salti senza segno,
// shift e rotazioni, loop, xchg, lea, cbw/cwd, ret n, disposizione x86 dei bit dei flag.
public class Fase3Tests
{
    [Fact]
    public async Task Carry_Add16_Add8()
    {
        var cpu = await Esegui(["mov ax, 0FFFFh", "add ax, 1", "stop"]);
        Assert.Equal(0, cpu.AX);
        Assert.True(cpu.FlagCarry);
        Assert.True(cpu.FlagZero);
        Assert.False(cpu.FlagOverflow);   // -1 + 1 non è un overflow con segno

        cpu = await Esegui(["mov ax, 0", "mov al, 200", "add al, 100", "stop"]);
        Assert.Equal(44, cpu.AX);         // 300 - 256; AH non cambia
        Assert.True(cpu.FlagCarry);
        Assert.False(cpu.FlagOverflow);
    }

    [Fact]
    public async Task Carry_SubCmpPrestito()
    {
        var cpu = await Esegui(["mov ax, 1", "sub ax, 2", "stop"]);
        Assert.Equal(-1, cpu.AX);
        Assert.True(cpu.FlagCarry);
        Assert.True(cpu.FlagSegno);

        cpu = await Esegui(["mov ax, 5", "cmp ax, 3", "stop"]);
        Assert.False(cpu.FlagCarry);
        Assert.Equal(5, cpu.AX);
    }

    [Fact]
    public async Task AdcSbb_32Bit()
    {
        // DX:AX = 0001:FFFF + 0000:0001 = 0002:0000
        var cpu = await Esegui(["mov dx, 1", "mov ax, 0FFFFh", "mov cx, 0", "mov bx, 1", "add ax, bx", "adc dx, cx", "stop"]);
        Assert.Equal((2, 0), (cpu.DX, cpu.AX));
        Assert.False(cpu.FlagCarry);

        // DX:AX = 0002:0000 - 0000:0001 = 0001:FFFF
        cpu = await Esegui(["mov dx, 2", "mov ax, 0", "sub ax, 1", "sbb dx, 0", "stop"]);
        Assert.Equal((1, -1), (cpu.DX, cpu.AX));
    }

    [Fact]
    public async Task ClcStcCmc_IncNonTocaCF()
    {
        Assert.True((await Esegui(["stc", "stop"])).FlagCarry);
        Assert.False((await Esegui(["stc", "cmc", "stop"])).FlagCarry);
        Assert.False((await Esegui(["stc", "clc", "stop"])).FlagCarry);
        Assert.True((await Esegui(["stc", "mov ax, 0FFFFh", "inc ax", "dec ax", "stop"])).FlagCarry);
    }

    [Fact]
    public async Task Neg_Carry()
    {
        Assert.False((await Esegui(["stc", "mov ax, 0", "neg ax", "stop"])).FlagCarry);
        Assert.True((await Esegui(["mov ax, 5", "neg ax", "stop"])).FlagCarry);
        var cpu = await Esegui(["mov ax, 0", "mov al, -128", "neg al", "stop"]);
        Assert.True(cpu.FlagOverflow);
    }

    // true se il salto viene eseguito dopo "cmp ax, b" con AX = a
    static async Task<bool> Salta(string salto, string a, string b)
    {
        var cpu = await Esegui(["mov bx, 0", "mov ax, " + a, "cmp ax, " + b, salto + " fine", "mov bx, 1", "fine: stop"]);
        return cpu.BX == 0;
    }

    [Theory]
    [InlineData("5", "3", "ja jae jnb jnc jnbe jnz jne jg jnle jnl jge", "jb jc jnae jbe jna jz je jng jnge jl jle")]
    [InlineData("3", "3", "jae jnb jnc jbe jna jz je jge jle jng jnl", "ja jnbe jb jc jnae jnz jne jg jl")]
    [InlineData("0FFFFh", "1", "ja jae jnbe jl jnge jle jng", "jb jc jbe jg jge")]
    [InlineData("1", "0FFFFh", "jb jc jnae jbe jna jg jge", "ja jae jnb jnc jl jle")]
    public async Task SaltiCondizionati_ConESenzaSegno(string a, string b, string eseguiti, string nonEseguiti)
    {
        foreach (var salto in eseguiti.Split(' '))
            Assert.True(await Salta(salto, a, b), $"{salto} con {a} cmp {b} doveva saltare");
        foreach (var salto in nonEseguiti.Split(' '))
            Assert.False(await Salta(salto, a, b), $"{salto} con {a} cmp {b} non doveva saltare");
    }

    [Fact]
    public async Task MulImul_8e16bit()
    {
        var cpu = await Esegui(["mov ax, 0FFh", "mov bl, 2", "mul bl", "stop"]);
        Assert.Equal(0x01FE, cpu.AX);                      // 255 * 2
        Assert.True(cpu.FlagCarry && cpu.FlagOverflow);

        cpu = await Esegui(["mov ax, 0FFh", "mov bl, 2", "imul bl", "stop"]);
        Assert.Equal(-2, cpu.AX);                          // -1 * 2
        Assert.False(cpu.FlagCarry || cpu.FlagOverflow);

        cpu = await Esegui(["mov ax, 0FFFFh", "mov bx, 0FFFFh", "mul bx", "stop"]);
        Assert.Equal((unchecked((short)0xFFFE), 1), (cpu.DX, cpu.AX));   // 65535 * 65535
        Assert.True(cpu.FlagCarry);

        cpu = await Esegui(["mov ax, 0FFFFh", "mov bx, 0FFFFh", "imul bx", "stop"]);
        Assert.Equal((0, 1), (cpu.DX, cpu.AX));           // -1 * -1
        Assert.False(cpu.FlagCarry);
    }

    [Fact]
    public async Task DivIdiv_8e16bit()
    {
        var cpu = await Esegui(["mov ax, 250", "mov bl, 0FEh", "div bl", "stop"]);
        Assert.Equal(0xFA00, (ushort)cpu.AX);              // 250 / 254 = 0 resto 250
        cpu = await Esegui(["mov ax, 250", "mov bl, 0FEh", "idiv bl", "stop"]);
        Assert.Equal(0x0083, cpu.AX);                      // 250 / -2 = -125 resto 0

        // DX:AX = 65536: div senza segno ok, idiv fuori intervallo
        cpu = await Esegui(["mov dx, 1", "mov ax, 0", "mov bx, 2", "div bx", "stop"]);
        Assert.Equal((0x8000, 0), ((ushort)cpu.AX, cpu.DX));
        var ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov dx, 1", "mov ax, 0", "mov bx, 2", "idiv bx", "stop"]));
        Assert.Equal(CodiceErrore.QuozienteFuoriIntervallo, ex.err);

        // idiv con cwd: -7 / 2 = -3 resto -1
        cpu = await Esegui(["mov ax, -7", "cwd", "mov bx, 2", "idiv bx", "stop"]);
        Assert.Equal((-3, -1), (cpu.AX, cpu.DX));
    }

    [Fact]
    public async Task CbwCwd()
    {
        Assert.Equal(-1, (await Esegui(["mov ax, 00FFh", "cbw", "stop"])).AX);
        Assert.Equal(0x7F, (await Esegui(["mov ax, 0A07Fh", "cbw", "stop"])).AX);
        Assert.Equal(-1, (await Esegui(["mov ax, -5", "cwd", "stop"])).DX);
        Assert.Equal(0, (await Esegui(["mov dx, 7", "mov ax, 5", "cwd", "stop"])).DX);
    }

    [Fact]
    public async Task ShrLogico_Sar_Shl()
    {
        var cpu = await Esegui(["mov ax, -8", "shr ax, 1", "mov bx, -8", "sar bx, 1", "mov cx, 5", "shr cx, 1", "stop"]);
        Assert.Equal(0x7FFC, cpu.AX);
        Assert.Equal(-4, cpu.BX);
        Assert.Equal(2, cpu.CX);
        Assert.True(cpu.FlagCarry);                        // bit uscito da 5 >> 1

        cpu = await Esegui(["mov ax, 0", "mov al, 80h", "shr al, 1", "mov bx, 0", "mov bl, 80h", "sar bl, 1", "stop"]);
        Assert.Equal(0x40, cpu.AX);
        Assert.Equal(0xC0, cpu.BX);

        cpu = await Esegui(["mov ax, 8000h", "shl ax, 1", "stop"]);
        Assert.Equal(0, cpu.AX);
        Assert.True(cpu.FlagCarry && cpu.FlagZero && cpu.FlagOverflow);

        Assert.True((await Esegui(["stc", "mov ax, 1", "shl ax, 0", "stop"])).FlagCarry);   // conteggio 0: flag invariati
    }

    [Fact]
    public async Task Rotazioni()
    {
        var cpu = await Esegui(["mov ax, 0", "mov al, 81h", "rol al, 1", "stop"]);
        Assert.Equal(0x03, cpu.AX);
        Assert.True(cpu.FlagCarry);

        cpu = await Esegui(["mov ax, 0", "mov al, 81h", "ror al, 1", "stop"]);
        Assert.Equal(0xC0, cpu.AX);
        Assert.True(cpu.FlagCarry);

        cpu = await Esegui(["clc", "mov ax, 0", "mov al, 80h", "rcl al, 1", "mov bx, ax", "rcl al, 1", "stop"]);
        Assert.Equal(0x00, cpu.BX);                         // 80h esce in CF
        Assert.Equal(0x01, cpu.AX);                         // CF rientra a destra
        Assert.False(cpu.FlagCarry);

        cpu = await Esegui(["stc", "mov ax, 0", "rcr al, 1", "stop"]);
        Assert.Equal(0x80, cpu.AX);
        Assert.False(cpu.FlagCarry);

        cpu = await Esegui(["mov ax, 1234h", "rol ax, 4", "mov bx, 1234h", "ror bx, 4", "stop"]);
        Assert.Equal(0x2341, cpu.AX);
        Assert.Equal(0x4123, cpu.BX);
    }

    [Fact]
    public async Task Test_NonModificaOperando()
    {
        var cpu = await Esegui(["mov ax, 5", "stc", "test ax, 4", "stop"]);
        Assert.Equal(5, cpu.AX);
        Assert.False(cpu.FlagZero);
        Assert.False(cpu.FlagCarry);
        Assert.True((await Esegui(["mov ax, 5", "test ax, 2", "stop"])).FlagZero);
    }

    [Fact]
    public async Task LoopFamiglia()
    {
        var cpu = await Esegui(["mov cx, 5", "mov ax, 0", "ciclo: add ax, 2", "loop ciclo", "stop"]);
        Assert.Equal((10, 0), (cpu.AX, cpu.CX));

        cpu = await Esegui(["mov cx, 5", "mov si, 0", "ciclo: inc si", "cmp si, 2", "loopne ciclo", "stop"]);
        Assert.Equal((2, 3), (cpu.SI, cpu.CX));

        cpu = await Esegui(["mov cx, 5", "mov si, 0", "ciclo: inc si", "cmp si, 3", "loopz ciclo", "stop"]);
        Assert.Equal((1, 4), (cpu.SI, cpu.CX));

        cpu = await Esegui(["mov ax, 0", "cmp ax, 0", "mov cx, 2", "l: loop l", "stop"]);
        Assert.True(cpu.FlagZero);                          // loop non modifica i flag
        Assert.Equal(0, cpu.CX);
    }

    [Fact]
    public async Task Xchg()
    {
        var cpu = await Esegui(["mov ax, 1", "mov bx, 2", "xchg ax, bx", "mov cx, 0102h", "xchg cl, ch", "mov dx, 9", "xchg dx, [10]", "stop"], ["10: 7"]);
        Assert.Equal((2, 1), (cpu.AX, cpu.BX));
        Assert.Equal(0x0201, cpu.CX);
        Assert.Equal((7, 9), (cpu.DX, cpu.LeggiMemoria(10)));
    }

    [Fact]
    public async Task Lea()
    {
        var cpu = await Esegui(["mov bx, 1", "lea si, [bx+2]", "lea di, vet+1", "lea ax, [10]", "lea cx, b", "stop"],
            ["vet DW 1, 2, 3", "b DB 5"]);
        Assert.Equal(3, cpu.SI);
        Assert.Equal(1, cpu.DI);
        Assert.Equal(10, cpu.AX);
        Assert.Equal(3, cpu.CX);
    }

    [Fact]
    public async Task RetN()
    {
        var cpu = await Esegui(["push 10", "push 20", "call somma", "stop", "somma: mov bp, sp", "mov ax, [bp+1]", "add ax, [bp+2]", "ret 2"]);
        Assert.Equal(30, cpu.AX);
        Assert.Equal(256, cpu.SP);

        var ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["call f", "stop", "f: ret 5"]));
        Assert.Equal(CodiceErrore.StackUnderflow, ex.err);
    }

    [Fact]
    public async Task BitFlag_ComeX86()
    {
        Assert.Equal(0x0001, (await Esegui(["stc", "pushf", "pop ax", "stop"])).AX);                   // CF bit 0
        Assert.Equal(0x0040, (await Esegui(["mov ax, 0", "cmp ax, 0", "pushf", "pop bx", "stop"])).BX); // ZF bit 6
        Assert.Equal(0x0880, (await Esegui(["mov ax, 32767", "add ax, 1", "pushf", "pop bx", "stop"])).BX); // SF bit 7, OF bit 11
        var cpu = await Esegui(["push 0801h", "popf", "stop"]);
        Assert.True(cpu.FlagOverflow && cpu.FlagCarry);
        Assert.False(cpu.FlagZero || cpu.FlagSegno);
    }

    [Fact]
    public void ErroriCompilazioneFase3()
    {
        string nonValido = Errori.Msg(CodiceErrore.OperandoNonValido);
        Assert.Equal(nonValido, ErroreCompilazione("xchg ax, 5"));
        Assert.Equal(nonValido, ErroreCompilazione("xchg [1], [2]"));
        Assert.Equal(Errori.Msg(CodiceErrore.DimensioneOperandi), ErroreCompilazione("xchg al, bx"));
        Assert.Equal(nonValido, ErroreCompilazione("lea al, [1]"));
        Assert.Equal(nonValido, ErroreCompilazione("lea ax, bx"));
        Assert.Equal(nonValido, ErroreCompilazione("lea ax, 5"));
        Assert.Equal(nonValido, ErroreCompilazione("ret ax"));
        Assert.Equal(nonValido, ErroreCompilazione("ret -1"));
        Assert.Equal(Errori.Msg(CodiceErrore.NumeroOperandi), ErroreCompilazione("cbw ax"));
        Assert.Null(ErroreCompilazione("rcr al, cl"));
        Assert.Null(ErroreCompilazione("ret 3"));
    }
}
