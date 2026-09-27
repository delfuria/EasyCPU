using System;
using System.Text;
using System.Collections;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Processore;
using EasyCpu.Common;

namespace EasyCpu.Assembler.Parsing
{
	public class Parser
	{
		public static readonly OpCode[] SetCode =
		{
			new OpCode("mov", 2),
			new OpCode("movs", 0),
			new OpCode("add", 2),
			new OpCode("sub", 2),
			new OpCode("cmp", 2),
			new OpCode("and", 2),
			new OpCode("or", 2),
			new OpCode("xor", 2),
			new OpCode("not", 1),
			new OpCode("neg", 1),
			new OpCode("mul", 1),
			new OpCode("div", 1),
			new OpCode("inc", 1),
			new OpCode("dec", 1),
			new OpCode("push", 1),
			new OpCode("pop", 1),
			new OpCode("int", 1),
			new OpCode("pushf", 0),
			new OpCode("popf", 0),
			new OpCode("call", 1, TipoOp.Codice),
			new OpCode("jcxz", 1, TipoOp.Codice),
			new OpCode("je", 1, TipoOp.Codice),
			new OpCode("jg", 1, TipoOp.Codice),
			new OpCode("jl", 1, TipoOp.Codice),
			new OpCode("jle", 1, TipoOp.Codice),
			new OpCode("jge", 1, TipoOp.Codice),
			new OpCode("jmp", 1, TipoOp.Codice),
			new OpCode("jne", 1, TipoOp.Codice),
			new OpCode("jo", 1, TipoOp.Codice),
			new OpCode("jno", 1, TipoOp.Codice),
			new OpCode("js", 1, TipoOp.Codice),
			new OpCode("jns", 1, TipoOp.Codice),
			new OpCode("ret", 0),
			new OpCode("nop", 0),
			new OpCode("stop", 0),
			new OpCode("shl", 2),
			new OpCode("shr", 2),
			// fase 3: carry, aritmetica senza segno, istruzioni x86 comuni
			new OpCode("adc", 2),
			new OpCode("sbb", 2),
			new OpCode("imul", 1),
			new OpCode("idiv", 1),
			new OpCode("cbw", 0),
			new OpCode("cwd", 0),
			new OpCode("clc", 0),
			new OpCode("stc", 0),
			new OpCode("cmc", 0),
			new OpCode("test", 2),
			new OpCode("xchg", 2),
			new OpCode("lea", 2),
			new OpCode("sar", 2),
			new OpCode("rol", 2),
			new OpCode("ror", 2),
			new OpCode("rcl", 2),
			new OpCode("rcr", 2),
			new OpCode("ja", 1, TipoOp.Codice),
			new OpCode("jae", 1, TipoOp.Codice),
			new OpCode("jb", 1, TipoOp.Codice),
			new OpCode("jbe", 1, TipoOp.Codice),
			new OpCode("jc", 1, TipoOp.Codice),
			new OpCode("jnc", 1, TipoOp.Codice),
			new OpCode("jz", 1, TipoOp.Codice),
			new OpCode("jnz", 1, TipoOp.Codice),
			new OpCode("jna", 1, TipoOp.Codice),
			new OpCode("jnae", 1, TipoOp.Codice),
			new OpCode("jnb", 1, TipoOp.Codice),
			new OpCode("jnbe", 1, TipoOp.Codice),
			new OpCode("jng", 1, TipoOp.Codice),
			new OpCode("jnge", 1, TipoOp.Codice),
			new OpCode("jnl", 1, TipoOp.Codice),
			new OpCode("jnle", 1, TipoOp.Codice),
			new OpCode("loop", 1, TipoOp.Codice),
			new OpCode("loope", 1, TipoOp.Codice),
			new OpCode("loopz", 1, TipoOp.Codice),
			new OpCode("loopne", 1, TipoOp.Codice),
			new OpCode("loopnz", 1, TipoOp.Codice),
			// fase 4: istruzioni stringa e flag di direzione
			new OpCode("movsb", 0),
			new OpCode("movsw", 0),
			new OpCode("lodsb", 0),
			new OpCode("lodsw", 0),
			new OpCode("stosb", 0),
			new OpCode("stosw", 0),
			new OpCode("cmpsb", 0),
			new OpCode("cmpsw", 0),
			new OpCode("scasb", 0),
			new OpCode("scasw", 0),
			new OpCode("cld", 0),
			new OpCode("std", 0),
		};

		static readonly HashSet<string> Prefissi = ["rep", "repe", "repz", "repne", "repnz"];
		static readonly HashSet<string> IstruzioniStringa =
			["movs", "movsb", "movsw", "lodsb", "lodsw", "stosb", "stosw", "cmpsb", "cmpsw", "scasb", "scasw"];

		static readonly Dictionary<string, Registro> Registri =
			Enum.GetValues<Registro>().ToDictionary(r => r.ToString());

		// parole riservate della sezione dati, non utilizzabili come nomi
		static readonly HashSet<string> Riservate = ["db", "dw", "equ", "org", "dup", "offset"];

		public const char FINE = '\0';

		// limiti della memoria del programma (indirizzi, area dello stack, unità occupate da DW)
		public ModelloMemoria Modello { get; set; } = ModelloMemoria.Parole;
		string _riga;
		int _indcar;
		bool _inDati;       // true durante la compilazione della sezione dati: le etichette valgono senza offset

		// nomi definiti nella sezione dati ed etichette del codice, in ordine di definizione
		readonly Dictionary<string, Simbolo> _simboli = new();
		public List<Simbolo> ElencoSimboli { get; } = new();

		// etichette della pre-scansione, non ancora aggiunte ai simboli: la sezione dati le può
		// usare, ma in caso di conflitto con una variabile l'errore va sulla riga dell'etichetta
		readonly Dictionary<string, Simbolo> _etichette = new();

		public void AzzeraSimboli()
		{
			_simboli.Clear();
			ElencoSimboli.Clear();
			_etichette.Clear();
		}

		public int IndCar => _indcar;

		void SaltaSpazi()
		{
			while (SeSpazio(_riga[_indcar]))
				_indcar++;
		}

		public char TestChar()
		{
			SaltaSpazi();
			return _riga[_indcar];
		}

		public string EstraiToken()
		{
			SaltaSpazi();
			if (SeFine())
				return null;

			switch (_riga[_indcar])
			{
				case '\'': _indcar++; return "'";
				case ',': _indcar++; return ",";
				case ':': _indcar++; return ":";
				case '[': _indcar++; return "[";
				case ']': _indcar++; return "]";
				case '(': _indcar++; return "(";
				case ')': _indcar++; return ")";
				case '?': _indcar++; return "?";
				case '+':
				case '-': return _riga[_indcar++].ToString();

				default:
					if (SeInIdentificatore(_riga[_indcar]))
					{
						int numCar = 1;
						while (SeInIdentificatore(_riga[_indcar + numCar]))
							numCar++;
						string tmp = _riga.Substring(_indcar, numCar);
						_indcar += numCar;
						return tmp;
					}
					else
						throw new CodiceException(CodiceErrore.CarattereNonValido);
			}
		}

		int LeggiCostanteChar()
		{
			_indcar++;
			char c = _riga[_indcar++];
			if (c == FINE)
				throw new CodiceException(CodiceErrore.Formato);
			int valore = c;
			c = _riga[_indcar++];
			if (c != '\'')
				throw new CodiceException(CodiceErrore.Formato);
			return valore;
		}

		public string LeggiIdentificatore()
		{
			SaltaSpazi();
			if (SeFine())
				return null;
			if (SeInIdentificatore(_riga[_indcar]))
			{
				int numCar = 1;
				while (SeInIdentificatore(_riga[_indcar + numCar]))
					numCar++;
				string tmp = _riga.Substring(_indcar, numCar);
				_indcar += numCar;
				return tmp;
			}
			return null;
		}

		public string TestToken()
		{
			int tmp = _indcar;
			string token = EstraiToken();
			_indcar = tmp;
			return token;
		}

		bool SeFine()
		{
			return _riga[_indcar] == FINE;
		}

		static bool SeSpazio(char c)
		{
			return c == ' ' || c == '\t';
		}

		static bool SeInIdentificatore(char c)
		{
			return Char.IsLetterOrDigit(c) || c == '_';
		}

		static bool SeEtichetta(string s)
		{
			return !(s == null) && !(s == "") && Char.IsLetter(s[0]) &&
				s[s.Length - 1] == ':';
		}

		public int LeggiValore()
		{
			char c = TestChar();
			if (c == '\'')
			{
				return LeggiCostanteChar();
			}
			string tok = EstraiToken();
			string segno = "";
			if (tok == "-" || tok == "+")
			{
				segno = tok;
				tok = EstraiToken();
			}
			return StringToInt(segno + tok);
		}

		int LeggiIndirizzo()
		{
			string tok = EstraiToken();
			try
			{
				return StringToInt(tok);
			}
			catch
			{
				throw new CodiceException(CodiceErrore.IndirizzoDatiNonValido);
			}
		}

		List<int> LeggiValori()
		{
			List<int> valori = new List<int>();
			valori.Add(LeggiValore());
			string token = EstraiToken();
			while (token != null)
			{
				if (token != ",")
					throw new CodiceException(CodiceErrore.AttesaVirgola);
				valori.Add(LeggiValore());
				token = EstraiToken();
			}
			return valori;
		}

		public bool LeggiSimbolo(string sim)
		{
			return EstraiToken() == sim;
		}

		public bool TestSimbolo(string sim)
		{
			return TestToken() == sim;
		}

		static bool SeSegno(char c)
		{
			return c == '-' || c == '+';
		}

		static bool SeSegno(string s)
		{
			return s == "-" || s == "+";
		}

		public static int StringToInt(string valore)
		{
			int tmp;
			if (valore == null || valore == "")
				throw new CodiceException(CodiceErrore.Formato);
			int segno = 1;
			if (SeSegno(valore[0]))
			{
				segno = (valore[0] == '+') ? 1 : -1;
				valore = valore.Remove(0, 1);
			}
			if (valore == "")
				throw new CodiceException(CodiceErrore.Formato);
			if (!Char.IsDigit(valore[0]))
				throw new CodiceException(CodiceErrore.Formato);
			if (valore[valore.Length - 1] == 'h')
			{
				if (segno == -1)
					throw new CodiceException(CodiceErrore.Formato);
				else
					tmp = segno * HexToInt(valore.Substring(0, valore.Length - 1));

				if (tmp > ushort.MaxValue || tmp < ushort.MinValue)
					throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
			}
			else
			{
				try
				{
					tmp = segno * Convert.ToInt32(valore);
				}
				catch
				{
					throw new CodiceException(CodiceErrore.Formato);
				}
				if (tmp > short.MaxValue || tmp < short.MinValue)
					throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
			}
			return tmp;
		}

		public static int HexToInt(string valore)
		{
			int ris = 0;
			int len = valore.Length - 1;
			for (int i = len; i >= 0; i--)
				if (valore[i] >= 'a' && valore[i] <= 'f')
					ris += (valore[i] - 'a' + 10) * (int)Math.Pow(16, len - i);
				else
					if (valore[i] >= '0' && valore[i] <= '9')
					ris += (valore[i] - '0') * (int)Math.Pow(16, len - i);
				else
					throw new CodiceException(CodiceErrore.Formato);
			return ris;
		}

		public static int CercaOpCode(string Nome, out int numOp, out TipoOp tipo)
		{
			for (int i = 0; i < SetCode.Length; i++)
				if (Nome == SetCode[i].Nome)
				{
					numOp = SetCode[i].NumOp;
					tipo = SetCode[i].Tipo;
					return i;
				}
			numOp = -1;
			tipo = TipoOp.Dati;
			return -1;
		}

		// Espressione: somma di termini (costanti, nomi EQU, variabili, offset di variabili
		// e, fra parentesi quadre, un registro tra SI, DI, BX, BP)
		struct Espressione
		{
			public int Valore;
			public bool HaRegistro;
			public Registro Registro;
			public bool HaIndice;       // secondo registro: [bx+si], [bp+di], ...
			public Registro Indice;
			public Simbolo Variabile;   // prima variabile DB/DW usata senza offset
		}

		Espressione LeggiEspressione(bool inParentesi)
		{
			var espr = new Espressione();
			string segno = "+";
			if (TestToken() is "+" or "-")
				segno = EstraiToken();
			while (true)
			{
				LeggiTermine(segno, inParentesi, ref espr);
				if (TestToken() is not ("+" or "-"))
					return espr;
				segno = EstraiToken();
			}
		}

		void LeggiTermine(string segno, bool inParentesi, ref Espressione espr)
		{
			int s = segno == "-" ? -1 : 1;
			if (TestChar() == '\'')
			{
				espr.Valore += s * LeggiCostanteChar();
				return;
			}
			string token = TestToken();
			if (token == null)
				throw new CodiceException(CodiceErrore.AttesaCostante);

			if (Registri.TryGetValue(token, out Registro reg))
			{
				if (!inParentesi)
					throw new CodiceException(CodiceErrore.Sintassi);
				if (reg is not (Registro.si or Registro.di or Registro.bx or Registro.bp))
					throw new CodiceException(CodiceErrore.AttesoRegistroIndiretto);
				if (s < 0 || espr.HaIndice)
					throw new CodiceException(CodiceErrore.Sintassi);
				EstraiToken();
				if (!espr.HaRegistro)
				{
					espr.HaRegistro = true;
					espr.Registro = reg;
					return;
				}
				// base + indice, come su x86: BX o BP con SI o DI, in qualsiasi ordine
				bool primoBase = espr.Registro is Registro.bx or Registro.bp;
				bool secondoBase = reg is Registro.bx or Registro.bp;
				if (primoBase == secondoBase)
					throw new CodiceException(CodiceErrore.CombinazioneRegistriNonValida);
				espr.HaIndice = true;
				espr.Indice = primoBase ? reg : espr.Registro;
				espr.Registro = primoBase ? espr.Registro : reg;
				return;
			}
			if (token == "offset")
			{
				EstraiToken();
				Simbolo var = CercaSimbolo(EstraiToken());
				if (var.Tipo == TipoSimbolo.Equ)
					throw new CodiceException(CodiceErrore.Sintassi);
				espr.Valore += s * var.Valore;
				return;
			}
			if (Char.IsLetter(token[0]) || token[0] == '_')
			{
				EstraiToken();
				Simbolo sim = CercaSimbolo(token);
				if (sim.Tipo == TipoSimbolo.Etichetta)
				{
					// nel codice l'indirizzo di un'etichetta si scrive offset nome, come per le variabili
					if (!_inDati)
						throw new CodiceException(CodiceErrore.EtichettaSenzaOffset);
				}
				else if (sim.Tipo != TipoSimbolo.Equ)
				{
					if (s < 0)
						throw new CodiceException(CodiceErrore.Sintassi);
					espr.Variabile ??= sim;
				}
				espr.Valore += s * sim.Valore;
				return;
			}
			EstraiToken();
			espr.Valore += StringToInt((s < 0 ? "-" : "") + token);
		}

		Simbolo CercaSimbolo(string nome)
		{
			if (nome == null || !(_simboli.TryGetValue(nome, out Simbolo sim) || _etichette.TryGetValue(nome, out sim)))
				throw new CodiceException(CodiceErrore.SimboloNonDefinito);
			return sim;
		}

		Operando LeggiOperandoIndiretto()
		{
			Espressione espr = LeggiEspressione(inParentesi: true);
			if (EstraiToken() != "]")
				throw new CodiceException(CodiceErrore.AttesaQuadraChiusura);
			return OperandoMemoria(espr);
		}

		Operando OperandoMemoria(Espressione espr)
		{
			Operando op;
			if (espr.HaRegistro)
			{
				op = Operando.DiIndiretto(espr.Registro, espr.Valore);
				op.HaIndice = espr.HaIndice;
				op.Indice = espr.Indice;
			}
			else
			{
				if (!IndirizzoOk(espr.Valore))
					throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);
				op = Operando.DiMemoria(espr.Valore);
			}
			op.Dimensione = espr.Variabile?.Larghezza ?? 0;
			return op;
		}

		bool IndirizzoOk(int indirizzo)
		{
			return !(indirizzo < 0 || indirizzo >= Modello.Dimensione);
		}

		static void VerificaIntervalloCostante(int valore)
		{
			if (valore < short.MinValue || valore > ushort.MaxValue)
				throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
		}

		// byte ptr / word ptr davanti a un operando in memoria: ne fissano la dimensione,
		// con la precedenza sul tipo della variabile
		Operando LeggiOperando()
		{
			int dimensione = LeggiPtr();
			Operando op = LeggiOperandoSemplice();
			if (dimensione != 0)
			{
				if (op.Tipo is not (TipoOperando.Memoria or TipoOperando.Indiretto))
					throw new CodiceException(CodiceErrore.OperandoNonValido);
				op.Dimensione = dimensione;
			}
			return op;
		}

		// 8 per byte ptr, 16 per word ptr, 0 se assente; byte e word senza ptr restano nomi qualsiasi
		int LeggiPtr()
		{
			string token = TestToken();
			if (token is not ("byte" or "word"))
				return 0;
			int inizio = _indcar;
			EstraiToken();
			if (TestToken() != "ptr")
			{
				_indcar = inizio;
				return 0;
			}
			EstraiToken();
			return token == "byte" ? 8 : 16;
		}

		Operando LeggiOperandoSemplice()
		{
			string token = TestToken();
			if (token == "[")
			{
				EstraiToken();      // scarta parentesi quadra
				return LeggiOperandoIndiretto();
			}
			if (token != null && Registri.TryGetValue(token, out Registro reg))
			{
				EstraiToken();  // scarta registro
				return Operando.DiRegistro(reg);
			}
			Espressione espr = LeggiEspressione(inParentesi: false);
			if (espr.Variabile != null)     // stile MASM: il nome di una variabile indica il suo contenuto
				return OperandoMemoria(espr);
			VerificaIntervalloCostante(espr.Valore);
			return Operando.DiCostante(espr.Valore);
		}

		// Compila una riga della sezione dati. Forme ammesse:
		//   indirizzo: valore, valore, ...     (sintassi originale, indirizzo esplicito)
		//   [nome] DB|DW elemento, ...          (allocazione dal contatore)
		//   nome EQU valore
		//   ORG indirizzo
		// Restituisce i valori da scrivere a partire da indirizzo (lista vuota per EQU e ORG).
		public List<int> CompilaDati(string s, string originale, ref int contatore, out int indirizzo)
		{
			indirizzo = 0;
			_riga = s;
			_indcar = 0;
			_inDati = true;
			string primo = TestToken();
			if (primo != null && Char.IsDigit(primo[0]))
			{
				if (Modello == ModelloMemoria.Byte)     // senza tipo: nella memoria a byte servono DB o DW
					throw new CodiceException(CodiceErrore.IndirizzoValoriInModalitaByte);
				return CompilaDatiIndirizzo(out indirizzo);
			}

			string nome = null;
			string grafia = null;
			string direttiva = LeggiIdentificatore();
			if (direttiva != null && !Riservate.Contains(direttiva))
			{
				nome = direttiva;
				grafia = originale.Substring(_indcar - nome.Length, nome.Length);
				direttiva = LeggiIdentificatore();
			}

			switch (direttiva)
			{
				case "org":
					if (nome != null)
						throw new CodiceException(CodiceErrore.Sintassi);
					int nuovo = LeggiEspressione(inParentesi: false).Valore;
					VerificaFineRiga();
					if (nuovo < 0 || nuovo >= Modello.InizioStack)
						throw new CodiceException(CodiceErrore.DatiInAreaStack);
					contatore = nuovo;
					return new List<int>();

				case "equ":
					if (nome == null)
						throw new CodiceException(CodiceErrore.Sintassi);
					int valore = LeggiEspressione(inParentesi: false).Valore;
					VerificaFineRiga();
					VerificaIntervalloCostante(valore);
					DefinisciSimbolo(new Simbolo(nome, TipoSimbolo.Equ, valore, 0) { Grafia = grafia });
					return new List<int>();

				case "db":
				case "dw":
					TipoSimbolo tipo = direttiva == "db" ? TipoSimbolo.Db : TipoSimbolo.Dw;
					List<int> valori = LeggiElementiDati(tipo);
					List<int> unita = InUnitaDiMemoria(tipo, valori);
					if (contatore + unita.Count > Modello.InizioStack)
						throw new CodiceException(CodiceErrore.DatiInAreaStack);
					if (nome != null)
						DefinisciSimbolo(new Simbolo(nome, tipo, contatore, valori.Count) { Grafia = grafia });
					indirizzo = contatore;
					contatore += unita.Count;
					return unita;

				default:
					throw new CodiceException(CodiceErrore.Sintassi);
			}
		}

		List<int> CompilaDatiIndirizzo(out int indirizzo)
		{
			indirizzo = LeggiIndirizzo();
			if (EstraiToken() != ":")
				throw new CodiceException(CodiceErrore.AttesoDuePunti);

			if (indirizzo < 0 || indirizzo >= Modello.Dimensione)
				throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);

			return LeggiValori();
		}

		void VerificaFineRiga()
		{
			if (TestToken() != null)
				throw new CodiceException(CodiceErrore.Sintassi);
		}

		void DefinisciSimbolo(Simbolo sim)
		{
			if (Registri.ContainsKey(sim.Nome) || CercaOpCode(sim.Nome, out _, out _) != -1)
				throw new CodiceException(CodiceErrore.NomeSimboloNonValido);
			if (_simboli.ContainsKey(sim.Nome))
				throw new CodiceException(CodiceErrore.SimboloDuplicato);
			_simboli.Add(sim.Nome, sim);
			ElencoSimboli.Add(sim);
		}

		List<int> LeggiElementiDati(TipoSimbolo tipo)
		{
			var valori = new List<int>();
			LeggiElementoDati(tipo, valori);
			while (TestToken() == ",")
			{
				EstraiToken();
				LeggiElementoDati(tipo, valori);
			}
			if (TestToken() != null)
				throw new CodiceException(CodiceErrore.AttesaVirgola);
			return valori;
		}

		// elemento: valore | ? | 'stringa' (solo DB) | n DUP(elemento)
		void LeggiElementoDati(TipoSimbolo tipo, List<int> valori)
		{
			if (TestChar() == '\'')
			{
				string testo = LeggiStringa();
				if (tipo == TipoSimbolo.Dw && testo.Length != 1)
					throw new CodiceException(CodiceErrore.Formato);
				foreach (char c in testo)
					valori.Add(ValoreDato(tipo, c));
				return;
			}
			if (TestToken() == "?")
			{
				EstraiToken();
				valori.Add(0);
				return;
			}
			int valore = LeggiEspressione(inParentesi: false).Valore;
			if (TestToken() != "dup")
			{
				valori.Add(ValoreDato(tipo, valore));
				return;
			}
			EstraiToken();
			if (EstraiToken() != "(")
				throw new CodiceException(CodiceErrore.Sintassi);
			var ripetuti = new List<int>();
			LeggiElementoDati(tipo, ripetuti);
			if (EstraiToken() != ")")
				throw new CodiceException(CodiceErrore.Sintassi);
			if (valore <= 0 || valore * ripetuti.Count > Modello.InizioStack)
				throw new CodiceException(CodiceErrore.DatiInAreaStack);
			for (int i = 0; i < valore; i++)
				valori.AddRange(ripetuti);
		}

		// Valori di una riga DB/DW come contenuto delle unità di memoria: una cella per valore nella
		// modalità a parole; con PassoParola 2 una parola DW occupa due byte, prima il basso (little-endian)
		List<int> InUnitaDiMemoria(TipoSimbolo tipo, List<int> valori)
		{
			if (tipo != TipoSimbolo.Dw || Modello.PassoParola == 1)
				return valori;
			var unita = new List<int>(valori.Count * 2);
			foreach (int v in valori)
			{
				unita.Add(v & 0xFF);
				unita.Add((v >> 8) & 0xFF);
			}
			return unita;
		}

		static int ValoreDato(TipoSimbolo tipo, int valore)
		{
			bool ok = tipo == TipoSimbolo.Db
				? valore >= sbyte.MinValue && valore <= byte.MaxValue
				: valore >= short.MinValue && valore <= ushort.MaxValue;
			if (!ok)
				throw new CodiceException(CodiceErrore.CostanteFuoriIntervallo);
			return valore;
		}

		// 'testo': restituisce i caratteri tra gli apici
		string LeggiStringa()
		{
			int inizio = ++_indcar;     // salta l'apice iniziale
			while (_riga[_indcar] != '\'' && _riga[_indcar] != FINE)
				_indcar++;
			if (_riga[_indcar] == FINE || _indcar == inizio)
				throw new CodiceException(CodiceErrore.Formato);
			string testo = _riga.Substring(inizio, _indcar - inizio);
			_indcar++;                  // salta l'apice finale
			return testo;
		}

		// Etichetta all'inizio della riga (identificatore seguito da ':'); null se assente.
		// soloEtichetta: dopo i duepunti non c'è altro, l'etichetta indica l'istruzione successiva.
		public string LeggiEtichetta(string s, out bool soloEtichetta)
		{
			_riga = s;
			_indcar = 0;
			soloEtichetta = false;
			string token = LeggiIdentificatore();
			if (token == null || _riga[_indcar] != ':')
			{
				_indcar = 0;
				return null;
			}
			_indcar++;              // scarta i duepunti
			SaltaSpazi();
			soloEtichetta = SeFine();
			return token;
		}

		// Pre-scansione: etichetta del codice con il numero dell'istruzione a cui punta
		public void DefinisciEtichetta(string nome, string grafia, int istruzione)
		{
			if (Registri.ContainsKey(nome) || CercaOpCode(nome, out _, out _) != -1)
				throw new CodiceException(CodiceErrore.NomeSimboloNonValido);
			if (_etichette.ContainsKey(nome))
				throw new CodiceException(CodiceErrore.SimboloDuplicato);
			_etichette.Add(nome, new Simbolo(nome, TipoSimbolo.Etichetta, istruzione, 0) { Grafia = grafia });
		}

		// Dopo la sezione dati: l'etichetta entra fra i simboli; con lo stesso nome di una variabile
		// o costante resta valido il nome dei dati e l'errore riguarda l'etichetta
		public void AggiungiEtichetta(string nome)
		{
			Simbolo sim = _etichette[nome];
			_etichette.Remove(nome);
			DefinisciSimbolo(sim);
		}

		// numero dell'istruzione indicata dall'etichetta; -1 se il nome non è un'etichetta
		public int CercaEtichetta(string nome)
		{
			return _simboli.TryGetValue(nome, out Simbolo sim) && sim.Tipo == TipoSimbolo.Etichetta ? sim.Valore : -1;
		}

		// jmp/call: forma indiretta se l'operando non è il nome di un'etichetta
		// (registro, [...], variabile, costante); un nome sconosciuto resta un'etichetta
		bool SeSaltoIndiretto()
		{
			string token = TestToken();
			if (token == null)
				return false;
			if (_simboli.TryGetValue(token, out Simbolo sim))
				return sim.Tipo != TipoSimbolo.Etichetta;
			return !(Char.IsLetter(token[0]) || token[0] == '_') || Registri.ContainsKey(token)
				|| token is "offset" or "byte" or "word";
		}

		public Instruction Compila(string s, out string etichetta)
		{
			Instruction istr = CompilaIstruzione(s, out etichetta);
			if (istr != null && Modello == ModelloMemoria.Byte)
				istr.VerificaMemoriaAByte();
			return istr;
		}

		Instruction CompilaIstruzione(string s, out string etichetta)
		{
			int numOp = -1;
			TipoOp tipo;
			Operando op1, op2;
			_inDati = false;
			etichetta = LeggiEtichetta(s, out bool soloEtichetta);
			if (soloEtichetta)
				return null;
			string token = etichetta == null ? LeggiIdentificatore() : EstraiToken();
			string prefisso = null;
			if (token != null && Prefissi.Contains(token))     // rep movsb, repne scasb, ...
			{
				prefisso = token;
				token = EstraiToken();
				bool confronto = token is "cmpsb" or "cmpsw" or "scasb" or "scasw";
				if (token == null || !IstruzioniStringa.Contains(token) || (prefisso != "rep" && !confronto))
					throw new CodiceException(CodiceErrore.PrefissoNonValido);
			}
			if (token == null || CercaOpCode(token, out numOp, out tipo) == -1)
				throw new CodiceException(CodiceErrore.AttesoCodiceIstruzione);

			string code = token;
			if (prefisso != null)
			{
				if (TestToken() != null)
					throw new CodiceException(CodiceErrore.NumeroOperandi);
				return new Instruction(code) { Prefisso = prefisso };
			}
			switch (numOp)
			{
				case 0:
					if (code == "ret" && TestToken() != null)     // ret n: numero di parametri da rimuovere
					{
						op1 = LeggiOperando();
						if (op1.Tipo != TipoOperando.Costante || op1.Scostamento < 0 || TestToken() != null)
							throw new CodiceException(CodiceErrore.OperandoNonValido);
						return new Instruction(code, op1);
					}
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code);

				case 1:
					if (code is "jmp" or "call" && SeSaltoIndiretto())
					{
						op1 = LeggiOperando();      // jmp bx, jmp [tab+bx], call tab
						if (TestToken() != null)
							throw new CodiceException(CodiceErrore.NumeroOperandi);
						return new Instruction(code, op1);
					}
					if (tipo == TipoOp.Codice)
					{
						string salto = EstraiToken();
						if (salto == null || TestToken() != null)
							throw new CodiceException(CodiceErrore.NumeroOperandi);
						return new Instruction(code, salto);
					}
					else
						op1 = LeggiOperando();
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code, op1);

				case 2:
					op1 = LeggiOperando();
					bool siVirgola = LeggiSimbolo(",");
					op2 = LeggiOperando();
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code, op1, op2);
			}
			throw new Exception("l'istruzione non è stata creata");
		}
	}
}
