using System;
using System.IO;
using System.Runtime.InteropServices;
using UIKit;

namespace EasyCPU.iOS;

public class Application
{
    static void Main(string[] args)
    {
        // SkiaSharp 3.119.4 ships the iOS-specific managed DLL at net10.0-ios26.2,
        // but our SDK is 26.1, so NuGet falls back to the generic net10.0 DLL whose
        // DllImport("libSkiaSharp") can't resolve the @rpath framework at runtime.
        // Register a resolver so Mono finds the framework in the app bundle.
        NativeLibrary.SetDllImportResolver(
            typeof(SkiaSharp.SKImageInfo).Assembly,
            (name, _, _) =>
            {
                if (name == "libSkiaSharp")
                {
                    var path = Path.Combine(
                        AppContext.BaseDirectory,
                        "Frameworks",
                        "libSkiaSharp.framework",
                        "libSkiaSharp");
                    return NativeLibrary.Load(path);
                }
                return IntPtr.Zero;
            });

        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}