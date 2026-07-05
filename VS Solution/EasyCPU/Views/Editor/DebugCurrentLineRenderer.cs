#nullable enable
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using EasyCPU.ViewModels;

namespace EasyCPU.Views.Editor;

/// <summary>
/// Evidenzia la riga corrente dell'IP durante il debug (giallo semitrasparente).
/// </summary>
public class DebugCurrentLineRenderer : IBackgroundRenderer
{
    private readonly MainViewModel _vm;
    private static readonly IBrush HighlightBrush =
        new SolidColorBrush(Avalonia.Media.Color.FromArgb(80, 255, 255, 0));

    public DebugCurrentLineRenderer(MainViewModel vm) => _vm = vm;

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        int lineNumber = _vm.CurrentSourceLine;
        if (lineNumber < 1 || textView.Document == null) return;
        if (lineNumber > textView.Document.LineCount) return;

        textView.EnsureVisualLines();
        var visualLine = textView.GetVisualLine(lineNumber);
        if (visualLine == null) return;

        // Copre l'intera larghezza visibile, non solo il testo della riga
        double y = visualLine.VisualTop - textView.ScrollOffset.Y;
        var rect = new Rect(0, y, textView.Bounds.Width, visualLine.Height);
        drawingContext.FillRectangle(HighlightBrush, rect);
    }
}
