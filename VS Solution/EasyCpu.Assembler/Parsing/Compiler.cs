using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Common;
using EasyCpu.Assembler.Processore;

namespace EasyCpu.Assembler.Parsing
{
    public class Compiler
    {
        readonly Parser _parser = new();
        List<IndirizzoEtichetta> _tabellaEtichette;

        public List<int> InstrToLineMap { get; private set; }  // indice istruzione → riga sorgente (0-based)
        public int[] LineToInstrMap { get; private set; }      // riga sorgente (0-based) → indice istruzione (-1 se non eseguibile)

        static bool SeCommento(string s)
        {
            return s[0] == '\'';
        }

        public List<int> CompilaDati(List<string> data, ref List<CompilerError> errori)
        {
            List<int> memoria = new int[Ram.MASSIMO_INDIRIZZO + 1].ToList();
            for (int indRiga = 0; indRiga < data.Count; indRiga++)
            {
                try
                {
                    int indirizzo;
                    string s = PreparaRiga(data[indRiga]);
                    if (s == "") continue;
                    List<int> rigaDati = _parser.CompilaDati(s, indRiga, out indirizzo);
                    if (rigaDati.Count + indirizzo > Ram.MASSIMO_INDIRIZZO)
                        throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);

                    for (int i = 0; i < rigaDati.Count; i++)
                        memoria[indirizzo + i] = rigaDati[i];
                }
                catch (CodiceException e)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(e.err), indRiga, _parser.IndCar, CompilerError.DATI));
                }
            }
            if (errori == null)
                return memoria;
            else
                return null;
        }

        public static string PreparaRiga(string riga)
        {
            StringBuilder sb = new StringBuilder();
            bool inCostanteChar = false;
            if (riga == null)
                return "";
            for (int i = 0; i < riga.Length; i++)
            {
                if (riga[i] == '/')
                {
                    if (i + 1 < riga.Length && riga[i + 1] == '/')
                        break;
                }
                if (riga[i] == '\'')
                    inCostanteChar = !inCostanteChar;
                char c = (inCostanteChar) ? riga[i] : Char.ToLower(riga[i]);
                sb.Append(c);
            }
            riga = sb.ToString().Trim() + Parser.FINE;
            if (riga.Length == 1)
                return "";
            return riga;
        }

        public List<Instruction> CompilaCodice(List<string> code, ref List<CompilerError> errori)
        {
            if (code == null || code.Count == 0) return null;
            int indiceEtichetta;
            List<Instruction> istruzioni = new List<Instruction>();
            List<IndirizzoEtichetta> etichette = new List<IndirizzoEtichetta>();
            List<int> debug = new List<int>();

            for (int indRiga = 0; indRiga < code.Count; indRiga++)
            {
                try
                {
                    string s = PreparaRiga(code[indRiga]);
                    string etichetta;
                    if (s == "") continue;
                    Instruction istr = _parser.Compila(s, out etichetta);

                    if (istr != null)
                    {
                        istr.indRiga = indRiga;
                        istruzioni.Add(istr);
                        debug.Add(indRiga);
                        indiceEtichetta = istruzioni.Count - 1;
                    }
                    else
                        indiceEtichetta = istruzioni.Count;

                    if (etichetta != null)
                        etichette.Add(new IndirizzoEtichetta(etichetta, indiceEtichetta));
                }
                catch (CodiceException e)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(e.err), indRiga, _parser.IndCar, CompilerError.CODICE));
                }
            }

            _tabellaEtichette = etichette;
            // risolve i riferimenti alle etichette
            for (int indRiga = 0; indRiga < istruzioni.Count; indRiga++)
            {
                Instruction istr = istruzioni[indRiga];
                if (istr.Etichetta == null) continue;
                istr.Offset1 = CercaEtichetta(istr.Etichetta);
                if (istr.Offset1 == -1)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(CodiceErrore.EtichettaNonValida), istr.indRiga, -1, CompilerError.CODICE));
                }
                istruzioni[indRiga] = istr;
            }

            if (errori == null && istruzioni.Count > 0)
            {
                InstrToLineMap = debug;
                BuildLineToInstrMap(code.Count, debug);
                return istruzioni;
            }
            else
                return null;
        }

        void BuildLineToInstrMap(int totalLines, List<int> debug)
        {
            int[] map = new int[totalLines];
            for (int i = 0; i < totalLines; i++) map[i] = -1;
            for (int instrIdx = 0; instrIdx < debug.Count; instrIdx++)
                map[debug[instrIdx]] = instrIdx;
            LineToInstrMap = map;
        }

        int CercaEtichetta(string s)
        {
            for (int i = 0; i < _tabellaEtichette.Count; i++)
                if (s == _tabellaEtichette[i].Etichetta)
                    return _tabellaEtichette[i].Indirizzo;
            return -1;
        }
    }
}
