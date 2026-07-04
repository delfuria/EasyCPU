using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public partial class MainForm
{
    private System.Windows.Forms.MenuStrip menuStrip1;
    private System.Windows.Forms.ToolStripMenuItem menuDebug;
    private System.ComponentModel.IContainer components;
    private System.Windows.Forms.ToolStripMenuItem menuStop;
    private System.Windows.Forms.ToolStripMenuItem menuFile;
    private System.Windows.Forms.ToolStripMenuItem menuNuovo;
    private System.Windows.Forms.ToolStripMenuItem menuApri;
    private System.Windows.Forms.ToolStripMenuItem menuSalva;
    private System.Windows.Forms.ToolStripMenuItem menuEsci;
    private System.Windows.Forms.Panel pnlMain;
    private System.Windows.Forms.ListBox lboRegs;
    private System.Windows.Forms.ListBox lboStack;
    private System.Windows.Forms.ListBox lboMem;
    private System.Windows.Forms.ToolStripMenuItem menuSalvaCome;
    private System.Windows.Forms.ToolStrip tbaMain;
    private System.Windows.Forms.ToolStripButton tbuNuovo;
    private System.Windows.Forms.ImageList imgListMain;
    private System.Windows.Forms.ToolStripButton tbuApri;
    private System.Windows.Forms.ToolStripButton tbuSalva;
    private System.Windows.Forms.StatusStrip staMain;
    private System.Windows.Forms.ToolStripStatusLabel spnStato;
    private System.Windows.Forms.TabControl tabEdit;
    private System.Windows.Forms.TabPage tabCode;
    private System.Windows.Forms.TabPage tabData;
    private System.Windows.Forms.ToolStripSeparator toolBarSeparator1;
    private System.Windows.Forms.ToolStripButton tbuEsegui;
    private System.Windows.Forms.ToolStripButton tbuStop;
    private System.Windows.Forms.ToolStripButton tbuStep;
    private System.Windows.Forms.ToolStripMenuItem menuEsegui;
    private System.Windows.Forms.ToolStripSeparator toolBarSeparator2;
    private System.Windows.Forms.ToolStripButton tbuHex;
    private System.Windows.Forms.ToolStripButton tbuDec;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox txtSF;
    private System.Windows.Forms.TextBox txtZF;
    private System.Windows.Forms.TextBox txtOF;
    private System.Windows.Forms.ToolStripButton tbuMostraMem;
    private System.Windows.Forms.ToolStripMenuItem menuRecenti;
    private System.Windows.Forms.ToolStripMenuItem menuPreferenze;
    private System.Windows.Forms.ToolStripMenuItem menuOpzioni;
    private System.Windows.Forms.ToolStripMenuItem menuSalvaOpzioni;
    private System.Windows.Forms.ToolStripMenuItem menuEseguiFino;
    private System.Windows.Forms.ToolStripMenuItem menuEseguiIstr;
    private System.Windows.Forms.ToolStripMenuItem menuFontEdit;
    private System.Windows.Forms.ToolStripMenuItem menuItem4;
    private System.Windows.Forms.ToolStripMenuItem menuInfo;
    private System.Windows.Forms.ToolStripButton tbuStack;
    private System.Windows.Forms.ToolStripStatusLabel spnIcon;
    private System.Windows.Forms.PictureBox picCpu;
    private System.Windows.Forms.ToolStripButton tbuCar;
    private System.Windows.Forms.ToolStripStatusLabel spnEditor;
    private System.Windows.Forms.ToolStripStatusLabel spnReg;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (components != null)
            {
                components.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
        menuStrip1 = new MenuStrip();
        menuFile = new ToolStripMenuItem();
        menuNuovo = new ToolStripMenuItem();
        menuApri = new ToolStripMenuItem();
        menuSalva = new ToolStripMenuItem();
        menuSalvaCome = new ToolStripMenuItem();
        toolStripSeparator3 = new ToolStripSeparator();
        menuRecenti = new ToolStripMenuItem();
        toolStripSeparator4 = new ToolStripSeparator();
        menuEsci = new ToolStripMenuItem();
        menuDebug = new ToolStripMenuItem();
        menuEsegui = new ToolStripMenuItem();
        menuEseguiFino = new ToolStripMenuItem();
        menuEseguiIstr = new ToolStripMenuItem();
        toolStripSeparator2 = new ToolStripSeparator();
        menuStop = new ToolStripMenuItem();
        menuPreferenze = new ToolStripMenuItem();
        menuOpzioni = new ToolStripMenuItem();
        menuFontEdit = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        menuSalvaOpzioni = new ToolStripMenuItem();
        menuItem4 = new ToolStripMenuItem();
        menuInfo = new ToolStripMenuItem();
        imgListMain = new ImageList(components);
        tbaMain = new ToolStrip();
        tbuNuovo = new ToolStripButton();
        tbuApri = new ToolStripButton();
        tbuSalva = new ToolStripButton();
        toolBarSeparator1 = new ToolStripSeparator();
        tbuEsegui = new ToolStripButton();
        tbuStop = new ToolStripButton();
        tbuStep = new ToolStripButton();
        toolBarSeparator2 = new ToolStripSeparator();
        tbuHex = new ToolStripButton();
        tbuDec = new ToolStripButton();
        tbuCar = new ToolStripButton();
        tbuMostraMem = new ToolStripButton();
        tbuStack = new ToolStripButton();
        staMain = new StatusStrip();
        spnIcon = new ToolStripStatusLabel();
        spnStato = new ToolStripStatusLabel();
        spnEditor = new ToolStripStatusLabel();
        spnReg = new ToolStripStatusLabel();
        pnlMain = new Panel();
        picCpu = new PictureBox();
        txtOF = new TextBox();
        txtZF = new TextBox();
        txtSF = new TextBox();
        label1 = new Label();
        tabEdit = new TabControl();
        tabCode = new TabPage();
        tabData = new TabPage();
        lboStack = new ListBox();
        lboRegs = new ListBox();
        lboMem = new ListBox();
        label2 = new Label();
        menuStrip1.SuspendLayout();
        tbaMain.SuspendLayout();
        staMain.SuspendLayout();
        pnlMain.SuspendLayout();
        ((ISupportInitialize)picCpu).BeginInit();
        tabEdit.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip1
        // 
        menuStrip1.ImageScalingSize = new Size(24, 24);
        menuStrip1.Items.AddRange(new ToolStripItem[] { menuFile, menuDebug, menuPreferenze, menuItem4 });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new Size(1612, 28);
        menuStrip1.TabIndex = 0;
        menuStrip1.Text = "menuStrip1";
        // 
        // menuFile
        // 
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuNuovo, menuApri, menuSalva, menuSalvaCome, toolStripSeparator3, menuRecenti, toolStripSeparator4, menuEsci });
        menuFile.Name = "menuFile";
        menuFile.Size = new Size(46, 24);
        menuFile.Text = "&File";
        // 
        // menuNuovo
        // 
        menuNuovo.Name = "menuNuovo";
        menuNuovo.Size = new Size(215, 26);
        menuNuovo.Text = "&Nuovo";
        menuNuovo.Click += menuNuovo_Click;
        // 
        // menuApri
        // 
        menuApri.Name = "menuApri";
        menuApri.Size = new Size(215, 26);
        menuApri.Text = "&Apri...";
        menuApri.Click += menuApri_Click;
        // 
        // menuSalva
        // 
        menuSalva.Name = "menuSalva";
        menuSalva.ShortcutKeys = Keys.Control | Keys.S;
        menuSalva.Size = new Size(215, 26);
        menuSalva.Text = "&Salva";
        menuSalva.Click += menuSalva_Click;
        // 
        // menuSalvaCome
        // 
        menuSalvaCome.Name = "menuSalvaCome";
        menuSalvaCome.Size = new Size(215, 26);
        menuSalvaCome.Text = "Salva &come...";
        menuSalvaCome.Click += menuSalvaCome_Click;
        // 
        // toolStripSeparator3
        // 
        toolStripSeparator3.Name = "toolStripSeparator3";
        toolStripSeparator3.Size = new Size(212, 6);
        // 
        // menuRecenti
        // 
        menuRecenti.Name = "menuRecenti";
        menuRecenti.Size = new Size(215, 26);
        menuRecenti.Text = "Programmi recenti";
        // 
        // toolStripSeparator4
        // 
        toolStripSeparator4.Name = "toolStripSeparator4";
        toolStripSeparator4.Size = new Size(212, 6);
        // 
        // menuEsci
        // 
        menuEsci.Name = "menuEsci";
        menuEsci.Size = new Size(215, 26);
        menuEsci.Text = "&Esci";
        menuEsci.Click += menuEsci_Click;
        // 
        // menuDebug
        // 
        menuDebug.DropDownItems.AddRange(new ToolStripItem[] { menuEsegui, menuEseguiFino, menuEseguiIstr, toolStripSeparator2, menuStop });
        menuDebug.Name = "menuDebug";
        menuDebug.Size = new Size(66, 24);
        menuDebug.Text = "&Esegui";
        // 
        // menuEsegui
        // 
        menuEsegui.Name = "menuEsegui";
        menuEsegui.ShortcutKeys = Keys.F5;
        menuEsegui.Size = new Size(235, 26);
        menuEsegui.Text = "&Esegui";
        menuEsegui.Click += menuRun_Click;
        // 
        // menuEseguiFino
        // 
        menuEseguiFino.Name = "menuEseguiFino";
        menuEseguiFino.ShortcutKeys = Keys.F4;
        menuEseguiFino.Size = new Size(235, 26);
        menuEseguiFino.Text = "Esegui fino a";
        menuEseguiFino.Click += menuEseguiFino_Click;
        // 
        // menuEseguiIstr
        // 
        menuEseguiIstr.Name = "menuEseguiIstr";
        menuEseguiIstr.ShortcutKeys = Keys.F10;
        menuEseguiIstr.Size = new Size(235, 26);
        menuEseguiIstr.Text = "Esegui istruzione";
        menuEseguiIstr.Click += menuStep_Click;
        // 
        // toolStripSeparator2
        // 
        toolStripSeparator2.Name = "toolStripSeparator2";
        toolStripSeparator2.Size = new Size(232, 6);
        // 
        // menuStop
        // 
        menuStop.Name = "menuStop";
        menuStop.ShortcutKeys = Keys.Shift | Keys.F5;
        menuStop.Size = new Size(235, 26);
        menuStop.Text = "Stop";
        menuStop.Click += menuStop_Click;
        // 
        // menuPreferenze
        // 
        menuPreferenze.DropDownItems.AddRange(new ToolStripItem[] { menuOpzioni, menuFontEdit, toolStripSeparator1, menuSalvaOpzioni });
        menuPreferenze.Name = "menuPreferenze";
        menuPreferenze.Size = new Size(93, 24);
        menuPreferenze.Text = "&Preferenze";
        // 
        // menuOpzioni
        // 
        menuOpzioni.Name = "menuOpzioni";
        menuOpzioni.Size = new Size(260, 26);
        menuOpzioni.Text = "&Opzioni...";
        menuOpzioni.Click += menuOpzioni_Click;
        // 
        // menuFontEdit
        // 
        menuFontEdit.Name = "menuFontEdit";
        menuFontEdit.Size = new Size(260, 26);
        menuFontEdit.Text = "Font codice e dati...";
        menuFontEdit.Click += menuFontEdit_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(257, 6);
        // 
        // menuSalvaOpzioni
        // 
        menuSalvaOpzioni.Checked = true;
        menuSalvaOpzioni.CheckState = CheckState.Checked;
        menuSalvaOpzioni.Name = "menuSalvaOpzioni";
        menuSalvaOpzioni.Size = new Size(260, 26);
        menuSalvaOpzioni.Text = "Salva preferenze in uscita";
        menuSalvaOpzioni.Click += menuSalvaOpzioni_Click;
        // 
        // menuItem4
        // 
        menuItem4.Name = "menuItem4";
        menuItem4.Size = new Size(14, 24);
        // 
        // menuInfo
        // 
        menuInfo.Name = "menuInfo";
        menuInfo.Size = new Size(32, 19);
        // 
        // imgListMain
        // 
        imgListMain.ColorDepth = ColorDepth.Depth24Bit;
        imgListMain.ImageStream = (ImageListStreamer)resources.GetObject("imgListMain.ImageStream");
        imgListMain.TransparentColor = Color.White;
        imgListMain.Images.SetKeyName(0, "");
        imgListMain.Images.SetKeyName(1, "");
        imgListMain.Images.SetKeyName(2, "");
        imgListMain.Images.SetKeyName(3, "");
        imgListMain.Images.SetKeyName(4, "");
        imgListMain.Images.SetKeyName(5, "");
        imgListMain.Images.SetKeyName(6, "");
        imgListMain.Images.SetKeyName(7, "");
        imgListMain.Images.SetKeyName(8, "");
        imgListMain.Images.SetKeyName(9, "");
        imgListMain.Images.SetKeyName(10, "");
        imgListMain.Images.SetKeyName(11, "");
        // 
        // tbaMain
        // 
        tbaMain.GripStyle = ToolStripGripStyle.Hidden;
        tbaMain.ImageScalingSize = new Size(24, 24);
        tbaMain.Items.AddRange(new ToolStripItem[] { tbuNuovo, tbuApri, tbuSalva, toolBarSeparator1, tbuEsegui, tbuStop, tbuStep, toolBarSeparator2, tbuHex, tbuDec, tbuCar, tbuMostraMem, tbuStack });
        tbaMain.Location = new Point(0, 28);
        tbaMain.Name = "tbaMain";
        tbaMain.Size = new Size(1612, 31);
        tbaMain.TabIndex = 1;
        tbaMain.Text = "toolStrip1";
        // 
        // tbuNuovo
        // 
        tbuNuovo.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuNuovo.Image = EasyCpu.Win.Properties.Resources.nuovo;
        tbuNuovo.ImageTransparentColor = Color.Magenta;
        tbuNuovo.Name = "tbuNuovo";
        tbuNuovo.Size = new Size(29, 28);
        tbuNuovo.ToolTipText = "Nuovo programma";
        tbuNuovo.Click += tbuNuovo_Click;
        // 
        // tbuApri
        // 
        tbuApri.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuApri.Image = EasyCpu.Win.Properties.Resources.apri;
        tbuApri.ImageTransparentColor = Color.Magenta;
        tbuApri.Name = "tbuApri";
        tbuApri.Size = new Size(29, 28);
        tbuApri.ToolTipText = "Apri";
        tbuApri.Click += tbuApri_Click;
        // 
        // tbuSalva
        // 
        tbuSalva.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuSalva.Image = EasyCpu.Win.Properties.Resources.salva;
        tbuSalva.ImageTransparentColor = Color.Magenta;
        tbuSalva.Name = "tbuSalva";
        tbuSalva.Size = new Size(29, 28);
        tbuSalva.ToolTipText = "Salva (Ctrl+S)";
        tbuSalva.Click += tbuSalva_Click;
        // 
        // toolBarSeparator1
        // 
        toolBarSeparator1.Name = "toolBarSeparator1";
        toolBarSeparator1.Size = new Size(6, 31);
        // 
        // tbuEsegui
        // 
        tbuEsegui.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuEsegui.Image = EasyCpu.Win.Properties.Resources.Esegui;
        tbuEsegui.ImageTransparentColor = Color.Magenta;
        tbuEsegui.Name = "tbuEsegui";
        tbuEsegui.Size = new Size(29, 28);
        tbuEsegui.ToolTipText = "Esegui (F5)";
        tbuEsegui.Click += tbuEsegui_Click;
        // 
        // tbuStop
        // 
        tbuStop.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuStop.Image = EasyCpu.Win.Properties.Resources.stop;
        tbuStop.ImageTransparentColor = Color.Magenta;
        tbuStop.Name = "tbuStop";
        tbuStop.Size = new Size(29, 28);
        tbuStop.ToolTipText = "Arresta (Shift+F5)";
        tbuStop.Click += tbuStop_Click;
        // 
        // tbuStep
        // 
        tbuStep.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuStep.Image = EasyCpu.Win.Properties.Resources.step;
        tbuStep.ImageTransparentColor = Color.Magenta;
        tbuStep.Name = "tbuStep";
        tbuStep.Size = new Size(29, 28);
        tbuStep.ToolTipText = "Esegui istruzione (F10)";
        tbuStep.Click += tbuStep_Click;
        // 
        // toolBarSeparator2
        // 
        toolBarSeparator2.Name = "toolBarSeparator2";
        toolBarSeparator2.Size = new Size(6, 31);
        // 
        // tbuHex
        // 
        tbuHex.CheckOnClick = true;
        tbuHex.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuHex.Image = EasyCpu.Win.Properties.Resources.hex;
        tbuHex.ImageTransparentColor = Color.Magenta;
        tbuHex.Name = "tbuHex";
        tbuHex.Size = new Size(29, 28);
        tbuHex.ToolTipText = "Formato esadecimale";
        tbuHex.CheckStateChanged += tbuHex_CheckStateChanged;
        // 
        // tbuDec
        // 
        tbuDec.CheckOnClick = true;
        tbuDec.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuDec.Image = EasyCpu.Win.Properties.Resources.dec;
        tbuDec.ImageTransparentColor = Color.Magenta;
        tbuDec.Name = "tbuDec";
        tbuDec.Size = new Size(29, 28);
        tbuDec.ToolTipText = "Formato decimale";
        tbuDec.CheckStateChanged += tbuDec_CheckStateChanged;
        // 
        // tbuCar
        // 
        tbuCar.CheckOnClick = true;
        tbuCar.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuCar.Image = EasyCpu.Win.Properties.Resources.car;
        tbuCar.ImageTransparentColor = Color.Magenta;
        tbuCar.Name = "tbuCar";
        tbuCar.Size = new Size(29, 28);
        tbuCar.ToolTipText = "Formato carattere";
        tbuCar.CheckStateChanged += tbuCar_CheckStateChanged;
        // 
        // tbuMostraMem
        // 
        tbuMostraMem.CheckOnClick = true;
        tbuMostraMem.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuMostraMem.Image = EasyCpu.Win.Properties.Resources.mostraMem;
        tbuMostraMem.ImageTransparentColor = Color.Magenta;
        tbuMostraMem.Name = "tbuMostraMem";
        tbuMostraMem.Size = new Size(29, 28);
        tbuMostraMem.ToolTipText = "Mostra/nascondi memoria";
        tbuMostraMem.CheckStateChanged += tbuMostraMem_CheckStateChanged;
        // 
        // tbuStack
        // 
        tbuStack.CheckOnClick = true;
        tbuStack.DisplayStyle = ToolStripItemDisplayStyle.Image;
        tbuStack.Image = EasyCpu.Win.Properties.Resources.stack2;
        tbuStack.ImageTransparentColor = Color.Magenta;
        tbuStack.Name = "tbuStack";
        tbuStack.Size = new Size(29, 28);
        tbuStack.ToolTipText = "Numero colonne stack";
        tbuStack.CheckStateChanged += tbuStack_CheckStateChanged;
        // 
        // staMain
        // 
        staMain.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        staMain.ImageScalingSize = new Size(24, 24);
        staMain.Items.AddRange(new ToolStripItem[] { spnIcon, spnStato, spnEditor, spnReg });
        staMain.Location = new Point(0, 1133);
        staMain.Name = "staMain";
        staMain.ShowItemToolTips = true;
        staMain.Size = new Size(1612, 22);
        staMain.TabIndex = 2;
        staMain.Text = "statusStrip1";
        // 
        // spnIcon
        // 
        spnIcon.Name = "spnIcon";
        spnIcon.Size = new Size(0, 16);
        spnIcon.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // spnStato
        // 
        spnStato.Name = "spnStato";
        spnStato.Size = new Size(0, 16);
        spnStato.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // spnEditor
        // 
        spnEditor.Name = "spnEditor";
        spnEditor.Size = new Size(0, 16);
        spnEditor.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // spnReg
        // 
        spnReg.Name = "spnReg";
        spnReg.Size = new Size(1597, 16);
        spnReg.Spring = true;
        spnReg.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pnlMain
        // 
        pnlMain.BackColor = SystemColors.Control;
        pnlMain.BorderStyle = BorderStyle.Fixed3D;
        pnlMain.Controls.Add(picCpu);
        pnlMain.Controls.Add(txtOF);
        pnlMain.Controls.Add(txtZF);
        pnlMain.Controls.Add(txtSF);
        pnlMain.Controls.Add(label1);
        pnlMain.Controls.Add(tabEdit);
        pnlMain.Controls.Add(lboStack);
        pnlMain.Controls.Add(lboRegs);
        pnlMain.Controls.Add(lboMem);
        pnlMain.Controls.Add(label2);
        pnlMain.Dock = DockStyle.Fill;
        pnlMain.Location = new Point(0, 59);
        pnlMain.Name = "pnlMain";
        pnlMain.Size = new Size(1612, 1074);
        pnlMain.TabIndex = 3;
        // 
        // picCpu
        // 
        picCpu.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        picCpu.Location = new Point(-2, 1040);
        picCpu.Name = "picCpu";
        picCpu.Size = new Size(25, 27);
        picCpu.SizeMode = PictureBoxSizeMode.CenterImage;
        picCpu.TabIndex = 3;
        picCpu.TabStop = false;
        // 
        // txtOF
        // 
        txtOF.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        txtOF.BackColor = Color.White;
        txtOF.BorderStyle = BorderStyle.None;
        txtOF.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtOF.Location = new Point(1471, 1031);
        txtOF.Name = "txtOF";
        txtOF.ReadOnly = true;
        txtOF.Size = new Size(35, 19);
        txtOF.TabIndex = 8;
        txtOF.Text = "of";
        // 
        // txtZF
        // 
        txtZF.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        txtZF.BackColor = Color.White;
        txtZF.BorderStyle = BorderStyle.None;
        txtZF.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtZF.Location = new Point(1330, 1031);
        txtZF.Name = "txtZF";
        txtZF.ReadOnly = true;
        txtZF.Size = new Size(36, 19);
        txtZF.TabIndex = 7;
        txtZF.Text = "zf";
        // 
        // txtSF
        // 
        txtSF.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        txtSF.BackColor = Color.White;
        txtSF.BorderStyle = BorderStyle.None;
        txtSF.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        txtSF.Location = new Point(1400, 1031);
        txtSF.Name = "txtSF";
        txtSF.ReadOnly = true;
        txtSF.Size = new Size(36, 19);
        txtSF.TabIndex = 6;
        txtSF.Text = "sf";
        // 
        // label1
        // 
        label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        label1.Font = new Font("Courier New", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
        label1.Location = new Point(1312, 13);
        label1.Name = "label1";
        label1.Size = new Size(282, 26);
        label1.TabIndex = 5;
        label1.Text = "Stack";
        // 
        // tabEdit
        // 
        tabEdit.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabEdit.Controls.Add(tabCode);
        tabEdit.Controls.Add(tabData);
        tabEdit.Font = new Font("Courier New", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
        tabEdit.Location = new Point(18, 0);
        tabEdit.Name = "tabEdit";
        tabEdit.SelectedIndex = 0;
        tabEdit.Size = new Size(1277, 892);
        tabEdit.TabIndex = 4;
        tabEdit.SelectedIndexChanged += tabEdit_SelectedIndexChanged;
        // 
        // tabCode
        // 
        tabCode.Font = new Font("Courier New", 12F);
        tabCode.Location = new Point(4, 31);
        tabCode.Name = "tabCode";
        tabCode.Size = new Size(1269, 857);
        tabCode.TabIndex = 0;
        tabCode.Text = "Codice";
        // 
        // tabData
        // 
        tabData.Location = new Point(4, 31);
        tabData.Name = "tabData";
        tabData.Size = new Size(1269, 857);
        tabData.TabIndex = 1;
        tabData.Text = "Dati";
        // 
        // lboStack
        // 
        lboStack.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        lboStack.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lboStack.Location = new Point(1312, 39);
        lboStack.Name = "lboStack";
        lboStack.Size = new Size(282, 284);
        lboStack.TabIndex = 3;
        lboStack.DoubleClick += lboStack_DoubleClick;
        // 
        // lboRegs
        // 
        lboRegs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        lboRegs.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lboRegs.IntegralHeight = false;
        lboRegs.Location = new Point(1312, 802);
        lboRegs.Name = "lboRegs";
        lboRegs.Size = new Size(282, 264);
        lboRegs.TabIndex = 2;
        lboRegs.SelectedIndexChanged += lboRegs_SelectedIndexChanged;
        lboRegs.DoubleClick += lboRegs_DoubleClick;
        // 
        // lboMem
        // 
        lboMem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lboMem.IntegralHeight = false;
        lboMem.Location = new Point(18, 894);
        lboMem.Name = "lboMem";
        lboMem.SelectionMode = SelectionMode.None;
        lboMem.Size = new Size(1277, 184);
        lboMem.TabIndex = 1;
        lboMem.DoubleClick += lboMem_DoubleClick;
        // 
        // label2
        // 
        label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        label2.Font = new Font("Courier New", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
        label2.Location = new Point(1312, 773);
        label2.Name = "label2";
        label2.Size = new Size(282, 26);
        label2.TabIndex = 5;
        label2.Text = "Registri";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(9F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1612, 1155);
        Controls.Add(pnlMain);
        Controls.Add(staMain);
        Controls.Add(tbaMain);
        Controls.Add(menuStrip1);
        Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Icon = (Icon)resources.GetObject("$this.Icon");
        KeyPreview = true;
        MainMenuStrip = menuStrip1;
        MinimumSize = new Size(990, 613);
        Name = "MainForm";
        Text = "Form1";
        FormClosing += Form1_Closing;
        Load += Form1_Load;
        KeyDown += Form1_KeyDown;
        Resize += Form1_Resize;
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        tbaMain.ResumeLayout(false);
        tbaMain.PerformLayout();
        staMain.ResumeLayout(false);
        staMain.PerformLayout();
        pnlMain.ResumeLayout(false);
        pnlMain.PerformLayout();
        ((ISupportInitialize)picCpu).EndInit();
        tabEdit.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();

    }
    private ToolStripSeparator toolStripSeparator3;
    private ToolStripSeparator toolStripSeparator4;
    private ToolStripSeparator toolStripSeparator2;
    private ToolStripSeparator toolStripSeparator1;
}
