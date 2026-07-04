using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EasyCpu.Backend.Local;

namespace EasyCpu.Backend.Serializers;

public class LegacyAsSerializer : ISourceSerializer
{
    public bool CanWrite => false;

    public Task<(string[] code, string[] data)> LoadAsync(Stream stream)
    {
        Storage.Apri(stream, out var code, out var data);
        return Task.FromResult((code.ToArray(), data.ToArray()));
    }

    public Task SaveAsync(Stream stream, string[] code, string[] data)
        => throw new InvalidOperationException("Il formato legacy (.as) è di sola lettura.");
}
