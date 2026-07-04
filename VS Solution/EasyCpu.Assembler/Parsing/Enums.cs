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

    public enum IdOp
    {
        ax, bx, cx, dx,
        si, di,
        bp, sp,
        ip,     // non utilizzato nel parsing degli operandi
        Null,
        Costante,
        Memoria,
        Etichetta,
        _si, _di, _bp, _bx
    }
}
