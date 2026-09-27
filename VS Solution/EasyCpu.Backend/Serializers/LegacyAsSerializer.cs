using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasyCpu.Backend.Local;

namespace EasyCpu.Backend.Serializers;

public class LegacyAsSerializer : ISourceSerializer
{
    public bool CanWrite => false;

    // il formato storico non ha il modello di memoria: sempre modalità a parole
    public Task<(string[] code, string[] data, string? memoria)> LoadAsync(Stream stream)
    {
        Storage.Apri(stream, out var code, out var data);
        return Task.FromResult<(string[], string[], string?)>((code.ToArray(), data.ToArray(), null));
    }

    public Task SaveAsync(Stream stream, string[] code, string[] data, string? memoria = null)
        => throw new InvalidOperationException("Il formato legacy (.as) è di sola lettura.");
}
