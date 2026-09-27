using System;
using System.IO;
using System.Threading.Tasks;

namespace EasyCpu.Backend.Serializers;

public interface ISourceSerializer
{
    // memoria: modello di memoria del programma ("byte"), null per la modalità a parole
    Task<(string[] code, string[] data, string? memoria)> LoadAsync(Stream stream);
    Task SaveAsync(Stream stream, string[] code, string[] data, string? memoria = null);
    bool CanWrite { get; }

    public static ISourceSerializer ForPath(string path) =>
        path.EndsWith(".asj", StringComparison.OrdinalIgnoreCase)
            ? new EasyFileSerializer()
            : (ISourceSerializer)new LegacyAsSerializer();
}
