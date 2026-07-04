using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public partial class SospendiForm
{
 private System.Windows.Forms.Button btnPausa;
 private System.Windows.Forms.Button btnArresta;
 private System.Windows.Forms.Label label1;
 private System.Windows.Forms.Button btnContinua;
 private System.Windows.Forms.Label label3;
 private System.Windows.Forms.Label label2;
 private System.Windows.Forms.Label label4;
 private System.Windows.Forms.PictureBox pictureBox1;
 private System.ComponentModel.Container components = null;

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
 this.btnPausa = new System.Windows.Forms.Button();
 this.btnArresta = new System.Windows.Forms.Button();
 this.label1 = new System.Windows.Forms.Label();
 this.btnContinua = new System.Windows.Forms.Button();
 this.label3 = new System.Windows.Forms.Label();
 this.label2 = new System.Windows.Forms.Label();
 this.label4 = new System.Windows.Forms.Label();
 this.pictureBox1 = new System.Windows.Forms.PictureBox();
 ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
 this.SuspendLayout();
 // 
 // btnPausa
 // 
 this.btnPausa.Location = new System.Drawing.Point(302,230);
 this.btnPausa.Name = "btnPausa";
 this.btnPausa.Size = new System.Drawing.Size(188,54);
 this.btnPausa.TabIndex =0;
 this.btnPausa.Text = "&Pausa";
 this.btnPausa.Click += new System.EventHandler(this.btnPausa_Click);
 // 
 // btnArresta
 // 
 this.btnArresta.Location = new System.Drawing.Point(72,230);
 this.btnArresta.Name = "btnArresta";
 this.btnArresta.Size = new System.Drawing.Size(187,54);
 this.btnArresta.TabIndex =0;
 this.btnArresta.Text = "&Arresta";
 this.btnArresta.Click += new System.EventHandler(this.btnArresta_Click);
 // 
 // label1
 // 
 this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif",9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
 this.label1.Location = new System.Drawing.Point(43,14);
 this.label1.Name = "label1";
 this.label1.Size = new System.Drawing.Size(711,27);
 this.label1.TabIndex =1;
 this.label1.Text = "Il programma si trova probabilmente nella condizione di ciclo infinito";
 // 
 // btnContinua
 // 
 this.btnContinua.Location = new System.Drawing.Point(533,230);
 this.btnContinua.Name = "btnContinua";
 this.btnContinua.Size = new System.Drawing.Size(187,54);
 this.btnContinua.TabIndex =0;
 this.btnContinua.Text = "&Continua";
 this.btnContinua.Click += new System.EventHandler(this.btnContinua_Click);
 // 
 // label3
 // 
 this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif",9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
 this.label3.Location = new System.Drawing.Point(245,108);
 this.label3.Name = "label3";
 this.label3.Size = new System.Drawing.Size(432,41);
 this.label3.TabIndex =2;
 this.label3.Text = " 'Pausa' per sospendere l'esecuzione";
 // 
 // label2
 // 
 this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif",9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
 this.label2.Location = new System.Drawing.Point(245,68);
 this.label2.Name = "label2";
 this.label2.Size = new System.Drawing.Size(432,40);
 this.label2.TabIndex =2;
 this.label2.Text = " 'Arresta' per fermare l'esecuzione";
 // 
 // label4
 // 
 this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif",9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
 this.label4.Location = new System.Drawing.Point(245,149);
 this.label4.Name = "label4";
 this.label4.Size = new System.Drawing.Size(475,41);
 this.label4.TabIndex =2;
 this.label4.Text = " 'Continua' per proseguire l'esecuzione";
 // 
 // pictureBox1
 // 
 this.pictureBox1.Location = new System.Drawing.Point(72,68);
 this.pictureBox1.Name = "pictureBox1";
 this.pictureBox1.Size = new System.Drawing.Size(115,108);
 this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
 this.pictureBox1.TabIndex =3;
 this.pictureBox1.TabStop = false;
 // 
 // SospendiForm
 // 
 this.AcceptButton = this.btnArresta;
 this.AutoScaleBaseSize = new System.Drawing.Size(9,22);
 this.ClientSize = new System.Drawing.Size(816,337);
 this.Controls.Add(this.pictureBox1);
 this.Controls.Add(this.label3);
 this.Controls.Add(this.label1);
 this.Controls.Add(this.btnPausa);
 this.Controls.Add(this.btnArresta);
 this.Controls.Add(this.btnContinua);
 this.Controls.Add(this.label2);
 this.Controls.Add(this.label4);
 this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
 this.MaximizeBox = false;
 this.MinimizeBox = false;
 this.Name = "SospendiForm";
 this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
 this.Text = "Attenzione";
 this.Activated += new System.EventHandler(this.SospendiForm_Activated);
 ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
 this.ResumeLayout(false);

 }
}
