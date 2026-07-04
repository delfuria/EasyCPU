#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using EasyCpu.Common;
using EasyCPU.ViewModels;
using EasyCPU.Views.Editor;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Highlighting;
using AvaInput = Avalonia.Input;

namespace EasyCPU.Views;

public partial class CodeEditorView : UserControl
{
    private TextEditor? _editor;
    private bool _suppressDirty;

    public CodeEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<TextEditor>("Editor");
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_editor == null) return;
        if (DataContext is CodeEditorViewModel vm)
            SetupEditor(vm);
    }

    private void SetupEditor(CodeEditorViewModel vm)
    {
        if (_editor == null) return;

        _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("EasyCPU");

        // Font: applica subito e aggiorna quando l'utente cambia le opzioni
        var settings = SettingsViewModel.Instance;
        ApplyFont(_editor, settings);
        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SettingsViewModel.FontEditorNome)
                               or nameof(SettingsViewModel.FontEditorSize)
                               or nameof(SettingsViewModel.FontEditorStyle))
                ApplyFont(_editor, settings);
        };

        // Breakpoint margin visuale — inserito prima del margine numeri di riga
        var bpMargin = new BreakpointMargin(vm.MainVm);
        _editor.TextArea.LeftMargins.Insert(0, bpMargin);

        // Renderer che evidenzia la riga corrente dell'IP
        var lineRenderer = new DebugCurrentLineRenderer(vm.MainVm);
        _editor.TextArea.TextView.BackgroundRenderers.Add(lineRenderer);

        // Invalidare il layer di sfondo quando CurrentSourceLine cambia
        vm.MainVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentSourceLine))
                _editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        };

        // Carica il testo salvato nel ViewModel (es. dopo hide/show del pannello)
        if (!string.IsNullOrEmpty(vm.SourceText))
            _editor.Document.Text = vm.SourceText;

        // Mantiene SourceText sincronizzato con il documento
        _editor.Document.Changed += (_, _) =>
        {
            vm.SourceText = _editor.Document.Text;
            vm.MainVm.RefreshCodeState();
            if (!_suppressDirty) vm.MainVm.MarkDirty();
        };

        // Mantiene CurrentLine sincronizzato con il caret (1-based)
        _editor.TextArea.Caret.PositionChanged += (_, _) =>
            vm.CurrentLine = _editor.TextArea.Caret.Line;

        // Tab e Enter personalizzati (EasyEditor: margine sinistro)
        _editor.TextArea.TextEntering += OnTextEntering;

        // ── Operazioni Modifica ───────────────────────────────────────────────
        vm.UndoAction      = () => _editor.Undo();
        vm.RedoAction      = () => _editor.Redo();
        vm.CutAction       = CutSelection;
        vm.CopyAction      = CopySelection;
        vm.PasteAction     = PasteFromClipboard;
        vm.SelectAllAction = () => _editor.TextArea.Selection =
            Selection.Create(_editor.TextArea, 0, _editor.Document.TextLength);
        vm.FindAction      = () => SearchPanel.Install(_editor).Open();
        vm.SetSourceTextAction = text =>
        {
            _suppressDirty = true;
            _editor.Document.Text = text;
            _editor.TextArea.Caret.Line = 1;
            _editor.TextArea.Caret.Column = 1;
            _suppressDirty = false;
        };

        vm.NavigateToLineAction = lineNumber => NavigateToLineWhenReady(lineNumber);
    }

    // Su Android/iOS il cambio di tab del docking (SetActiveDockable) può completare
    // l'attach del native view un frame dopo il tab-switch. Scroll/caret/focus vanno
    // quindi eseguiti insieme SOLO quando l'editor è davvero nell'albero visivo,
    // altrimenti il caret viene calcolato su un TextView non ancora misurato e si
    // perde al layout successivo. Se non è ancora attaccato, aspettiamo l'evento reale.
    private void NavigateToLineWhenReady(int lineNumber)
    {
        if (_editor is null) return;

        if (_editor.IsAttachedToVisualTree())
        {
            DoNavigateToLine(lineNumber);
            return;
        }

        void OnAttached(object? s, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            _editor.AttachedToVisualTree -= OnAttached;
            Dispatcher.UIThread.Post(() => DoNavigateToLine(lineNumber), DispatcherPriority.Loaded);
        }
        _editor.AttachedToVisualTree += OnAttached;
    }

    private void DoNavigateToLine(int lineNumber)
    {
        if (_editor is null || _editor.Document.LineCount == 0) return;
        int n = Math.Clamp(lineNumber, 1, _editor.Document.LineCount);
        _editor.ScrollTo(n, 1);
        var docLine = _editor.Document.GetLineByNumber(n);
        var text    = _editor.Document.GetText(docLine.Offset, docLine.Length);
        int leading = text.Length - text.TrimStart().Length;
        _editor.TextArea.Caret.Line   = n;
        _editor.TextArea.Caret.Column = leading < text.Length ? leading + 1 : 1;
        _editor.TextArea.Focus();
        Dispatcher.UIThread.Post(() => _editor.TextArea.Focus(), DispatcherPriority.Background);
        // Su iOS il gesture recognizer nativo del doppio tap sulla DataGridCell degli
        // Errori completa la propria selezione/focus con ~60-90ms di ritardo rispetto
        // al PointerPressed che Avalonia consegna subito, riportando il focus lì e
        // facendo sparire il caret. Rivendichiamo il focus una terza volta dopo un
        // ritardo che copre questa finestra.
        _ = ReclaimFocusAfterDelay();
    }

    private async Task ReclaimFocusAfterDelay()
    {
        await Task.Delay(300);
        _editor?.TextArea.Focus();
    }

    private void CopySelection()
    {
        var text = _editor?.SelectedText;
        if (string.IsNullOrEmpty(text)) return;
        var clipboard = TopLevel.GetTopLevel(_editor)?.Clipboard;
        if (clipboard == null) return;
        var item = AvaInput.DataTransferItem.CreateText(text);
        var transfer = new AvaInput.DataTransfer();
        transfer.Add(item);
        _ = clipboard.SetDataAsync(transfer);
    }

    private void CutSelection()
    {
        var text = _editor?.SelectedText;
        if (string.IsNullOrEmpty(text)) return;
        var clipboard = TopLevel.GetTopLevel(_editor)?.Clipboard;
        if (clipboard == null) return;
        var item = AvaInput.DataTransferItem.CreateText(text);
        var transfer = new AvaInput.DataTransfer();
        transfer.Add(item);
        _ = clipboard.SetDataAsync(transfer);
        _editor!.TextArea.Selection.ReplaceSelectionWithText("");
    }

    private async void PasteFromClipboard()
    {
        var clipboard = TopLevel.GetTopLevel(_editor)?.Clipboard;
        if (clipboard == null) return;
        var data = await clipboard.TryGetDataAsync();
        if (data == null) return;
        var text = await AvaInput.AsyncDataTransferExtensions.TryGetTextAsync(data);
        if (text != null)
            _editor!.TextArea.Selection.ReplaceSelectionWithText(text);
    }

    private static void ApplyFont(TextEditor editor, SettingsViewModel s)
    {
        if (!string.IsNullOrWhiteSpace(s.FontEditorNome))
            editor.FontFamily = new FontFamily(s.FontEditorNome);
        if (s.FontEditorSize > 0)
            editor.FontSize = s.FontEditorSize;
        editor.FontWeight = (s.FontEditorStyle & 1) != 0 ? FontWeight.Bold   : FontWeight.Normal;
        editor.FontStyle  = (s.FontEditorStyle & 2) != 0 ? FontStyle.Italic  : FontStyle.Normal;
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (_editor == null) return;
        int margin = Ambiente.MargineSinistro;

        if (e.Text == "\t")
        {
            // Tab → spazi fino al prossimo multiplo di MargineSinistro
            e.Handled = true;
            int col = _editor.TextArea.Caret.Column - 1; // 0-based
            int spaces = margin - (col % margin);
            _editor.TextArea.Document.Insert(
                _editor.TextArea.Caret.Offset,
                new string(' ', spaces));
        }
        else if (e.Text == "\n")
        {
            // Enter → nuova riga con lo stesso indent della riga corrente
            e.Handled = true;
            var doc      = _editor.TextArea.Document;
            var line     = doc.GetLineByNumber(_editor.TextArea.Caret.Line);
            var lineText = doc.GetText(line.Offset, line.Length);
            string indent = "";
            if (!string.IsNullOrWhiteSpace(lineText))
            {
                int leading = lineText.Length - lineText.TrimStart().Length;
                indent = lineText[..leading];
            }
            doc.Insert(_editor.TextArea.Caret.Offset, Environment.NewLine + indent);
        }
    }
}
