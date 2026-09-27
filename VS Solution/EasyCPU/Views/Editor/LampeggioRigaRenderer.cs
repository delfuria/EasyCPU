#nullable enable
using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Rendering;

namespace EasyCPU.Views.Editor;

/// <summary>
/// Fa lampeggiare brevemente una riga (due impulsi rosso/arancio che si attenuano),
/// per ritrovarla dopo il doppio click su un errore.
/// </summary>
public class LampeggioRigaRenderer : IBackgroundRenderer
{
    private static readonly TimeSpan Durata = TimeSpan.FromMilliseconds(1200);
    private const byte AlfaMassimo = 150;

    private readonly TextView _textView;
    private readonly DispatcherTimer _timer;
    private int _riga;
    private DateTime _inizio;

    public LampeggioRigaRenderer(TextView textView)
    {
        _textView = textView;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timer.Tick += (_, _) =>
        {
            if (DateTime.Now - _inizio >= Durata)
            {
                _timer.Stop();
                _riga = 0;
            }
            _textView.InvalidateLayer(KnownLayer.Background);
        };
    }

    public KnownLayer Layer => KnownLayer.Background;

    // Una nuova chiamata riavvia il lampeggio sulla nuova riga
    public void Avvia(int riga)
    {
        _riga = riga;
        _inizio = DateTime.Now;
        _timer.Start();
        _textView.InvalidateLayer(KnownLayer.Background);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_riga < 1 || textView.Document == null || _riga > textView.Document.LineCount) return;

        // sin² su due semiperiodi: due impulsi; il secondo più tenue
        double t = (DateTime.Now - _inizio).TotalMilliseconds / Durata.TotalMilliseconds;
        if (t >= 1) return;
        double s = Math.Sin(2 * Math.PI * t);
        byte alfa = (byte)(AlfaMassimo * s * s * (1 - 0.5 * t));

        textView.EnsureVisualLines();
        var visualLine = textView.GetVisualLine(_riga);
        if (visualLine == null) return;     // riga fuori dalla parte visibile

        double y = visualLine.VisualTop - textView.ScrollOffset.Y;
        var rect = new Rect(0, y, textView.Bounds.Width, visualLine.Height);
        drawingContext.FillRectangle(new SolidColorBrush(Color.FromArgb(alfa, 255, 80, 0)), rect);
    }
}
