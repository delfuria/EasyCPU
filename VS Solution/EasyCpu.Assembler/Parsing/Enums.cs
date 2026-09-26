using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCpu.Assembler.Parsing
{
    public enum TipoOp
    {
        Dati,
        Codice
    }

    public enum TipoOperando
    {
        Nessuno,
        Registro,
        Costante,
        Memoria,        // indirizzamento diretto: [10]
        Indiretto,      // indirizzamento tramite registro: [si], [bp+2]
        Etichetta
    }

    public enum Registro
    {
        ax, bx, cx, dx,
        si, di,
        bp, sp,
        // registri a 8 bit: viste sui byte basso/alto di AX..DX
        al, ah, bl, bh, cl, ch, dl, dh
    }
}
