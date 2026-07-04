using EasyCpu.Assembler.Processore;
using EasyCpu.Assembler.Parsing;


namespace EasyCpu.CustomControl
{
    public class DebugListBox : ListBox
    {

        public DebugListBox()
        {
            DrawItem += new DrawItemEventHandler(_DrawItem);
        }

        void _DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1) return;
            Graphics dc = e.Graphics;
            Color coloreTesto = e.ForeColor;

            e.DrawBackground();
            if (Cpu.Stato == Cpu.StatoCpu.Ferma) return;
            if (e.Index == Compiler.TabellaDebug[Cpu.IP])
            {
                coloreTesto = Color.Red;
                dc.FillRectangle(Brushes.Yellow, e.Bounds);
            }
            string item = (string)Items[e.Index];
            dc.DrawString(item, e.Font, new SolidBrush(coloreTesto), e.Bounds);

        }
    }
}
