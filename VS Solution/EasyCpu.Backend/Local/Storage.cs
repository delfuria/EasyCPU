using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EasyCpu.Common;


namespace EasyCpu.Backend.Local
{

    public static class Storage
    {
        static string PREF_DATA = ".DATA";

        // Archivio delle impostazioni: file sul Desktop (predefinito), localStorage nel browser
        // (impostato da EasyCPU.Browser prima dell'avvio dell'app).
        public static IArchivioImpostazioni Archivio { get; set; } = new ArchivioFile();

        public static void Salva(string nome, string[] codice, string[] dati)
        {
            StreamWriter sw = new StreamWriter(nome);
            if (codice != null)
            {
                for (int i = 0; i < codice.Length; i++)
                    sw.WriteLine(codice[i]);
            }

            if (dati != null)
            {
                sw.WriteLine(PREF_DATA);
                for (int i = 0; i < dati.Length; i++)
                    sw.WriteLine(dati[i]);
            }
            sw.Close();
        }

        public static void Apri(string nome, out List<string> codice, out List<string> dati)
        {
            using var stream = File.OpenRead(nome);
            Apri(stream, out codice, out dati);
        }

        public static void Apri(Stream input, out List<string> codice, out List<string> dati)
        {
            codice = new List<string>();
            dati = new List<string>();
            StreamReader sr = new StreamReader(input);

            string riga = sr.ReadLine();
            while (riga != null && riga.Trim().ToUpper() != PREF_DATA)
            {
                codice.Add(riga);
                riga = sr.ReadLine();
            }
            if (riga == PREF_DATA)
            {
                riga = sr.ReadLine();
                while (riga != null)
                {
                    dati.Add(riga);
                    riga = sr.ReadLine();
                }
            }

            sr.Close();
        }

        public static void SalvaOpzioni()
        {
            Ambiente.VersioneAssembly = "";
            var dto = new OpzioniDto(
                Ambiente.FormatoDati,
                Ambiente.FormatoCarZero,
                Ambiente.MaxNumErrori,
                Ambiente.ColonneStack,
                Ambiente.InizializzaRegistri,
                Ambiente.LoopInfinito,
                Ambiente.MostraMemoria,
                Ambiente.FontEditorNome,
                Ambiente.FontEditorSize,
                Ambiente.FontEditorStyle,
                Ambiente.EditorZoomFactor,
                Ambiente.PienoSchermo,
                Ambiente.VersioneAssembly,
                Ambiente.FontPanelliSize,
                Ambiente.MargineSinistro);
            var json = JsonSerializer.Serialize(dto, SettingsJsonContext.Default.OpzioniDto);
            Archivio.Scrivi("opzioni", json);
        }

        public static void LeggiOpzioni()
        {
            var json = Archivio.Leggi("opzioni");
            if (json == null)
                return;
            var dto = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.OpzioniDto);
            if (dto == null)
                return;

            Ambiente.FormatoDati = dto.formatoDati;
            Ambiente.FormatoCarZero = dto.formatoCarZero;
            Ambiente.MaxNumErrori = dto.maxNumErrori;
            Ambiente.ColonneStack = dto.colonneStack;
            Ambiente.InizializzaRegistri = dto.inizializzaRegistri;
            Ambiente.LoopInfinito = dto.loopInfinito;
            Ambiente.MargineSinistro = dto.margineSinistro;
            if (Ambiente.MargineSinistro < 0 || Ambiente.MargineSinistro > 15)
                Ambiente.MargineSinistro = 0;
            Ambiente.PienoSchermo = dto.pienoSchermo;
            Ambiente.MostraMemoria = dto.mostraMemoria;
            Ambiente.FontEditorNome = dto.fontEditorNome;
            Ambiente.FontEditorSize = dto.fontEditorSize;
            Ambiente.EditorZoomFactor = dto.editorZoomFactor;
            Ambiente.FontEditorStyle = dto.fontEditorStyle;
            Ambiente.VersioneAssembly = dto.versioneAssembly;
            Ambiente.FontPanelliSize = dto.fontPanelliSize;
        }

        public static void SalvaFileRecenti()
        {
            var dto = new RecentiDto(1, Ambiente.FileRecenti.Take(Ambiente.MAXFILERECENTI).ToArray());
            var json = JsonSerializer.Serialize(dto, SettingsJsonContext.Default.RecentiDto);
            Archivio.Scrivi("recenti", json);
        }

        public static void ApriFileRecenti()
        {
            var json = Archivio.Leggi("recenti");
            if (json == null) return;
            var dto = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.RecentiDto);
            Ambiente.FileRecenti = (dto?.files ?? [])
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Take(Ambiente.MAXFILERECENTI)
                .ToList();
        }

        public static bool CreaPathProgetti()
        {
            if (Directory.Exists(Ambiente.ProgettiPath))
                return true;
            try
            {
                Directory.CreateDirectory(Ambiente.ProgettiPath);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string CartellaIniziale()
        {
            Storage.ApriFileRecenti();
            if (Storage.CreaPathProgetti() && Ambiente.PathCorrente == "")
                return Ambiente.ProgettiPath;
            else
                return Ambiente.PathCorrente;
        }
    }
}
