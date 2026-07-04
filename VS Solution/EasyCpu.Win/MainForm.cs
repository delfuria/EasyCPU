
using EasyCpu;
using EasyCpu.Common;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Backend.Local;
using EasyCpu.Assembler.Processore;
using EasyCpu.CustomControl;
using EasyCpu.Win.Controls;
using System.Windows.Forms;

public partial class MainForm : System.Windows.Forms.Form
{
    string documenti = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\";
    public MainForm()
    {
        InitializeComponent();
        CreaControlli();
        InizializzaAmbiente(true);
        AggiornaDimensioniTabEdit();
        //VersioneVer.VerificaVersione();
    }

    //[STAThread]
    //static void Main(string[] args)
    //{
    //    if (args.Length > 0)
    //        NomeFileIniziale = args[0];

    //    Application.Run(new MainForm());
    //}

    DebugListBox lboDebug;
    int DimPxValoreMemoria;
    int DimPxIndMemoria;
    ListBox lboErrori;
    EasyEditor edtCode;
    EasyEditor edtData;
    int lboMemTopIndex;
    List<CompilerError> errori;
    int NumColonneMemoria;
    IconaCpu iconaCpu;
    static string NomeFileIniziale;
    Font defaultMemFont;
    Dictionary<string, Rectangle> finestre = new Dictionary<string, Rectangle>();


    MemoriaForm _frmMemoria;
    private MemoriaForm FormMemoria
    {
        get
        {
            if (_frmMemoria == null || _frmMemoria.IsDisposed)
            {
                var r = finestre["memoria"];
                _frmMemoria = MemoriaForm.Create("Memoria", () => Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, 8), r);
                _frmMemoria.FontMemoria = new Font(Ambiente.FontEditorNome, Ambiente.FontEditorSize);
                _frmMemoria.F10KeyPressed += menuStep_Click;
            }
            return _frmMemoria;
        }
    }

    MemoriaForm _frmRegs;
    private MemoriaForm FormRegistri
    {
        get
        {
            if (_frmRegs == null || _frmRegs.IsDisposed)
            {
                var r = finestre["registri"];
                _frmRegs = MemoriaForm.Create("Registri", () => DumbRegsAndFlags(), r);
                _frmRegs.FontMemoria = new Font(Ambiente.FontEditorNome, Ambiente.FontEditorSize);
                _frmRegs.F10KeyPressed += menuStep_Click;
            }
            return _frmRegs;
        }
    }

    List<string> DumbRegsAndFlags()
    {
        List<string> regs = new List<string>(Cpu.DumpRegs());
        var s = string.Format("\r\n{0} {1} {2}",
        Cpu.FlagZero ? "Z" : "z",
        Cpu.FlagSegno ? "S" : "s",
        Cpu.FlagOverflow ? "O" : "o");
        regs.Add(s);
        return regs;

    }

    MemoriaForm _frmStack;
    private MemoriaForm FormStack
    {
        get
        {
            if (_frmStack == null || _frmStack.IsDisposed)
            {
                var r = finestre["stack"];

                _frmStack = MemoriaForm.Create("Stack", () => GetStack(), r);
                _frmStack.FontMemoria = new Font(Ambiente.FontEditorNome, Ambiente.FontEditorSize);
                _frmStack.F10KeyPressed += menuStep_Click;
            }
            return _frmStack;
        }
    }

    private List<string> GetStack()
    {
        var stack = Cpu.DumpMemoria(Ram.INDIRIZZO_STACK, Ram.MASSIMO_INDIRIZZO + 1, Ambiente.ColonneStack);
        stack.Reverse();
        return stack;
    }


    #region metodi 


    void CreaControlli()
    {
        edtCode = new EasyEditor(true);
        edtCode.Dock = DockStyle.Fill;
        edtCode.AcceptsTab = true;
        edtCode.SelectionChanged += new EventHandler(PosCursorChanged);

        tabCode.Controls.Add(edtCode);

        edtData = new EasyEditor(false);
        edtData.Dock = DockStyle.Fill;
        edtData.AcceptsTab = true;
        edtData.SelectionChanged += new EventHandler(PosCursorChanged);
        edtData.BackColor = Color.LightCyan;
        tabData.Controls.Add(edtData);

        lboDebug = new DebugListBox();
        lboDebug.DrawMode = DrawMode.OwnerDrawFixed;

        lboDebug.IntegralHeight = false;
        lboDebug.Dock = DockStyle.Fill;
        lboDebug.Visible = false;
        tabCode.Controls.Add(lboDebug);
        
        iconaCpu = new IconaCpu();
        Controls.Add(iconaCpu);
        iconaCpu.BringToFront();
        iconaCpu.Invalidate();

        lboErrori = new ListBox();
        lboErrori.Location = lboMem.Location;
        lboErrori.Size = lboMem.Size;
        lboErrori.Visible = false;
        lboErrori.Anchor = lboMem.Anchor;
        lboErrori.DoubleClick += new EventHandler(lboErrori_DoubleClick);
        pnlMain.Controls.Add(lboErrori);
        defaultMemFont = lboMem.Font;
        AggiornaPosCursore();
    }

    int CalcolaNumColonneMemoria()
    {

        return (lboMem.ClientSize.Width - DimPxIndMemoria - 16) / DimPxValoreMemoria;
    }

    void AggiornaPosCursore()
    {
        EasyEditor edt = (EasyEditor)((tabEdit.SelectedIndex == 0) ? edtCode : edtData);
        PosCursorChanged(edt, null);
    }

    void AggiornaDimPixelMemoria()
    {
        Graphics dc = lboMem.CreateGraphics();

        string formatoDato = "{0" + Ambiente.FD + "} ";
        string formatoIndirizzo = "{0" + Ambiente.FI + "}: ";
        string s = string.Format(formatoDato, 0);
        DimPxValoreMemoria = (int)dc.MeasureString(s, lboMem.Font).Width;

        // !unico modo per far funzionare correttamente il resize!
        if (Ambiente.FormatoDati == FormatoValore.Hex)
            DimPxValoreMemoria = 34;

        s = string.Format(formatoIndirizzo, 0);
        DimPxIndMemoria = (int)dc.MeasureString(s, lboMem.Font).Width;
        NumColonneMemoria = CalcolaNumColonneMemoria();
    }

    void LeggiOpzioniUtente()
    {
        try
        {
            Storage.LeggiOpzioni();
        }
        catch (IOException)
        {
            Dialog.Errore("Impossibile caricare impostazioni. \n Saranno utilizzate le impostazioni predefinite", "Errore lettura file opzioni");
            Ambiente.Inizializza();
        }

    }

    void LeggiFileRecenti()
    {
        try
        {
            Storage.ApriFileRecenti();
        }
        catch (IOException)
        {
            Dialog.Errore("Impossibile leggere nomi file recenti.", "Errore lettura file recenti");
        }

    }



    void InizializzaAmbiente(bool leggiOpzioni)
    {
        //!setting predefinito finestre
        Rectangle r = new Rectangle(100, 100, 400, 300);
        finestre["main"] = new Rectangle(50, 50, 1024, 768);
        finestre["memoria"] = r;
        finestre["registri"] = r;
        finestre["stack"] = r;

        Storage.LeggiStatoFinestre(finestre);
        Bounds = finestre["main"];
        if (leggiOpzioni)
        {
            Ambiente.Inizializza();
            LeggiOpzioniUtente();
        }

        LeggiFileRecenti();
        Ambiente.NomeFile = Ambiente.NomeNuovoFile;

        CreaVociFileRecenti();
        edtCode.Clear();
        // Posiziona il cursore alla colonna del margine sinistro
        int colonna = Ambiente.MargineSinistro;

        // Se il testo è più corto della posizione richiesta, lo estendiamo
        if (edtCode.Text.Length < colonna)
        {
            edtCode.Text = edtCode.Text.PadRight(colonna, ' ');
            // Imposta la posizione del cursore
            edtCode.SelectionStart = colonna;  // -1 perché l'indice è zero-based
            edtCode.SelectionLength = 0;
        }

        edtData.Clear();
        tbuMostraMem.Checked = Ambiente.MostraMemoria;
        //edtCode.Select();
        if (Ambiente.PienoSchermo)
            WindowState = FormWindowState.Maximized;
        else
            WindowState = FormWindowState.Normal;
        AggiornaTitoloForm();

        AggiornaFontControlli();
        AggiornaZoomFactor();
        AggiornaDimPixelMemoria();
        Aggiorna();
        edtCode.Select();
        edtCode.Modified = false;
    }

    void CreaVociFileRecenti()
    {
        if (Ambiente.FileRecenti.Count == 0)
            menuRecenti.Enabled = false;
        else
        {
            menuRecenti.DropDownItems.Clear();
            menuRecenti.Enabled = true;
            foreach (string path in Ambiente.FileRecenti)
            {
                var item = new ToolStripMenuItem(path);
                item.Click += new EventHandler(Recenti_Click);
                menuRecenti.DropDownItems.Add(item);
            }
        }
    }

    void AggiornaRigaPrecedente(int riga)
    {
        riga -= lboDebug.TopIndex;
        Rectangle r = new Rectangle(new Point(0, lboDebug.ItemHeight * riga),
        new Size(lboDebug.Width, lboDebug.ItemHeight));
        lboDebug.Invalidate(r);
    }

    Font CreateFontFromPreferences()
    {
        return new Font(new FontFamily("Courier new"), Ambiente.FontEditorSize, (FontStyle)Ambiente.FontEditorStyle, GraphicsUnit.Pixel);//, Ambiente.FontEditorSize, AmbienteWin.FontEditorStyle);
    }

    void AggiornaZoomFactor()
    {
        edtCode.ZoomFactor = edtData.ZoomFactor = 1.0f;
        edtCode.ZoomFactor = Ambiente.EditorZoomFactor;
        edtData.ZoomFactor = Ambiente.EditorZoomFactor;
    }
    void AggiornaFontControlli()
    {
        lboDebug.Font = CreateFontFromPreferences();
        lboDebug.ItemHeight = lboDebug.Font.Height;
        edtCode.Font = CreateFontFromPreferences();
        edtData.Font = CreateFontFromPreferences();
        AggiornaZoomFactor();
        FormMemoria.FontMemoria = CreateFontFromPreferences();
        FormRegistri.FontMemoria = CreateFontFromPreferences();
        FormStack.FontMemoria = CreateFontFromPreferences();
    }

    void AggiornaComandi()
    {
        if (Ambiente.ColonneStack == 1)
            tbuStack.Image = imgListMain.Images[10];
        else
            tbuStack.Image = imgListMain.Images[9];

        tbuHex.Checked = Ambiente.FormatoDati == FormatoValore.Hex;
        tbuDec.Checked = Ambiente.FormatoDati == FormatoValore.Dec;
        tbuCar.Checked = Ambiente.FormatoDati == FormatoValore.Car;
        menuStop.Enabled = tbuStop.Enabled = lboDebug.Visible;
        //menuEseguiFino.Enabled = lboDebug.Visible == false; 
    }

    void AggiornaRegs()
    {
        int selIndex = lboRegs.SelectedIndex;
        lboRegs.DataSource = Cpu.DumpRegs();
        if (selIndex != -1)
            lboRegs.SelectedIndex = selIndex;
        FormRegistri.Aggiorna();
    }

    void AggiornaMem()
    {
        List<string> mem = Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, NumColonneMemoria);
        if (mem.Count == lboMem.Items.Count)
        {
            bool diverso = false;
            for (int i = 0; i < mem.Count; i++)
                if (mem[i] != (string)lboMem.Items[i])
                    diverso = true;
            if (!diverso)
                return;
        }
        lboMem.SuspendLayout();
        lboMemTopIndex = lboMem.TopIndex;
        lboMem.Items.Clear();
        lboMem.Items.AddRange(mem.ToArray());
        lboMem.TopIndex = lboMemTopIndex;
        lboMem.ResumeLayout();
        FormMemoria.Aggiorna();
    }

    void AggiornaStack()
    {
        List<string> mem = Cpu.DumpMemoria(Ram.INDIRIZZO_STACK, Ram.MASSIMO_INDIRIZZO + 1, Ambiente.ColonneStack);
        if (mem.Count == lboStack.Items.Count)
        {
            bool diverso = false;
            for (int i = 0; i < mem.Count; i++)
                if (mem[i] != (string)lboStack.Items[i])
                    diverso = true;
            if (!diverso)
                return;
        }
        lboStack.SuspendLayout();
        lboStack.DataSource = null;
        lboStack.DataSource = mem;
        lboStack.ResumeLayout();
        FormStack.Aggiorna();
    }

    void AggiornaStato()
    {
        iconaCpu.Invalidate();
        switch (Cpu.Stato)
        {
            case Cpu.StatoCpu.Attiva:
                spnStato.Text = "CPU in esecuzione";
                break;
            case Cpu.StatoCpu.Pronta:
                spnStato.Text = "CPU pronta";
                break;
            case Cpu.StatoCpu.Ferma:
                spnStato.Text = "CPU ferma";
                break;
        }

    }

    void AggiornaFlag(TextBox txtFlag, bool stato)
    {
        if (stato)
        {
            txtFlag.Text = txtFlag.Text.ToUpper();
            txtFlag.ForeColor = Color.Red;
        }
        else
        {
            txtFlag.Text = txtFlag.Text.ToLower();
            txtFlag.ForeColor = Color.Green;
        }
    }

    void AggiornaFlags()
    {
        AggiornaFlag(txtSF, Cpu.FlagSegno);
        AggiornaFlag(txtZF, Cpu.FlagZero);
        AggiornaFlag(txtOF, Cpu.FlagOverflow);
    }

    void Aggiorna()
    {
        AggiornaStato();
        AggiornaRegs();
        AggiornaMem();
        AggiornaStack();
        AggiornaFlags();
        AggiornaComandi();
    }

    void AggiornaTitoloForm()
    {
        string nomeFile = Path.GetFileNameWithoutExtension(Ambiente.NomeFile);
        Text = Ambiente.TitoloForm + " - " + nomeFile;
    }

    void ImpostaDimensioniEdit(bool pieno)
    {
        if (pieno && !lboErrori.Visible)
        {
            tabEdit.Height = tabEdit.Top + lboErrori.Bottom + 1;
        }
        else
        {
            tabEdit.Height = tabEdit.Top + lboErrori.Top - 4;
        }
    }

    void AggiornaDimensioniTabEdit()
    {
        ImpostaDimensioniEdit(!Ambiente.MostraMemoria);
    }

    void DebugMode(bool modo)
    {
        if (modo)
        {
            lboDebug.Visible = true;
            edtCode.Visible = false;
            edtData.Enabled = false;
            lboDebug.DataSource = edtCode.Testo;
            lboDebug.SelectedIndex = Compiler.TabellaDebug[Cpu.IP];
            tabEdit.SelectedIndex = 0;
        }
        else
        {
            edtCode.Visible = true;
            edtData.Enabled = true;
            edtCode.Select();
            lboDebug.Visible = false;
        }
        AggiornaComandi();
    }


    void VisualizzaErrori(List<CompilerError> errori)
    {
        if (errori == null) return;
        int numErrori = (errori.Count < Ambiente.MaxNumErrori) ? errori.Count : Ambiente.MaxNumErrori;
        if (numErrori == 0)
            numErrori = errori.Count;

        lboErrori.Items.Clear();
        for (int i = 0; i < numErrori; i++)
        {
            lboErrori.Items.Add(errori[i].ToString("T", null));
        }
        lboErrori.Visible = true;
        lboMem.Visible = false;
        AggiornaDimensioniTabEdit();
    }

    int TrovaRigaTrap(List<int> tabellaDebug, int riga)
    {
        for (int i = 0; i < tabellaDebug.Count; i++)
        {
            if (riga == tabellaDebug[i] || riga < tabellaDebug[i])
                return i;
        }
        return -1;
    }

    bool InizializzaCpu(int rigaTrap)
    {
        List<Instruction> istruzioni;
        errori = null;
        List<int> memoria;

        try
        {
            List<string> testo = edtCode.Testo;
            rigaTrap = edtCode.TraslaRigaTrap(rigaTrap);
            istruzioni = Compiler.CompilaCodice(testo, ref errori, rigaTrap);
            memoria = Compiler.CompilaDati(edtData.Testo, ref errori);
        }
        catch (Exception)
        {
            Dialog.Errore("Impossibile compilare il codice", "Errore compilazione");
            errori = null;
            return false;
        }
        if (errori != null)
        {
            VisualizzaErrori(errori);
            return false;
        }
        if (istruzioni == null)     // verifica se c'? codice
            return false;
        Cpu.Init(istruzioni, memoria, Ambiente.InizializzaRegistri, Ambiente.LoopInfinito);
        lboErrori.Visible = false;
        lboMem.Visible = true;
        edtData.Enabled = false;
        return true;
    }

    void Step()
    {
        if (Cpu.Stato == Cpu.StatoCpu.Ferma)
        {
            if (!InizializzaCpu(-1))
                return;
            DebugMode(true);
            lboDebug.SelectedIndex = Compiler.TabellaDebug[Cpu.IP];
            Aggiorna();
            return;
        }
        int rigaPrecedente = Compiler.TabellaDebug[Cpu.IP];
        try
        {
            Cpu.Debug();
        }
        catch (CpuException ex)
        {
            Dialog.Errore(ex.err, "Errore di esecuzione");
            DebugMode(false);
            PosizionaRigaErrore(Compiler.TabellaDebug[Cpu.IP], edtCode);
        }
        catch (Exception ex)
        {
            Dialog.Errore(ex.Message, "Errore di esecuzione sconosciuto");
            DebugMode(false);
            PosizionaRigaErrore(Compiler.TabellaDebug[Cpu.IP], edtCode);
        }

        if (Cpu.Stato == Cpu.StatoCpu.Ferma)
        {
            DebugMode(false);
            edtCode.Select();
        }
        else
        {
            lboDebug.SelectedIndex = Compiler.TabellaDebug[Cpu.IP];
            //lboDebug.Invalidate(); 
            AggiornaRigaPrecedente(rigaPrecedente);
        }
        Aggiorna();
    }

    // il parametro trap ? usato in via sperimentale
    void Run(int rigaTrap, bool trap)
    {
        if (Cpu.Stato == Cpu.StatoCpu.Ferma)
        {
            if (!InizializzaCpu(rigaTrap))
            {
                return;
            }
            trap = false; // evita un nuovo trapping se questo ? gi? stato impostato
        }

        // ! codice sperimentale !
        // verificare funzionamento se ? impostata Ambiente.MostraSoloCodice
        if (trap && (Cpu.Stato == Cpu.StatoCpu.Attiva || Cpu.Stato == Cpu.StatoCpu.Pronta))
        {

            int riga = TrovaRigaTrap(Compiler.TabellaDebug, lboDebug.SelectedIndex);
            if (riga != -1)
                Cpu.SetTrap(riga);
        }
        // ! fine codice sperimentale

        DebugMode(false);
        bool riparti;
        do
        {
            riparti = false;
            try
            {
                Cpu.Run(Cpu.IP);
            }

            catch (CpuTrapException) // gestione trap
            {
                DebugMode(true);
                Aggiorna();
                return;
            }

            catch (CpuLoopException) // gestione loop infinito
            {
                AggiornaStato();
                SospendiForm sf = new SospendiForm();
                sf.ShowDialog();
                switch (sf.Modo)
                {
                    case ModoSospendi.Pausa:
                        DebugMode(true);
                        Aggiorna();
                        return;
                    case ModoSospendi.Arresta:
                        Cpu.Stop();
                        break;
                    case ModoSospendi.Continua:
                        riparti = true;
                        break;
                }
            }

            catch (CpuException ex)
            {
                Dialog.Errore(ex.err, "Errore di esecuzione");
                if (!Cpu.IPOverRun)
                    PosizionaRigaErrore(Compiler.TabellaDebug[Cpu.IP], edtCode);
            }
            catch (Exception ex)
            {
                Dialog.Errore(ex.Message, "Errore di esecuzione sconosciuto");
                if (!Cpu.IPOverRun)
                    PosizionaRigaErrore(Compiler.TabellaDebug[Cpu.IP], edtCode);
            }
        } while (riparti);
        edtData.Enabled = true;
        edtCode.Select();
        Aggiorna();
    }

    void PosizionaRigaErrore(int numRiga, EasyEditor editor)
    {
        PosizionaRigaErrore(numRiga, 0, editor);
    }

    void PosizionaRigaErrore(int numRiga, int numColonna, EasyEditor editor)
    {

        if (editor == edtData)
            tabEdit.SelectedIndex = 1;
        else
            tabEdit.SelectedIndex = 0;
        editor.SelectionLength = 0;
        editor.Select();
        editor.LineaToCaret(numRiga, numColonna);
    }

    void LeggiDati(string nome)
    {
        edtCode.Lines = null;
        try
        {
            List<string> codice;
            List<string> dati;
            Storage.Apri(nome, out codice, out dati);
            if (codice != null)
                edtCode.Lines = codice.ToArray();
            if (dati != null)
                edtData.Lines = dati.ToArray();
        }
        catch (IOException ex)
        {
            Dialog.Errore("Impossibile aprire il file.\nEccezione: " + ex.Message, "Errore apertura file");
            return;
        }
        Ambiente.NomeFile = nome;
        Ambiente.AggiungiRecenti(nome);
        CreaVociFileRecenti();
        AggiornaTitoloForm();
        AggiornaZoomFactor();
    }

    bool SalvaDati(bool chiediNome)
    {
        string nome = Ambiente.NomeFile;
        if (chiediNome)
        {
            SaveFileDialog dgSalva = new SaveFileDialog();

            dgSalva.Filter = Ambiente.FiltroFileDialog;
            dgSalva.CheckFileExists = false;
            dgSalva.FileName = nome;
            dgSalva.InitialDirectory = Storage.CartellaIniziale();
            DialogResult cmd = dgSalva.ShowDialog();
            if (cmd != DialogResult.OK)
                return false;
            nome = dgSalva.FileName;
            Ambiente.PathCorrente = Path.GetDirectoryName(nome);
        }

        try
        {
            Storage.Salva(nome, edtCode.Lines, edtData.Lines);
        }
        catch (IOException ex)
        {
            Dialog.Errore("Impossibile salvare i dati.\nEccezione:" + ex.Message, "Errore salvataggio file");
            return false;
        }
        Ambiente.NomeFile = nome;
        AggiornaTitoloForm();
        edtCode.Modified = false;
        edtData.Modified = false;
        Ambiente.AggiungiRecenti(nome);
        CreaVociFileRecenti();
        return true;
    }

    // ritorna true se si pu? procedere
    bool ChiedeSalvataggioFile()
    {
        DialogResult dr = Dialog.Attenzione("Il file attuale ? stato modificato.\nVuoi salvare le modifiche?", "Salvataggio modifiche");
        if (dr == DialogResult.No)
            return true;
        else
        if (dr == DialogResult.Cancel)
            return false;

        if (dr == DialogResult.Yes)
        {
            if (!SalvaDati(Ambiente.NomeFile == Ambiente.NomeNuovoFile))
            {
                dr = Dialog.Attenzione("Il file non ? stato salvato.\nDesideri procedere comunque?", "Nuovo file");
                if (dr == DialogResult.Yes)
                    return true;
            }
            return true;
        }
        return false;
    }


    #endregion

    private void menuRun_Click(object sender, System.EventArgs e)
    {
        Run(-1, false);
    }

    private void menuStep_Click(object sender, System.EventArgs e)
    {
        var f = edtCode.Font;
        lboDebug.Font = new Font(f.Name, f.Size * edtCode.ZoomFactor, edtCode.Font.Style);
        lboDebug.ItemHeight = lboDebug.Font.Height;
        Step();
    }

    private void menuStop_Click(object sender, System.EventArgs e)
    {
        Cpu.Stop();
        DebugMode(false);
        Aggiorna();
    }

    private void menuNuovo_Click(object sender, System.EventArgs e)
    {
        if (edtCode.Modified || edtData.Modified)
        {
            if (!ChiedeSalvataggioFile())
                return;
        }
        InizializzaAmbiente(false);

    }

    private void menuSalva_Click(object sender, System.EventArgs e)
    {
        bool ok = false;
        if (Ambiente.NomeFile == Ambiente.NomeNuovoFile)
            ok = SalvaDati(true);
        else
            ok = SalvaDati(false);
        if (ok)
            AggiornaTitoloForm();
    }

    private void menuApri_Click(object sender, System.EventArgs e)
    {
        if (edtCode.Modified || edtData.Modified)
        {
            if (!ChiedeSalvataggioFile())
                return;
        }

        OpenFileDialog dgApri = new OpenFileDialog();
        dgApri.InitialDirectory = Storage.CartellaIniziale();
        dgApri.Filter = Ambiente.FiltroFileDialog;
        dgApri.CheckFileExists = true;
        DialogResult cmd = dgApri.ShowDialog();

        if (cmd != DialogResult.OK)
            return;
        Ambiente.PathCorrente = Path.GetDirectoryName(dgApri.FileName);

        LeggiDati(dgApri.FileName);
    }

    private void menuEsci_Click(object sender, System.EventArgs e)
    {
        Close();
    }

    private void menuSalvaCome_Click(object sender, System.EventArgs e)
    {
        if (SalvaDati(true))
            AggiornaTitoloForm();
    }

    private void Recenti_Click(object sender, System.EventArgs e)
    {
        if (edtCode.Modified || edtData.Modified)
        {
            if (!ChiedeSalvataggioFile())
                return;
        }
        ToolStripItem m = (ToolStripItem)sender;
        LeggiDati(m.Text);
    }

    void lboErrori_DoubleClick(object sender, EventArgs e)
    {
        int indice = lboErrori.SelectedIndex;
        if (indice == -1) return;
        int riga = errori[indice].Riga;
        if (riga == -1)
            return;
        int colonna = errori[indice].Colonna;
        int tipo = errori[indice].Tipo;
        if (tipo == CompilerError.CODICE)
            PosizionaRigaErrore(riga, colonna, edtCode);
        else
            PosizionaRigaErrore(riga, colonna, edtData);
    }

    private void menuInfo_Click(object sender, System.EventArgs e)
    {
        //Global.ShowDefaultAboutDialog(); 
    }

    private void menuOpzioni_Click(object sender, System.EventArgs e)
    {
        OpzioniForm of = new OpzioniForm();
        DialogResult cmd = of.ShowDialog();
        if (cmd == DialogResult.OK)
            Aggiorna();
        of.Dispose();
    }

    private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = false;
        if (edtCode.Modified || edtData.Modified)
        {
            if (!ChiedeSalvataggioFile())
                e.Cancel = true;
        }
        if (e.Cancel == false)
            if (menuSalvaOpzioni.Checked)
            {
                Ambiente.PienoSchermo = WindowState == FormWindowState.Maximized;
                Ambiente.EditorZoomFactor = edtCode.ZoomFactor;
                try
                {
                    Storage.SalvaOpzioni();
                    SalvaStatoFinestre();
                }
                catch (IOException)
                {
                    Dialog.Errore("Impossibile salvare le opzioni", "EasyCPU");
                }
            }
        try
        {
            Storage.SalvaFileRecenti();
        }
        catch (IOException)
        {
            Dialog.Errore("Impossibile nomi file recenti", "EasyCPU");
        }
    }

    private void SalvaStatoFinestre()
    {
        var finestre = new Dictionary<string, Rectangle>();
        finestre.Add("main", this.Bounds);
        finestre.Add("memoria", FormMemoria.Bounds);
        finestre.Add("registri", FormRegistri.Bounds);
        finestre.Add("stack", FormStack.Bounds);
        Storage.SalvaStatoFinestre(finestre);
    }

    private void menuSalvaOpzioni_Click(object sender, System.EventArgs e)
    {
        menuSalvaOpzioni.Checked = !menuSalvaOpzioni.Checked;
    }

    private void Form1_Resize(object sender, System.EventArgs e)
    {
        Ambiente.PienoSchermo = WindowState == FormWindowState.Maximized;
        if (WindowState == FormWindowState.Minimized || DimPxIndMemoria == 0) return;
        int tmp = CalcolaNumColonneMemoria();

        if (NumColonneMemoria != tmp)
        {
            NumColonneMemoria = tmp;
            AggiornaMem();
        }
    }

    private void menuEseguiFino_Click(object sender, System.EventArgs e)
    {
        Run(edtCode.CaretToLinea(), true);
    }

    private void lboRegs_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        string s = Cpu.DumpReg(lboRegs.SelectedIndex);
        spnReg.Text = s;
    }

    private void menuFontEdit_Click(object sender, System.EventArgs e)
    {
        FontDialog fd = new FontDialog();
        fd.Font = new Font(Ambiente.FontEditorNome, Ambiente.FontEditorSize);
        DialogResult cmd = fd.ShowDialog();
        if (cmd == DialogResult.OK)
        {
            Ambiente.FontEditorNome = fd.Font.Name;
            Ambiente.FontEditorSize = fd.Font.Size;
            Ambiente.FontEditorStyle = (int) fd.Font.Style;
            AggiornaFontControlli();
        }
    }

    private void tabEdit_SelectedIndexChanged(object sender, System.EventArgs e)
    {
        if (tabEdit.SelectedIndex == 0)
            edtCode.Select();
        else
            edtData.Select();
        AggiornaPosCursore();
    }

    private void Form1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.F6)
            if (tabEdit.SelectedIndex == 0)
                tabEdit.SelectedIndex = 1;
            else
                tabEdit.SelectedIndex = 0;
    }

    private void PosCursorChanged(object sender, EventArgs e)
    {
        EasyEditor edt = (EasyEditor)sender;
        spnEditor.Text = "Riga: " + (edt.CaretToLinea() + 1).ToString();
    }

    private void Form1_Load(object sender, System.EventArgs e)
    {
        if (NomeFileIniziale != null)
            LeggiDati(NomeFileIniziale);
    }

    private void lboMem_DoubleClick(object sender, EventArgs e)
    {
        FormMemoria.Visible = !FormMemoria.Visible;
    }

    private void lboStack_DoubleClick(object sender, EventArgs e)
    {
        FormStack.Visible = !FormStack.Visible;
    }

    private void lboRegs_DoubleClick(object sender, EventArgs e)
    {
        FormRegistri.Visible = !FormRegistri.Visible;
    }

    private void tbuDec_CheckStateChanged(object sender, EventArgs e)
    {
        if (tbuDec.Checked)
        {
            tbuCar.Checked = false;
            tbuHex.Checked = false;

            Ambiente.FormatoDati = FormatoValore.Dec;
            AggiornaDimPixelMemoria();
            Form1_Resize(null, EventArgs.Empty);
        }
        Aggiorna();
    }

    private void tbuHex_CheckStateChanged(object sender, EventArgs e)
    {
        if (tbuHex.Checked)
        {
            tbuCar.Checked = false;
            tbuDec.Checked = false;

            Ambiente.FormatoDati = FormatoValore.Hex;
            AggiornaDimPixelMemoria();
            Form1_Resize(null, EventArgs.Empty);
        }
        Aggiorna();
    }

    private void tbuCar_CheckStateChanged(object sender, EventArgs e)
    {
        if (tbuCar.Checked)
        {
            tbuHex.Checked = false;
            tbuDec.Checked = false;

            Ambiente.FormatoDati = FormatoValore.Car;
            AggiornaDimPixelMemoria();
            Form1_Resize(null, EventArgs.Empty);
        }
        Aggiorna();
    }

    private void tbuMostraMem_CheckStateChanged(object sender, EventArgs e)
    {
        Ambiente.MostraMemoria = ((ToolStripButton)sender).Checked;
        AggiornaDimensioniTabEdit();
    }

    private void tbuStack_CheckStateChanged(object sender, EventArgs e)
    {

        if (Ambiente.ColonneStack == 1)
        {
            Ambiente.ColonneStack = 2;
            tbuStack.Image = imgListMain.Images[9];
        }
        else
        {
            Ambiente.ColonneStack = 1;
            tbuStack.Image = imgListMain.Images[10];
        }
        Aggiorna();
    }

    private void tbuNuovo_Click(object sender, EventArgs e)
    {
        menuNuovo.PerformClick();
    }

    private void tbuApri_Click(object sender, EventArgs e)
    {
        menuApri.PerformClick();


    }

    private void tbuSalva_Click(object sender, EventArgs e)
    {
        menuSalva.PerformClick();
    }

    private void tbuEsegui_Click(object sender, EventArgs e)
    {
        menuEsegui.PerformClick();
    }

    private void tbuStep_Click(object sender, EventArgs e)
    {
        menuEseguiIstr.PerformClick();
    }

    private void tbuStop_Click(object sender, EventArgs e)
    {
        menuStop.PerformClick();
    }
}

