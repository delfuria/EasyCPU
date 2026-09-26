using EasyCpu.Assembler.Processore;
using EasyCpu.Common;
using static EasyCpu.Assembler.Tests.IstruzioniTests;

// Ambiente è statico (formato dei dump): i test non devono girare in parallelo.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace EasyCpu.Assembler.Tests;

// Registri a 8 bit (AL..DH) come viste su AX..DX, memoria a celle da 16 bit, errori di divisione.
public class Registri8BitTests
{
    [Fact]
    public async Task MovAhAl_ComponeAx()
    {
        var cpu = await Esegui(["mov ax, 0", "mov ah, 1", "mov al, 41h", "stop"]);
        Assert.Equal(0x0141, cpu.AX);
    }

    [Fact]
    public async Task ScritturaByte_NonToccaAltroByte()
    {
        var cpu = await Esegui(
        [
            "mov bx, 1234h", "mov bl, 0FFh",    // 12FF
            "mov cx, 1234h", "mov ch, 0",       // 0034
            "mov dx, 0", "mov dh, 'A'", "mov dl, dh",
            "stop",
        ]);
        Assert.Equal(0x12FF, cpu.BX);
        Assert.Equal(0x0034, cpu.CX);
        Assert.Equal(0x4141, cpu.DX);
    }

    [Fact]
    public async Task LetturaByte_InRegistro16ConCmp()
    {
        // al = -1 (FFh): il confronto con -1 a 8 bit è uguale
        var cpu = await Esegui(["mov ax, 00FFh", "cmp al, -1", "stop"]);
        Assert.True(cpu.FlagZero);
        Assert.False(cpu.FlagSegno);
    }

    [Fact]
    public async Task Add8_Riporto_NonToccaAh()
    {
        // 0FFh + 1 = 00h a 8 bit: AH resta 7, ZF=1, nessun overflow (-1 + 1)
        var cpu = await Esegui(["mov ax, 07FFh", "add al, 1", "stop"]);
        Assert.Equal(0x0700, cpu.AX);
        Assert.True(cpu.FlagZero);
        Assert.False(cpu.FlagOverflow);
    }

    [Fact]
    public async Task Add8_Overflow()
    {
        // 127 + 1 = -128 a 8 bit: OF=1, SF=1
        var cpu = await Esegui(["mov ax, 0", "mov al, 127", "add al, 1", "stop"]);
        Assert.Equal(0x0080, cpu.AX);
        Assert.True(cpu.FlagOverflow);
        Assert.True(cpu.FlagSegno);
        Assert.False(cpu.FlagZero);
    }

    [Fact]
    public async Task Sub8_CostanteSenzaSegno()
    {
        // 200 come byte vale -56: 10 - (-56) = 66, nessun overflow
        var cpu = await Esegui(["mov ax, 0", "mov al, 10", "sub al, 200", "stop"]);
        Assert.Equal(66, cpu.AX);
        Assert.False(cpu.FlagOverflow);
    }

    [Fact]
    public async Task IncDecNegNot8()
    {
        var cpu = await Esegui(
        [
            "mov ax, 0", "mov al, 7Fh", "inc al",   // 80h, OF
            "mov bx, 0100h", "dec bl",              // 01FFh
            "mov cx, 0", "mov cl, 5", "neg cl",     // 00FBh
            "mov dx, 0", "not dh",                  // FF00h
            "stop",
        ]);
        Assert.Equal(0x0080, cpu.AX);
        Assert.Equal(0x01FF, cpu.BX);
        Assert.Equal(0x00FB, cpu.CX);
        Assert.Equal(unchecked((short)0xFF00), cpu.DX);
    }

    [Fact]
    public async Task LogicheEShift8()
    {
        var cpu = await Esegui(["mov ax, 0AB0Fh", "and al, 3", "or ah, 0F0h", "mov bx, 0", "mov bl, 3", "shl bl, 6", "mov cx, 1", "shl ax, cl", "stop"]);
        Assert.Equal(unchecked((short)(0xFB03 << 1)), cpu.AX);
        Assert.Equal(0x00C0, cpu.BX);
    }

    [Fact]
    public async Task Memoria8_ScriveSoloByteBasso()
    {
        var cpu = await Esegui(["mov si, 10", "mov al, 'Z'", "mov [si], al", "mov [11], al", "mov bh, [12]", "stop"], ["10: 1234h, 0, -1"]);
        Assert.Equal(0x125A, cpu.LeggiMemoria(10));
        Assert.Equal(0x005A, cpu.LeggiMemoria(11));
        Assert.Equal(unchecked((short)0xFF00), cpu.BX);
    }

    [Fact]
    public async Task Mul8()
    {
        // AX = AL * BL, con segno
        var cpu = await Esegui(["mov ax, 7F10h", "mov bx, 0", "mov bl, 20", "mul bl", "mov cx, ax", "mov al, -3", "mul bl", "stop"]);
        Assert.Equal(320, cpu.CX);
        Assert.Equal(-60, cpu.AX);
        Assert.False(cpu.FlagZero);
    }

    [Fact]
    public async Task Mul8_OverflowSeProdottoNonStaInByte()
    {
        var cpu = await Esegui(["mov ax, 10", "mov bx, 20", "mul bl", "stop"]);
        Assert.True(cpu.FlagOverflow);
        var cpu2 = await Esegui(["mov ax, 10", "mov bx, 2", "mul bl", "stop"]);
        Assert.False(cpu2.FlagOverflow);
    }

    [Fact]
    public async Task Div8()
    {
        // AL = AX / CL, AH = AX % CL
        var cpu = await Esegui(["mov ax, 300", "mov cx, 7", "div cl", "mov bx, ax", "mov ax, -7", "div cl", "stop"]);
        Assert.Equal((300 % 7) << 8 | (300 / 7), cpu.BX);
        Assert.Equal(0x00FF, cpu.AX);   // -7 / 7: AL = -1 (FFh), AH = resto 0
    }

    [Fact]
    public async Task DivisionePerZero_CpuException()
    {
        var ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ax, 5", "mov bx, 0", "div bx", "stop"]));
        Assert.Equal(CodiceErrore.DivisionePerZero, ex.err);
        ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ax, 5", "mov bx, 0", "div bl", "stop"]));
        Assert.Equal(CodiceErrore.DivisionePerZero, ex.err);
    }

    [Fact]
    public async Task QuozienteFuoriIntervallo_CpuException()
    {
        var ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ax, 1000", "mov bx, 2", "div bl", "stop"]));
        Assert.Equal(CodiceErrore.QuozienteFuoriIntervallo, ex.err);
        ex = await Assert.ThrowsAsync<CpuException>(() => Esegui(["mov ax, 0", "mov dx, 10", "div 2", "stop"]));
        Assert.Equal(CodiceErrore.QuozienteFuoriIntervallo, ex.err);
    }

    [Fact]
    public void ErroriCompilazione8()
    {
        string dim = Errori.Msg(CodiceErrore.DimensioneOperandi);
        Assert.Equal(dim, ErroreCompilazione("mov al, bx"));
        Assert.Equal(dim, ErroreCompilazione("add si, cl"));
        Assert.Equal(dim, ErroreCompilazione("push al"));
        Assert.Equal(dim, ErroreCompilazione("pop ah"));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), ErroreCompilazione("mov al, 256"));
        Assert.Equal(Errori.Msg(CodiceErrore.CostanteFuoriIntervallo), ErroreCompilazione("mov al, -129"));
        Assert.Null(ErroreCompilazione("mov al, 255"));
        Assert.Null(ErroreCompilazione("mov al, -128"));
        Assert.Null(ErroreCompilazione("mov al, 0FFh"));
        Assert.Null(ErroreCompilazione("mov [si+1], dl"));
        Assert.Null(ErroreCompilazione("shl ax, cl"));
        Assert.Null(ErroreCompilazione("cmp ch, 'a'"));
    }

    [Fact]
    public async Task DumpRegs_MostraByteAltoEBasso()
    {
        var cpu = await Esegui(["mov ax, 0141h", "stop"]);
        Ambiente.FormatoDati = FormatoValore.Hex;
        try
        {
            Assert.Equal("AX = 0141 [AH=01 AL=41]", cpu.DumpRegs()[0]);
            Assert.DoesNotContain("[", cpu.DumpRegs()[4]);   // SI non ha byte separati
        }
        finally
        {
            Ambiente.FormatoDati = FormatoValore.Dec;
        }
    }
}
