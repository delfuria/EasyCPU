using EasyCpu.Common;
using EasyCpu.Assembler.Processore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCpu.Assembler.Parsing
{
    public class Instruction
    {
        public string Code;
        public IdOp Op1;
        public IdOp Op2;
        public int Offset1;
        public int Offset2;
        public string Etichetta;
        public int indRiga;     // riga sorgente 0-based; per gestione errori compilazione
        public Instruction(string code, IdOp op1, IdOp op2, int offset1, int offset2)
        {
            this.Code = code;
            this.Op1 = op1;
            this.Op2 = op2;
            this.Offset1 = offset1;
            this.Offset2 = offset2;
            this.Etichetta = null;
            VerificaIstruzione();
        }

        public Instruction(string code, string etichetta) : this(code, IdOp.Null, IdOp.Null, 0, 0)
        {
            this.Etichetta = etichetta;
            this.Op1 = IdOp.Etichetta;
        }

        public Instruction(string code) : this(code, IdOp.Null, IdOp.Null, 0, 0) { }
        public Instruction(string code, IdOp op, int offset) : this(code, op, IdOp.Null, offset, 0) { }
        public Instruction(string code, IdOp op1, IdOp op2) : this(code, op1, op2, 0, 0) { }

        string OffsetToString(int offset)
        {
            if (offset > 0)
                return "+" + offset.ToString();
            if (offset < 0)
                return offset.ToString();
            return "";
        }

        string OpToString(IdOp op, int offset)
        {
            const string formatoIndiretto = "[{0}{1}]";
            string strOff = OffsetToString(offset);

            switch (op)
            {
                case IdOp.Costante: return OffsetToString(offset);
                case IdOp.Memoria: return "[" + offset.ToString() + "]";
                case IdOp.Etichetta: return offset.ToString();
                case IdOp.ax: return "ax";
                case IdOp.bx: return "bx";
                case IdOp.cx: return "cx";
                case IdOp.dx: return "dx";
                case IdOp.si: return "si";
                case IdOp.di: return "di";
                case IdOp.bp: return "bp";
                case IdOp.sp: return "sp";
                case IdOp._si: return string.Format(formatoIndiretto, "si", strOff);
                case IdOp._di: return string.Format(formatoIndiretto, "di", strOff);
                case IdOp._bp: return string.Format(formatoIndiretto, "bp", strOff);
                case IdOp._bx: return string.Format(formatoIndiretto, "bx", strOff);

                default: return "";
            }
        }


        public override string ToString()
        {
            string strOp1 = OpToString(Op1, Offset1);
            string strOp2 = OpToString(Op2, Offset2);
            string virgola = "";
            if (strOp2 != "")
                virgola = ",";
            return string.Format("{0} {1}{2} {3}", Code, strOp1, virgola, strOp2);
        }

        void VerificaIstruzione()
        {
            if (Op1 == IdOp.Costante && Op2 != IdOp.Null)   // destinazione costante
                throw new CodiceException(CodiceErrore.DestinazioneCostante);

            if ("not neg inc dec pop".IndexOf(Code) != -1)  // destinazione costante
            {
                if (Op1 == IdOp.Costante)
                    throw new CodiceException(CodiceErrore.DestinazioneCostante);
            }
        }
    }

}
