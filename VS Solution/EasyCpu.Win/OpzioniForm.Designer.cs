using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public partial class OpzioniForm
{
 private System.Windows.Forms.Button btnOk;
 private System.Windows.Forms.Button btnAnnulla;
 private System.Windows.Forms.NumericUpDown updnMaxErrori;
 private System.Windows.Forms.Label label1;
 private System.Windows.Forms.GroupBox grpFormato;
 private System.Windows.Forms.RadioButton rbuDec;
 private System.Windows.Forms.RadioButton rbuHex;
 private System.Windows.Forms.GroupBox grpColStack;
 private System.Windows.Forms.RadioButton rbuStack1;
 private System.Windows.Forms.RadioButton rbuStack2;
 private System.Windows.Forms.CheckBox chkInitRegs;
 private System.Windows.Forms.TextBox txtLoopInfinito;
 private System.Windows.Forms.Label label2;
 private System.Windows.Forms.ErrorProvider errLoopInfinito;
 private System.Windows.Forms.Label label3;
 private System.Windows.Forms.NumericUpDown updnMargine;
 private System.Windows.Forms.RadioButton rbuCar;
 private System.Windows.Forms.TextBox txtFormatoCarZero;
 private System.Windows.Forms.Label label4;
 private IContainer components;

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
 this.components = new System.ComponentModel.Container();
 this.btnOk = new System.Windows.Forms.Button();
 this.btnAnnulla = new System.Windows.Forms.Button();
 this.updnMaxErrori = new System.Windows.Forms.NumericUpDown();
 this.label1 = new System.Windows.Forms.Label();
 this.grpFormato = new System.Windows.Forms.GroupBox();
 this.txtFormatoCarZero = new System.Windows.Forms.TextBox();
 this.label4 = new System.Windows.Forms.Label();
 this.rbuHex = new System.Windows.Forms.RadioButton();
 this.rbuDec = new System.Windows.Forms.RadioButton();
 this.rbuCar = new System.Windows.Forms.RadioButton();
 this.grpColStack = new System.Windows.Forms.GroupBox();
 this.rbuStack1 = new System.Windows.Forms.RadioButton();
 this.rbuStack2 = new System.Windows.Forms.RadioButton();
 this.chkInitRegs = new System.Windows.Forms.CheckBox();
 this.txtLoopInfinito = new System.Windows.Forms.TextBox();
 this.label2 = new System.Windows.Forms.Label();
 this.errLoopInfinito = new System.Windows.Forms.ErrorProvider(this.components);
 this.label3 = new System.Windows.Forms.Label();
 this.updnMargine = new System.Windows.Forms.NumericUpDown();
 ((System.ComponentModel.ISupportInitialize)(this.updnMaxErrori)).BeginInit();
 this.grpFormato.SuspendLayout();
 this.grpColStack.SuspendLayout();
 ((System.ComponentModel.ISupportInitialize)(this.errLoopInfinito)).BeginInit();
 ((System.ComponentModel.ISupportInitialize)(this.updnMargine)).BeginInit();
 this.SuspendLayout();
 // 
 // btnOk
 // 
 this.btnOk.Location = new System.Drawing.Point(360,447);
 this.btnOk.Name = "btnOk";
 this.btnOk.Size = new System.Drawing.Size(158,54);
 this.btnOk.TabIndex =4;
 this.btnOk.Text = "&OK";
 this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
 // 
 // btnAnnulla
 // 
 this.btnAnnulla.DialogResult = System.Windows.Forms.DialogResult.Cancel;
 this.btnAnnulla.Location = new System.Drawing.Point(533,447);
 this.btnAnnulla.Name = "btnAnnulla";
 this.btnAnnulla.Size = new System.Drawing.Size(158,54);
 this.btnAnnulla.TabIndex =4;
 this.btnAnnulla.Text = "Annulla";
 this.btnAnnulla.Click += new System.EventHandler(this.btnAnnulla_Click);
 // 
 // updnMaxErrori
 // 
 this.updnMaxErrori.Location = new System.Drawing.Point(374,41);
 this.updnMaxErrori.Name = "updnMaxErrori";
 this.updnMaxErrori.Size = new System.Drawing.Size(72,29);
 this.updnMaxErrori.TabIndex =9;
 this.updnMaxErrori.Value = new decimal(new int[] {
5,
0,
0,
0});
 // 
 // label1
 // 
 this.label1.Location = new System.Drawing.Point(29,41);
 this.label1.Name = "label1";
 this.label1.Size = new System.Drawing.Size(345,40);
 this.label1.TabIndex =8;
 this.label1.Text = "Massimo numero di errori visualizzati";
 // 
 // grpFormato
 // 
 this.grpFormato.Controls.Add(this.txtFormatoCarZero);
 this.grpFormato.Controls.Add(this.label4);
 this.grpFormato.Controls.Add(this.rbuHex);
 this.grpFormato.Controls.Add(this.rbuDec);
 this.grpFormato.Controls.Add(this.rbuCar);
 this.grpFormato.Location = new System.Drawing.Point(29,122);
 this.grpFormato.Name = "grpFormato";
 this.grpFormato.Size = new System.Drawing.Size(360,181);
 this.grpFormato.TabIndex =12;
 this.grpFormato.TabStop = false;
 this.grpFormato.Text = "Formato visualizzazione";
 // 
 // txtFormatoCarZero
 // 
 this.txtFormatoCarZero.Location = new System.Drawing.Point(274,122);
 this.txtFormatoCarZero.MaxLength =4;
 this.txtFormatoCarZero.Name = "txtFormatoCarZero";
 this.txtFormatoCarZero.Size = new System.Drawing.Size(57,29);
 this.txtFormatoCarZero.TabIndex =14;
 // 
 // label4
 // 
 this.label4.Location = new System.Drawing.Point(14,122);
 this.label4.Name = "label4";
 this.label4.Size = new System.Drawing.Size(274,40);
 this.label4.TabIndex =15;
 this.label4.Text = "Visualizza caratteri '\\0' come:";
 // 
 // rbuHex
 // 
 this.rbuHex.Location = new System.Drawing.Point(14,41);
 this.rbuHex.Name = "rbuHex";
 this.rbuHex.Size = new System.Drawing.Size(87,40);
 this.rbuHex.TabIndex =12;
 this.rbuHex.Text = "Hex";
 // 
 // rbuDec
 // 
 this.rbuDec.Location = new System.Drawing.Point(130,41);
 this.rbuDec.Name = "rbuDec";
 this.rbuDec.Size = new System.Drawing.Size(86,40);
 this.rbuDec.TabIndex =11;
 this.rbuDec.Text = "Dec";
 // 
 // rbuCar
 // 
 this.rbuCar.Location = new System.Drawing.Point(245,41);
 this.rbuCar.Name = "rbuCar";
 this.rbuCar.Size = new System.Drawing.Size(86,40);
 this.rbuCar.TabIndex =11;
 this.rbuCar.Text = "Car";
 // 
 // grpColStack
 // 
 this.grpColStack.Controls.Add(this.rbuStack1);
 this.grpColStack.Controls.Add(this.rbuStack2);
 this.grpColStack.Location = new System.Drawing.Point(418,122);
 this.grpColStack.Name = "grpColStack";
 this.grpColStack.Size = new System.Drawing.Size(288,95);
 this.grpColStack.TabIndex =13;
 this.grpColStack.TabStop = false;
 this.grpColStack.Text = "Numero colonne stack";
 // 
 // rbuStack1
 // 
 this.rbuStack1.Location = new System.Drawing.Point(29,54);
 this.rbuStack1.Name = "rbuStack1";
 this.rbuStack1.Size = new System.Drawing.Size(86,27);
 this.rbuStack1.TabIndex =0;
 this.rbuStack1.Text = "Una";
 // 
 // rbuStack2
 // 
 this.rbuStack2.Location = new System.Drawing.Point(144,54);
 this.rbuStack2.Name = "rbuStack2";
 this.rbuStack2.Size = new System.Drawing.Size(101,27);
 this.rbuStack2.TabIndex =0;
 this.rbuStack2.Text = "Due";
 // 
 // chkInitRegs
 // 
 this.chkInitRegs.Location = new System.Drawing.Point(29,382);
 this.chkInitRegs.Name = "chkInitRegs";
 this.chkInitRegs.Size = new System.Drawing.Size(288,41);
 this.chkInitRegs.TabIndex =7;
 this.chkInitRegs.Text = "Inizializza registri all'avvio";
 // 
 // txtLoopInfinito
 // 
 this.txtLoopInfinito.Location = new System.Drawing.Point(479,337);
 this.txtLoopInfinito.Name = "txtLoopInfinito";
 this.txtLoopInfinito.Size = new System.Drawing.Size(129,29);
 this.txtLoopInfinito.TabIndex =14;
 this.txtLoopInfinito.Validating += new System.ComponentModel.CancelEventHandler(this.txtLoopInfinito_Validating);
 // 
 // label2
 // 
 this.label2.AutoSize = true;
 this.label2.Location = new System.Drawing.Point(29,342);
 this.label2.Name = "label2";
 this.label2.Size = new System.Drawing.Size(453,25);
 this.label2.TabIndex =15;
 this.label2.Text = "Numero di istruzioni che prefigurano un ciclo infinito";
 // 
 // errLoopInfinito
 // 
 this.errLoopInfinito.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
 this.errLoopInfinito.ContainerControl = this;
 this.errLoopInfinito.DataMember = "";
 // 
 // label3
 // 
 this.label3.AutoSize = true;
 this.label3.Location = new System.Drawing.Point(412,262);
 this.label3.Name = "label3";
 this.label3.Size = new System.Drawing.Size(213,25);
 this.label3.TabIndex =15;
 this.label3.Text = "N? caratteri tabulazione";
 // 
 // updnMargine
 // 
 this.updnMargine.Location = new System.Drawing.Point(634,259);
 this.updnMargine.Maximum = new decimal(new int[] {
15,
0,
0,
0});
 this.updnMargine.Name = "updnMargine";
 this.updnMargine.Size = new System.Drawing.Size(72,29);
 this.updnMargine.TabIndex =16;
 // 
 // OpzioniForm
 // 
 this.AcceptButton = this.btnOk;
 this.AutoScaleBaseSize = new System.Drawing.Size(9,22);
 this.CancelButton = this.btnAnnulla;
 this.ClientSize = new System.Drawing.Size(783,551);
 this.Controls.Add(this.updnMargine);
 this.Controls.Add(this.label2);
 this.Controls.Add(this.txtLoopInfinito);
 this.Controls.Add(this.grpColStack);
 this.Controls.Add(this.grpFormato);
 this.Controls.Add(this.updnMaxErrori);
 this.Controls.Add(this.label1);
 this.Controls.Add(this.btnOk);
 this.Controls.Add(this.btnAnnulla);
 this.Controls.Add(this.chkInitRegs);
 this.Controls.Add(this.label3);
 this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
 this.MaximizeBox = false;
 this.MinimizeBox = false;
 this.Name = "OpzioniForm";
 this.Text = "Opzioni";
 this.Activated += new System.EventHandler(this.OpzioniForm_Activated);
 ((System.ComponentModel.ISupportInitialize)(this.updnMaxErrori)).EndInit();
 this.grpFormato.ResumeLayout(false);
 this.grpFormato.PerformLayout();
 this.grpColStack.ResumeLayout(false);
 ((System.ComponentModel.ISupportInitialize)(this.errLoopInfinito)).EndInit();
 ((System.ComponentModel.ISupportInitialize)(this.updnMargine)).EndInit();
 this.ResumeLayout(false);
 this.PerformLayout();

 }
}
