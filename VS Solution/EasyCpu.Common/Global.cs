using System;
using System.Reflection;
using System.IO;
using System.Drawing;


namespace EasyCpu.Common
{
	public class Global
	{
		public static readonly string ApplicationName = "Easy CPU";

		public static string Version
		{
			get
			{
				AssemblyName name = Assembly.GetExecutingAssembly().GetName();
				Version ver = name.Version;
				return string.Format("Versione {0}.{1}", ver.Major, ver.Minor);
			}
		}

        //TODO: Controllare se funziona con .NET 5 e successive
        /*
         public static Image GetEmbeddedLogo(string logoName, string ext)
		{
			Assembly thisExe = Assembly.GetExecutingAssembly();
			if (logoName == null)
				logoName = thisExe.GetName().Name + "." + ext;
			Stream file = thisExe.GetManifestResourceStream(logoName);
			if (file == null)
				return null;
			return Image.FromStream(file);
		}
		
		public static void ShowDefaultAboutDialog()
		{
			Image logo = Global.GetEmbeddedLogo(null, "bmp");
			InfoDialog id = new InfoDialog();
			id.SetApplicationInfo(Global.ApplicationName, Global.Version, logo);
			id.ShowDialog();
			id.Dispose();
		}
		*/
	}
}