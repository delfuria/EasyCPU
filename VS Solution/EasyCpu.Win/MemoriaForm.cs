using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Processore;
using EasyCpu.CustomControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EasyCpu
{
    public partial class MemoriaForm : Form
    {
        public MemoriaForm()
        {
            InitializeComponent();
            TopLevel = true;
            GetContenuto = () => Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, 8);
        }

        public event EventHandler F10KeyPressed;
        public static MemoriaForm Create(string title, Func<List<string>> getContenuto)
        {
            return Create(title, getContenuto, new Rectangle(100, 100, 400, 300));
        }
        public static MemoriaForm Create(string title, Func<List<string>> getContenuto, Rectangle r)
        {
            MemoriaForm frm = new MemoriaForm();
            frm.StartPosition = FormStartPosition.Manual;
            frm.Bounds = r;
            frm.Title = title;
            frm.GetContenuto = getContenuto;
            return frm;
        }

        public Func<List<string>> GetContenuto;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get { return Text; }
            set { Text = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font FontMemoria 
        {
            get { return txtMemoria.Font; }
            set { txtMemoria.Font = value; }
        }

        public void Aggiorna()
        {
            if (GetContenuto == null)
                return;
            txtMemoria.SaveScrollInfo();
            List<string> lines = GetContenuto();
            
            float zf = txtMemoria.ZoomFactor;
            txtMemoria.SuspendLayout();
            int start = txtMemoria.SelectionStart;
            txtMemoria.Clear();            
            foreach (var line in lines)
            {
                txtMemoria.AppendText(line + "\r\n");
            }
            txtMemoria.SelectionStart = start;            
            txtMemoria.ResumeLayout();

            //! necessario  reimpostarlo a 1.0f prima di settarlo al valore effettivo!
            txtMemoria.ZoomFactor = 1.0f;
            txtMemoria.ZoomFactor = zf;
            txtMemoria.RestoreScrollInfo();
        }

        private void MemoriaForm_Load(object sender, EventArgs e)
        {
            TopMost = true;
        }

        private void MemoriaForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void MemoriaForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void MemoriaForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
                if (F10KeyPressed != null)
                {
                    F10KeyPressed(this, EventArgs.Empty);
                    e.Handled = true;
                }
        }

    }
}
