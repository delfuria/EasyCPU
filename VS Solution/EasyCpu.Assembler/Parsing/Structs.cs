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

    public struct IndirizzoEtichetta
    {
        public string Etichetta;
        public int Indirizzo;
        public IndirizzoEtichetta(string etichetta, int indirizzo)
        {
            this.Etichetta = etichetta;
            this.Indirizzo = indirizzo;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Etichetta, Indirizzo);
        }
    }

}
