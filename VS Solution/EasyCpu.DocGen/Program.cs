using EasyCpu.DocGen;

// Uso: dotnet run --project EasyCpu.DocGen -- <reference.md> <reference.html>
if (args.Length != 2)
{
    Console.Error.WriteLine("Uso: EasyCpu.DocGen <reference.md> <reference.html>");
    return 1;
}

var html = Generatore.Genera(File.ReadAllText(args[0]));
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
File.WriteAllText(args[1], html);
Console.WriteLine($"Generato {args[1]}");
return 0;
