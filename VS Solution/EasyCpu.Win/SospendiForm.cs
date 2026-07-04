using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;


public partial class SospendiForm : System.Windows.Forms.Form
{
 public SospendiForm()
 {
 InitializeComponent();
 }

 public ModoSospendi Modo;

 private void btnPausa_Click(object sender, System.EventArgs e)
 {
 Modo = ModoSospendi.Pausa;
 DialogResult = DialogResult.OK;
 }

 private void btnArresta_Click(object sender, System.EventArgs e)
 {
 Modo = ModoSospendi.Arresta; 
 DialogResult = DialogResult.OK;
 }

 private void SospendiForm_Activated(object sender, System.EventArgs e)
 {
 Modo = ModoSospendi.Arresta;
 btnArresta.Select();
 }

 private void btnContinua_Click(object sender, System.EventArgs e)
 {
 Modo = ModoSospendi.Continua; 
 DialogResult = DialogResult.OK; 
 }
}

public enum ModoSospendi
{
 Continua,
 Pausa,
 Arresta

}
