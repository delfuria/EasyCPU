using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using EasyCpu.Common;


public partial class OpzioniForm : System.Windows.Forms.Form
{
    public OpzioniForm()
    {
        InitializeComponent();
    }


    void LeggiOpzioni()
    {
        updnMaxErrori.Value = Ambiente.MaxNumErrori;
        rbuDec.Checked = Ambiente.FormatoDati == FormatoValore.Dec;
        rbuHex.Checked = Ambiente.FormatoDati == FormatoValore.Hex;
        rbuCar.Checked = Ambiente.FormatoDati == FormatoValore.Car;
        rbuStack1.Checked = Ambiente.ColonneStack == 1;
        rbuStack2.Checked = Ambiente.ColonneStack == 2;
        chkInitRegs.Checked = Ambiente.InizializzaRegistri;
        txtLoopInfinito.Text = Ambiente.LoopInfinito.ToString();
        updnMargine.Value = Ambiente.MargineSinistro;
        txtFormatoCarZero.Text = Ambiente.FormatoCarZero;

    }

    void ImpostaOpzioni()
    {
        Ambiente.MaxNumErrori = (int)updnMaxErrori.Value;
        if (rbuDec.Checked)
            Ambiente.FormatoDati = FormatoValore.Dec;
        else
        if (rbuHex.Checked)
            Ambiente.FormatoDati = FormatoValore.Hex;
        else
            Ambiente.FormatoDati = FormatoValore.Car;
        if (rbuStack1.Checked)
            Ambiente.ColonneStack = 1;
        else
            Ambiente.ColonneStack = 2;
        Ambiente.InizializzaRegistri = chkInitRegs.Checked;
        Ambiente.LoopInfinito = int.Parse(txtLoopInfinito.Text);
        Ambiente.MargineSinistro = (int)updnMargine.Value;
        Ambiente.FormatoCarZero = txtFormatoCarZero.Text;
    }

    private void btnOk_Click(object sender, System.EventArgs e)
    {
        ImpostaOpzioni();
        DialogResult = DialogResult.OK;
    }

    private void OpzioniForm_Activated(object sender, System.EventArgs e)
    {
        LeggiOpzioni();
    }

    private void btnAnnulla_Click(object sender, System.EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
    }

    private void txtLoopInfinito_Validating(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            int tmp = int.Parse(txtLoopInfinito.Text);
        }
        catch (FormatException)
        {
            e.Cancel = true;
            errLoopInfinito.SetError(txtLoopInfinito, "E' atteso un numero maggiore o uguale a zero");
        }
        txtLoopInfinito.SelectAll();
    }
}
