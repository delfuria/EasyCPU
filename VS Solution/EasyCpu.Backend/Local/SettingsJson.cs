using System.Text.Json.Serialization;
using EasyCpu.Common;

namespace EasyCpu.Backend.Local
{
    internal record OpzioniDto(
        [property: JsonPropertyName("formatoDati")] FormatoValore formatoDati,
        [property: JsonPropertyName("formatoCarZero")] string formatoCarZero,
        [property: JsonPropertyName("maxNumErrori")] int maxNumErrori,
        [property: JsonPropertyName("colonneStack")] int colonneStack,
        [property: JsonPropertyName("inizializzaRegistri")] bool inizializzaRegistri,
        [property: JsonPropertyName("loopInfinito")] int loopInfinito,
        [property: JsonPropertyName("mostraMemoria")] bool mostraMemoria,
        [property: JsonPropertyName("fontEditorNome")] string fontEditorNome,
        [property: JsonPropertyName("fontEditorSize")] float fontEditorSize,
        [property: JsonPropertyName("fontEditorStyle")] int fontEditorStyle,
        [property: JsonPropertyName("editorZoomFactor")] float editorZoomFactor,
        [property: JsonPropertyName("pienoSchermo")] bool pienoSchermo,
        [property: JsonPropertyName("versioneAssembly")] string versioneAssembly,
        [property: JsonPropertyName("fontPanelliSize")] float fontPanelliSize,
        [property: JsonPropertyName("margineSinistro")] int margineSinistro);

    internal record RecentiDto(
        [property: JsonPropertyName("version")] int version,
        [property: JsonPropertyName("files")] string[] files);

    [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(OpzioniDto))]
    [JsonSerializable(typeof(RecentiDto))]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}
