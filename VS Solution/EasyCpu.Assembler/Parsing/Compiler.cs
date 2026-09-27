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
        bool _etichetteLette;                       // pre-scansione del codice già eseguita
        List<CompilerError> _erroriEtichette;       // errori della pre-scansione, riportati da CompilaCodice
        List<(string nome, int indRiga)> _righeEtichette;   // etichette lette, con la riga che le definisce

        // modello di memoria del programma: da impostare prima di CompilaDati
        public ModelloMemoria Modello
        {
            get => _parser.Modello;
            set => _parser.Modello = value;
        }

        public List<int> InstrToLineMap { get; private set; }  // indice istruzione → riga sorgente (0-based)
        public int[] LineToInstrMap { get; private set; }      // riga sorgente (0-based) → indice istruzione (-1 se non eseguibile)

        // nomi definiti nella sezione dati ed etichette del codice: i dati vanno compilati prima del codice che li usa
        public IReadOnlyList<Simbolo> Simboli => _parser.ElencoSimboli;

        static bool SeCommento(string s)
        {
            return s[0] == '\'';
        }

        // codice: se fornito, le sue etichette sono lette per prime e i dati possono usarle (tab DW caso0, caso1)
        public List<int> CompilaDati(List<string> data, ref List<CompilerError> errori, List<string> codice = null)
        {
            List<int> memoria = new int[Modello.Dimensione].ToList();
            _parser.AzzeraSimboli();
            _etichetteLette = false;
            if (codice != null)
                LeggiEtichette(codice);
            int contatore = 0;      // prossimo indirizzo libero per DB/DW
            for (int indRiga = 0; indRiga < data.Count; indRiga++)
            {
                try
                {
                    int indirizzo;
                    string s = PreparaRiga(data[indRiga]);
                    if (s == "") continue;
                    // stessa riga senza conversione in minuscolo: fornisce la grafia originale dei nomi
                    string originale = PreparaRiga(data[indRiga], minuscole: false);
                    List<int> rigaDati = _parser.CompilaDati(s, originale, ref contatore, out indirizzo);
                    if (rigaDati.Count + indirizzo > Modello.Dimensione - 1)
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

        public static string PreparaRiga(string riga, bool minuscole = true)
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
                if (!inCostanteChar && riga[i] == ';')
                    break;
                if (riga[i] == '\'')
                    inCostanteChar = !inCostanteChar;
                char c = (inCostanteChar || !minuscole) ? riga[i] : Char.ToLower(riga[i]);
                sb.Append(c);
            }
            riga = sb.ToString().Trim() + Parser.FINE;
            if (riga.Length == 1)
                return "";
            return riga;
        }

        // Pre-scansione: definisce le etichette con il numero dell'istruzione a cui puntano, prima
        // che dati e codice le usino. Ogni riga non vuota produce al massimo un'istruzione; una riga
        // con la sola etichetta punta all'istruzione successiva (stessa regola di CompilaCodice).
        void LeggiEtichette(List<string> code)
        {
            _etichetteLette = true;
            _erroriEtichette = null;
            _righeEtichette = new();
            int istruzione = 0;
            for (int indRiga = 0; indRiga < code.Count; indRiga++)
            {
                string s = PreparaRiga(code[indRiga]);
                if (s == "") continue;
                string etichetta = _parser.LeggiEtichetta(s, out bool soloEtichetta);
                if (etichetta != null)
                {
                    try
                    {
                        string grafia = PreparaRiga(code[indRiga], minuscole: false).Substring(0, etichetta.Length);
                        _parser.DefinisciEtichetta(etichetta, grafia, istruzione);
                        _righeEtichette.Add((etichetta, indRiga));
                    }
                    catch (CodiceException e)
                    {
                        _erroriEtichette ??= new List<CompilerError>();
                        _erroriEtichette.Add(new CompilerError(Errori.Msg(e.err), indRiga, 0, CompilerError.CODICE));
                    }
                }
                if (!soloEtichetta)
                    istruzione++;
            }
        }

        public List<Instruction> CompilaCodice(List<string> code, ref List<CompilerError> errori)
        {
            if (code == null || code.Count == 0) return null;
            if (!_etichetteLette)
                LeggiEtichette(code);
            if (_erroriEtichette != null)
                (errori ??= new List<CompilerError>()).AddRange(_erroriEtichette);
            foreach (var (nome, indRiga) in _righeEtichette)
            {
                try
                {
                    _parser.AggiungiEtichetta(nome);
                }
                catch (CodiceException e)
                {
                    (errori ??= new List<CompilerError>()).Add(new CompilerError(Errori.Msg(e.err), indRiga, 0, CompilerError.CODICE));
                }
            }
            List<Instruction> istruzioni = new List<Instruction>();
            List<int> debug = new List<int>();

            for (int indRiga = 0; indRiga < code.Count; indRiga++)
            {
                try
                {
                    string s = PreparaRiga(code[indRiga]);
                    if (s == "") continue;
                    Instruction istr = _parser.Compila(s, out _);     // etichette già definite dalla pre-scansione

                    if (istr != null)
                    {
                        istr.indRiga = indRiga;
                        istruzioni.Add(istr);
                        debug.Add(indRiga);
                    }
                }
                catch (CodiceException e)
                {
                    if (errori == null)
                        errori = new List<CompilerError>();
                    errori.Add(new CompilerError(Errori.Msg(e.err), indRiga, _parser.IndCar, CompilerError.CODICE));
                }
            }

            // risolve i riferimenti alle etichette
            for (int indRiga = 0; indRiga < istruzioni.Count; indRiga++)
            {
                Instruction istr = istruzioni[indRiga];
                if (istr.Etichetta == null) continue;
                istr.Op1.Scostamento = _parser.CercaEtichetta(istr.Etichetta);
                if (istr.Op1.Scostamento == -1)
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
    }
}
