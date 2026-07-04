using EasyCpu.Assembler.Processore;
using System.Drawing.Drawing2D;


namespace EasyCpu.CustomControl
{
    public class IconaCpu : PictureBox
    {

        public IconaCpu()
        {
            Location = new Point(8, 398);
            Size = new Size(16, 16);
            Anchor = AnchorStyles.Bottom| AnchorStyles.Left;
            Paint += new PaintEventHandler(_Paint);
        }

        Brush OttieniBrush(Cpu.StatoCpu stato)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, Width, Height);
            PathGradientBrush brush = new PathGradientBrush(path);
            brush.CenterColor = Color.White;

            switch (stato)
            {
                case Cpu.StatoCpu.Attiva:
                    brush.SurroundColors = new Color[] { Color.Gold };
                    break;

                case Cpu.StatoCpu.Pronta:
                    brush.SurroundColors = new Color[] { Color.Green };
                    break;

                case Cpu.StatoCpu.Ferma:
                    brush.SurroundColors = new Color[] { Color.Red };
                    break;
            }
            return brush;

        }

        void _Paint(object sender, PaintEventArgs e)
        {
            Graphics dc = e.Graphics;
            dc.FillEllipse(OttieniBrush(Cpu.Stato), 0, 0, Width, Height);
        }


    }
}
