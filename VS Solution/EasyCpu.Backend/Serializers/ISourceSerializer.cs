using System;
using System.IO;
using System.Threading.Tasks;

namespace EasyCpu.Backend.Serializers;

public interface ISourceSerializer
{
    Task<(string[] code, string[] data)> LoadAsync(Stream stream);
    Task SaveAsync(Stream stream, string[] code, string[] data);
    bool CanWrite { get; }

    public static ISourceSerializer ForPath(string path) =>
        path.EndsWith(".asj", StringComparison.OrdinalIgnoreCase)
            ? new EasyFileSerializer()
            : (ISourceSerializer)new LegacyAsSerializer();
}
