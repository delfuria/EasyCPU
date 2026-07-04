namespace EasyCpu
{
    partial class MemoriaForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtMemoria = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // txtMemoria
            // 
            this.txtMemoria.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMemoria.Location = new System.Drawing.Point(12, 44);
            this.txtMemoria.Name = "txtMemoria";
            this.txtMemoria.Size = new System.Drawing.Size(454, 280);
            this.txtMemoria.TabIndex = 0;
            this.txtMemoria.Text = "";
            this.txtMemoria.WordWrap = false;
            // 
            // MemoriaForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(477, 334);
            this.Controls.Add(this.txtMemoria);
            this.KeyPreview = true;
            this.Name = "MemoriaForm";
            this.Text = "Memoria";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MemoriaForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MemoriaForm_FormClosed);
            this.Load += new System.EventHandler(this.MemoriaForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MemoriaForm_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtMemoria;
    }
}