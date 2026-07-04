using System;
using System.IO;
using System.Collections.Generic;


namespace EasyCpu.Common
{
	public enum FormatoValore
	{
		Dec,
		Hex,
		Car
	}

	public class Ambiente
	{
		public const string TitoloForm = "Easy Cpu";
		public const string Versione = "1.00";
		public const string NOMEFILENUOVO = "file1.as";
		public const string NOMEFILEOPZIONI = "opzioni.json";
		public const string NOMEFILEFINESTRE = "Finestre.txt";
		public const string PATHPROGETTI = "EasyCPU Progetti";
		public const string NOMEFILERECENTI = "recenti.json";

		public const string FiltroFileDialog = "Easy CPU assembly (*.as)|*.as|Tutti i file (*.*)|*.*";

		public static string DocumentiPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		public static string EasyCPUPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyCPU");
		public static string CurDir = Environment.CurrentDirectory;
		public static string NomeNuovoFile = NOMEFILENUOVO;
		public static string PathCorrente = "";

		public static string OpzioniNomeFile = Path.Combine(EasyCPUPath, NOMEFILEOPZIONI);
		public static string FinestreNomeFile = Path.Combine(EasyCPUPath, NOMEFILEFINESTRE);
		public static string RecentiNomeFile = Path.Combine(EasyCPUPath, NOMEFILERECENTI);
		public static string ProgettiPath = Path.Combine(DocumentiPath, PATHPROGETTI);
		public static bool PrimoTentativo = true;

		// opzioni configurabili, memorizzate nel profilo dell'utente

		public static bool PienoSchermo;

		public static int MaxNumErrori;

		public static int ColonneStack;

		// ! obsoleto !
		public static bool MostraSoloCodice;
		public static string CHIAVE_SOLOCODICE = "SOLO_CODICE";

		public static bool InizializzaRegistri;

		public static int LoopInfinito;

		public static int MargineSinistro;

		static FormatoValore formatoDati;

		public static string FormatoCarZero;

		public static bool MostraMemoria;

		public static string FontEditorNome;

		public static float FontEditorSize;

		public static int FontEditorStyle;

		public static float EditorZoomFactor;

		public static float FontPanelliSize;

		public static string VersioneAssembly;

		public static string FI;
		public static string FD;
		public static string FR;
		public const string FRHEX = ":X4";
		public const string FRDEC = ":0";

		public static string NomeFile;

		public const int MAXFILERECENTI = 10;
		public static List<string> FileRecenti = new List<string>();

		// imposta i valori iniziali delle opzioni configurabili
		public static void Inizializza()
		{
			ColonneStack = 1;
			MostraSoloCodice = false;
			FormatoDati = FormatoValore.Dec;
			MaxNumErrori = 5;
			MostraMemoria = true;
			FontEditorNome = "Courier new";
			FontEditorSize = 14;
			FontEditorStyle = 0;
			EditorZoomFactor = 1.0f;
			FontPanelliSize = 12f;
			InizializzaRegistri = true;
			LoopInfinito = 65535;
			PienoSchermo = false;
			VersioneAssembly = "";
			FormatoCarZero = "\\0";
			MargineSinistro = 7;
		}

		public static FormatoValore FormatoDati
		{
			get { return formatoDati; }
			set
			{
				formatoDati = value;
				if (formatoDati == FormatoValore.Hex)
				{
					FI = ":X4";
					FD = ":X4";
					FR = ":X4";
				}
				else
				{
					FI = ",4:0";
					FD = ",5:0";
					FR = ",5:0";
				}
			}
		}

		public static void AggiungiRecenti(string path)
		{
			int pos = FileRecenti.IndexOf(path);
			if (pos != -1)
				FileRecenti.RemoveAt(pos);
			FileRecenti.Insert(0, path);
			while (FileRecenti.Count > MAXFILERECENTI)
				FileRecenti.RemoveAt(FileRecenti.Count - 1);
		}
	}
}
