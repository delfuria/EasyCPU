
using System.Runtime.InteropServices;

namespace EasyCpu.CustomControl
{
    public static class ScrollHelper
    {
        static Dictionary<Control, int> controls = new Dictionary<Control, int>();
        public static void SaveScrollInfo(this Control ctl)
        {
            int nPos = GetScrollPos(ctl.Handle, (int)ScrollBarType.SbVert);
            nPos <<= 16;
            controls[ctl] = nPos;
        }

        public static void RestoreScrollInfo(this Control ctl)
        {
            if (!controls.ContainsKey(ctl))
                return;
            int nPos = controls[ctl];
            uint wParam = (uint)ScrollBarCommands.SB_THUMBPOSITION | (uint)nPos;
            SendMessage(ctl.Handle, (int)Message.WM_VSCROLL, new IntPtr(wParam), new IntPtr(0));
        }
        public enum ScrollBarType : uint
        {
            SbHorz = 0,
            SbVert = 1,
            SbCtl = 2,
            SbBoth = 3
        }

        public enum Message : uint
        {
            WM_VSCROLL = 0x0115
        }

        public enum ScrollBarCommands : uint
        {
            SB_THUMBPOSITION = 4
        }

        [DllImport("User32.dll")]
        public extern static int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("User32.dll")]
        public extern static int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    }

}