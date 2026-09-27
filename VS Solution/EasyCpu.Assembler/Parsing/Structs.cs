using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCpu.Assembler.Parsing
{
    public struct OpCode
    {
        public string Nome;
        public int NumOp;
        public TipoOp Tipo;

        public OpCode(string Nome, int numOp, TipoOp tipo)
        {
            this.Nome = Nome;
            this.NumOp = numOp;
            this.Tipo = tipo;
        }

        public OpCode(string Nome, int numOp)
        {
            this.Nome = Nome;
            this.NumOp = numOp;
            this.Tipo = TipoOp.Dati;
        }
    }

    public struct Operando
    {
        public TipoOperando Tipo;
        public Registro Base;       // Registro: il registro; Indiretto: il registro base
        public bool HaIndice;       // Indiretto: true se è presente anche un registro indice ([bx+si])
        public Registro Indice;     // Indiretto: SI o DI sommato a BX o BP
        public int Scostamento;     // Costante: valore; Memoria: indirizzo; Indiretto: scostamento; Etichetta: indice istruzione
        public int Dimensione;      // Memoria/Indiretto: 8 o 16 se l'accesso usa una variabile DB/DW, altrimenti 0

        // 8 o 16 per i registri e le variabili; 0 se la dimensione non è determinata dall'operando
        public int Larghezza => Tipo == TipoOperando.Registro ? (Base >= Registro.al ? 8 : 16) : Dimensione;

        public static readonly Operando Nessuno = new Operando { Tipo = TipoOperando.Nessuno };

        public static Operando DiRegistro(Registro reg) => new Operando { Tipo = TipoOperando.Registro, Base = reg };
        public static Operando DiIndiretto(Registro reg, int scostamento) => new Operando { Tipo = TipoOperando.Indiretto, Base = reg, Scostamento = scostamento };
        public static Operando DiCostante(int valore) => new Operando { Tipo = TipoOperando.Costante, Scostamento = valore };
        public static Operando DiMemoria(int indirizzo) => new Operando { Tipo = TipoOperando.Memoria, Scostamento = indirizzo };
    }
}
