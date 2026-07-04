using EasyCpu.Common;
using System.Collections;


namespace EasyCpu.CustomControl
{
    public class EasyEditor : RichTextBox
    {
        public List<int> mappaLinee;
        bool codeEdit;

        public EasyEditor(bool codeEdit)
        {
            KeyPress += new KeyPressEventHandler(_KeyPress);
            this.codeEdit = codeEdit;
            //Text = new String(' ', Ambiente.MargineSinistro);
        }

        void _KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Ambiente.MargineSinistro == 0 || !codeEdit)
                return;

            if (e.KeyChar == (char)Keys.Tab)
            {
                e.Handled = true;
                int numSpaces = Ambiente.MargineSinistro - ((SelectionStart - GetFirstCharIndexOfCurrentLine()) % 4);
                if (numSpaces > 0)
                    SelectedText = new string(' ', numSpaces);
            }

            if (e.KeyChar == 13)
                SelectedText = new String(' ', Ambiente.MargineSinistro);

        }

        public List<string> Testo
        {
            get
            {
                if (!Ambiente.MostraSoloCodice) // mostra solo istruzioni durante il debug
                {
                    mappaLinee = null;
                    return Lines.ToList();
                }
                List<string> linee = new List<string>();
                List<int> mappa = new List<int>();
                int i = 0;
                foreach (string s in Lines)
                {
                    if (s != null && s.Trim() != "" && s.Trim()[0] != '\'')
                    {
                        linee.Add(s);
                        mappa.Add(i);
                    }
                    i++;
                }
                mappaLinee = mappa;
                return linee;
            }
        }

        public void LineaToCaret(int numRiga, int numColonna)
        {
            string[] lines = Lines;
            if (mappaLinee != null)
                numRiga = mappaLinee[numRiga];
            if (numColonna == -1)
                numColonna = 0;
            int posCaret = 0;
            for (int i = 0; i < numRiga; i++)
            {
                posCaret += lines[i].Length + 1;
            }
            if (Lines[numRiga] == "") numColonna = 0;
            SelectionStart = posCaret + numColonna;
        }

        public int CaretToLinea()
        {
            int caret = SelectionStart;
            string[] linee = Lines;
            int i, pos = 0;
            for (i = 0; i < linee.Length && pos <= caret; i++)
            {
                pos += linee[i].Length + 1;
            }
            if (i == 0) return i;
            return i - 1;
        }

        public int TraslaRigaTrap(int rigaTrap)
        {
            if (mappaLinee == null)
                return rigaTrap;
            for (int i = 0; i < mappaLinee.Count; i++)
                if (rigaTrap <= mappaLinee[i])
                    return i;
            return rigaTrap;
        }
    }
}
