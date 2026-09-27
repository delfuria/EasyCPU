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
        public Operando Op1;
        public Operando Op2;
        public int Larghezza;   // dimensione dell'operazione: 8 o 16 bit
        public string Etichetta;
        public string Prefisso;     // rep, repe/repz, repne/repnz; null se assente
        public int indRiga;     // riga sorgente 0-based; per gestione errori compilazione
        public Instruction(string code, Operando op1, Operando op2)
        {
            this.Code = code;
            this.Op1 = op1;
            this.Op2 = op2;
            this.Etichetta = null;
            VerificaIstruzione();
        }

        public Instruction(string code, string etichetta) : this(code, Operando.Nessuno, Operando.Nessuno)
        {
            this.Etichetta = etichetta;
            this.Op1.Tipo = TipoOperando.Etichetta;
        }

        public Instruction(string code) : this(code, Operando.Nessuno, Operando.Nessuno) { }
        public Instruction(string code, Operando op) : this(code, op, Operando.Nessuno) { }

        string OffsetToString(int offset)
        {
            if (offset > 0)
                return "+" + offset.ToString();
            if (offset < 0)
                return offset.ToString();
            return "";
        }

        string OpToString(Operando op)
        {
            const string formatoIndiretto = "[{0}{1}]";

            switch (op.Tipo)
            {
                case TipoOperando.Costante: return OffsetToString(op.Scostamento);
                case TipoOperando.Memoria: return "[" + op.Scostamento.ToString() + "]";
                case TipoOperando.Etichetta: return op.Scostamento.ToString();
                case TipoOperando.Registro: return op.Base.ToString();
                case TipoOperando.Indiretto: return string.Format(formatoIndiretto,
                    op.HaIndice ? op.Base + "+" + op.Indice : op.Base.ToString(), OffsetToString(op.Scostamento));

                default: return "";
            }
        }


        public override string ToString()
        {
            string strOp1 = OpToString(Op1);
            string strOp2 = OpToString(Op2);
            string virgola = "";
            if (strOp2 != "")
                virgola = ",";
            return string.Format("{0} {1}{2} {3}", Code, strOp1, virgola, strOp2);
        }

        static bool InMemoria(Operando op) => op.Tipo is TipoOperando.Memoria or TipoOperando.Indiretto;

        // Modalità x86 fedele: niente operazioni memoria-memoria (le istruzioni stringa non hanno
        // operandi) e la dimensione di un operando in memoria dev'essere determinata da un registro,
        // dal tipo della variabile o da byte ptr / word ptr. Stack e salti indiretti lavorano a parole.
        public void VerificaMemoriaAByte()
        {
            if (InMemoria(Op1) && InMemoria(Op2))
                throw new CodiceException(CodiceErrore.OperandiMemoriaMemoria);
            if (Code is "push" or "pop" or "jmp" or "call" or "lea")
                return;
            bool shift = Code is "shl" or "shr" or "sar" or "rol" or "ror" or "rcl" or "rcr";
            int larg2 = shift ? 0 : Op2.Larghezza;
            if ((InMemoria(Op1) || InMemoria(Op2)) && Op1.Larghezza == 0 && larg2 == 0)
                throw new CodiceException(CodiceErrore.DimensioneNonSpecificata);
        }

        void VerificaIstruzione()
        {
            if (Op1.Tipo == TipoOperando.Costante && Op2.Tipo != TipoOperando.Nessuno)   // destinazione costante
                throw new CodiceException(CodiceErrore.DestinazioneCostante);

            if ("not neg inc dec pop".IndexOf(Code) != -1)  // destinazione costante
            {
                if (Op1.Tipo == TipoOperando.Costante)
                    throw new CodiceException(CodiceErrore.DestinazioneCostante);
            }

            if (Code is "jmp" or "call")    // forma indiretta: indirizzo a 16 bit in un registro o in memoria
            {
                if (Op1.Tipo == TipoOperando.Costante)      // per un salto diretto si scrive l'etichetta
                    throw new CodiceException(CodiceErrore.OperandoNonValido);
                if (Op1.Larghezza == 8)
                    throw new CodiceException(CodiceErrore.DimensioneOperandi);
            }

            if (Code == "xchg")     // scambio: nessuna costante, al massimo un operando in memoria
            {
                if (Op2.Tipo == TipoOperando.Costante || (InMemoria(Op1) && InMemoria(Op2)))
                    throw new CodiceException(CodiceErrore.OperandoNonValido);
            }

            if (Code == "lea")      // lea registro16, memoria
            {
                if (Op1.Tipo != TipoOperando.Registro || Op1.Larghezza != 16 || !InMemoria(Op2))
                    throw new CodiceException(CodiceErrore.OperandoNonValido);
                Larghezza = 16;
                return;
            }

            // dimensione: la determinano i registri; nelle shift e rotazioni il secondo operando è il conteggio
            bool shift = Code is "shl" or "shr" or "sar" or "rol" or "ror" or "rcl" or "rcr";
            int larg1 = Op1.Larghezza;
            int larg2 = shift ? 0 : Op2.Larghezza;
            if (larg1 != 0 && larg2 != 0 && larg1 != larg2)
                throw new CodiceException(CodiceErrore.DimensioneOperandi);
            Larghezza = larg1 != 0 ? larg1 : (larg2 != 0 ? larg2 : 16);
            if (Code is "movsb" or "lodsb" or "stosb" or "cmpsb" or "scasb")
                Larghezza = 8;

            if (Larghezza == 8)
            {
                if (Code == "push" || Code == "pop")
                    throw new CodiceException(CodiceErrore.DimensioneOperandi);
                if (!shift && Op2.Tipo == TipoOperando.Costante && (Op2.Scostamento < sbyte.MinValue || Op2.Scostamento > byte.MaxValue))
                    throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
            }
        }
    }

}
