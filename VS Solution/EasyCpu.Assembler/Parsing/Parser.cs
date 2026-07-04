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
		};

		public const char FINE = '\0';
		string _riga;
		int _indcar;

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

		void LeggiOperandoIndiretto(out IdOp op, out int offset)
		{
			offset = 0;
			string token = TestToken();
			switch (token)
			{
				case "si": op = IdOp._si; break;
				case "di": op = IdOp._di; break;
				case "bx": op = IdOp._bx; break;
				case "bp": op = IdOp._bp; break;
				default:
					offset = LeggiValore();
					op = IdOp.Memoria;
					if (!IntervalloOk(op, offset))
						throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);
					token = EstraiToken();
					if (token != "]")
						throw new CodiceException(CodiceErrore.AttesaQuadraChiusura);
					return;
			}
			EstraiToken();      // scarta registro precedentemente testato
			token = EstraiToken();
			switch (token)
			{
				case "+":
				case "-":
					offset = LeggiValore();
					if (token == "-")
						offset *= -1;
					if (EstraiToken() != "]")
						throw new CodiceException(CodiceErrore.AttesaQuadraChiusura);
					break;
				case "]": break;
				default: throw new CodiceException(CodiceErrore.Sintassi);
			}
		}

		static bool IntervalloOk(IdOp op, int offset)
		{
			return !(op == IdOp.Memoria && (offset < 0 || offset > Ram.MASSIMO_INDIRIZZO));
		}

		void LeggiOperando(out IdOp op, out int offset)
		{
			offset = 0;
			op = IdOp.Null;
			string token = TestToken();
			if (token == "[")
			{
				EstraiToken();      // scarta parentesi quadra
				LeggiOperandoIndiretto(out op, out offset);
				return;
			}
			else
				switch (token)
				{
					case "ax": op = IdOp.ax; break;
					case "bx": op = IdOp.bx; break;
					case "cx": op = IdOp.cx; break;
					case "dx": op = IdOp.dx; break;
					case "si": op = IdOp.si; break;
					case "di": op = IdOp.di; break;
					case "bp": op = IdOp.bp; break;
					case "sp": op = IdOp.sp; break;
					case "'": goto default;     // costante carattere
					default:
						op = IdOp.Costante;
						offset = LeggiValore();
						if (!IntervalloOk(op, offset))
							throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);
						return;
				}
			EstraiToken();  // scarta registro
		}

		public List<int> CompilaDati(string s, int indice, out int indirizzo)
		{
			indirizzo = 0;
			_riga = s;
			_indcar = 0;
			indirizzo = LeggiIndirizzo();
			if (EstraiToken() != ":")
				throw new CodiceException(CodiceErrore.AttesoDuePunti);

			if (indirizzo < 0 || indirizzo > Ram.MASSIMO_INDIRIZZO)
				throw new CodiceException(CodiceErrore.IntervalloIndirizzoDati);

			return LeggiValori();
		}

		public Instruction Compila(string s, out string etichetta)
		{
			etichetta = null;
			int numOp = -1;
			TipoOp tipo;
			int offset1, offset2;
			IdOp op1, op2;
			_indcar = 0;
			_riga = s;
			string token = LeggiIdentificatore();
			if (token != null && _riga[_indcar] == ':')
			{
				etichetta = token;
				EstraiToken();          // scarta i duepunti
				token = EstraiToken();
				if (token == null)      // c'è solo l'etichetta
					return null;
			}
			if (token == null || CercaOpCode(token, out numOp, out tipo) == -1)
				throw new CodiceException(CodiceErrore.AttesoCodiceIstruzione);

			string code = token;
			switch (numOp)
			{
				case 0:
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code);

				case 1:
					if (tipo == TipoOp.Codice)
					{
						string salto = EstraiToken();
						if (salto == null || TestToken() != null)
							throw new CodiceException(CodiceErrore.NumeroOperandi);
						return new Instruction(code, salto);
					}
					else
						LeggiOperando(out op1, out offset1);
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code, op1, offset1);

				case 2:
					LeggiOperando(out op1, out offset1);
					bool siVirgola = LeggiSimbolo(",");
					LeggiOperando(out op2, out offset2);
					if (TestToken() != null)
						throw new CodiceException(CodiceErrore.NumeroOperandi);
					return new Instruction(code, op1, op2, offset1, offset2);
			}
			throw new Exception("l'istruzione non è stata creata");
		}
	}
}
