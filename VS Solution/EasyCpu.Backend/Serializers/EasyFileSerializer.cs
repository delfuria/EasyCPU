using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyCpu.Backend.Serializers;

// 1. Il record ora è esterno alla classe ed è accessibile dal Source Generator
internal record FileDoc(
    [property: JsonPropertyName("version")] int version,
    [property: JsonPropertyName("code")]    string[] code,
    [property: JsonPropertyName("data")]    string[] data);

public class EasyFileSerializer : ISourceSerializer
{
    public bool CanWrite => true;

    public async Task<(string[] code, string[] data)> LoadAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();
        
        // Corretto: FileDoc ora è accessibile globale nel namespace
        var doc = JsonSerializer.Deserialize(text, EasyFileJsonContext.Default.FileDoc);
        
        return (doc?.code ?? [], doc?.data ?? []);
    }

    public async Task SaveAsync(Stream stream, string[] code, string[] data)
    {
        var doc = new FileDoc(1, code, data);
        var json = JsonSerializer.Serialize(doc, EasyFileJsonContext.Default.FileDoc);
        
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
        await writer.FlushAsync();
    }
}

// 2. Il contesto ora può leggere FileDoc senza problemi di protezione
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(FileDoc))] // Puntiamo direttamente al record esterno
internal partial class EasyFileJsonContext : JsonSerializerContext
{
}