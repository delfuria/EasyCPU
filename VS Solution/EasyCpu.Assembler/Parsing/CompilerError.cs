using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCpu.Assembler.Parsing
{
    public class CompilerError : IFormattable
    {
        public const int CODICE = 0;
        public const int DATI = 1;
        public string Msg;
        public int Riga = -1;
        public int Colonna = 0;
        public int Tipo;
        public CompilerError(string msg, int riga, int colonna, int tipo)
        {
            this.Msg = msg;
            this.Riga = riga;
            this.Colonna = colonna;
            this.Tipo = tipo;
        }

        public override string ToString()
        {
            return Msg;
        }

        public string ToString(string format, IFormatProvider fp)
        {
            if (format == null)
                return ToString();

            string tipostr;
            if (Tipo == CODICE)
                tipostr = "C";
            else
                tipostr = "D";

            if (format == "T")
                return string.Format("[{0}] ({1}) {2}", tipostr, Riga + 1, Msg);
            else
                return ToString();

        }
    }

}
