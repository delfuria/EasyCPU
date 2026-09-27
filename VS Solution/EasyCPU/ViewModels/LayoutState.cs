using System.Text.Json.Serialization;

namespace EasyCPU.ViewModels;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(RootNode),     "root")]
[JsonDerivedType(typeof(PropNode),     "prop")]
[JsonDerivedType(typeof(DocNode),      "doc")]
[JsonDerivedType(typeof(ToolNode),     "tool")]
[JsonDerivedType(typeof(SplitterNode), "spl")]
internal abstract record DockNode;

internal record RootNode(string? ActiveId, DockNode[] Children) : DockNode;
internal record PropNode(string Orient, double Prop, string? ActiveId, DockNode[] Children) : DockNode;
internal record DocNode(double Prop, string? ActiveId, string[] Ids) : DockNode;
internal record ToolNode(double Prop, string? ActiveId, string[] Ids) : DockNode;
internal record SplitterNode() : DockNode;

// Serializzazione generata in compilazione: nella versione Browser (WebAssembly ottimizzata)
// la serializzazione per reflection è disattivata.
[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
[JsonSerializable(typeof(DockNode))]
internal partial class LayoutJsonContext : JsonSerializerContext;
