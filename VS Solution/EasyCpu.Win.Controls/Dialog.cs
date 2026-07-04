using EasyCpu.Common;

namespace EasyCpu.Win.Controls
{
    public class Dialog
    {
        public static DialogResult Attenzione(string msg, string titolo)
        {
            return MessageBox.Show(msg, titolo, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        }

        public static DialogResult Conferma(string msg, string titolo)
        {
            return MessageBox.Show(msg, titolo, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }

        public static DialogResult Errore(string msg, string titolo)
        {
            return MessageBox.Show(msg, titolo, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static DialogResult Errore(CodiceErrore err, string titolo)
        {
            return MessageBox.Show(Errori.Msg(err), titolo, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static DialogResult Informazione(string msg, string titolo)
        {
            return MessageBox.Show(msg, titolo, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


    }
}
