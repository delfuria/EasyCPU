#nullable enable
using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using EasyCPU.ViewModels;

namespace EasyCPU.Views.Editor;

public class BreakpointMargin : AbstractMargin
{
    private readonly MainViewModel _vm;
    private static readonly IBrush BreakpointBrush = Brushes.Red;

    public BreakpointMargin(MainViewModel vm)
    {
        _vm = vm;
        _vm.Breakpoints.CollectionChanged += (_, _) => InvalidateVisual();
    }

    // Ridisegna il margine ogni volta che le VisualLines vengono ricostruite.
    // Senza questo, Render() veniva chiamato con VisualLines vuote (rebuild in corso)
    // e i pallini non venivano disegnati dopo l'attivazione di un breakpoint.
    protected override void OnTextViewChanged(
        AvaloniaEdit.Rendering.TextView? oldTextView,
        AvaloniaEdit.Rendering.TextView? newTextView)
    {
        if (oldTextView != null)
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
        if (newTextView != null)
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
        base.OnTextViewChanged(oldTextView, newTextView);
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e) => InvalidateVisual();

    protected override void OnPointerPressed(Avalonia.Input.PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var textView = TextView;
        if (textView == null || textView.Document == null) return;
        if (e.GetCurrentPoint(null).Properties.PointerUpdateKind
            != Avalonia.Input.PointerUpdateKind.LeftButtonPressed) return;

        double lineHeight = textView.DefaultLineHeight;
        double docY = e.GetPosition(this).Y + textView.VerticalOffset;
        int lineNumber = (int)(docY / lineHeight) + 1;
        if (lineNumber < 1 || lineNumber > textView.Document.LineCount) return;

        _vm.ToggleBreakpointLine(lineNumber);
        e.Handled = true;
    }

    protected override Size MeasureOverride(Size availableSize) => new Size(16, 0);

    public override void Render(DrawingContext context)
    {
        // Fill quasi-trasparente sull'intera area: rende il controllo hit-testable
        // anche quando non ci sono pallini disegnati (senza Background, Avalonia
        // considera hit-testabile solo le zone con contenuto renderizzato).
        context.FillRectangle(new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.FromArgb(1, 0, 0, 0)), new Rect(Bounds.Size));

        var textView = TextView;
        if (textView == null || textView.Document == null) return;

        double lineHeight = textView.DefaultLineHeight;
        if (lineHeight <= 0 || Bounds.Width <= 0) return;

        double r = Math.Min(Bounds.Width, lineHeight) / 2 - 2;
        if (r <= 0) return;

        // Calcolo diretto posizione Y dai numeri di riga — evita dipendenza da
        // textView.VisualLines che può essere vuoto durante il rebuild post-click.
        foreach (int lineNumber in _vm.Breakpoints)
        {
            if (lineNumber < 1 || lineNumber > textView.Document.LineCount) continue;
            double viewportTop = (lineNumber - 1) * lineHeight - textView.VerticalOffset;
            if (viewportTop + lineHeight < 0 || viewportTop > Bounds.Height) continue;
            context.DrawEllipse(BreakpointBrush, null,
                new Point(Bounds.Width / 2, viewportTop + lineHeight / 2), r, r);
        }
    }

}
