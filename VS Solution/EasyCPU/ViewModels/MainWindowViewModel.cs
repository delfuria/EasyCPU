#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Processore;
using EasyCpu.Backend.Local;
using EasyCpu.Backend.Serializers;
using EasyCpu.Common;
using EasyCPU.Views;

namespace EasyCPU.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DockFactory _factory;
    private bool _atBreakpoint;
    private bool _pendingFirstStep;
    private string? _currentFilePath;
    private IStorageFile? _currentFile;
    private bool _isLegacyFile;

    public Cpu Cpu { get; } = new();
    public Compiler Compiler { get; } = new();
    public SettingsViewModel Settings { get; }
    public IRootDock? Layout { get; private set; }
    public IFactory DockFactory => _factory;

    // Breakpoints: 1-based line numbers (AvaloniaEdit convention)
    public ObservableCollection<int> Breakpoints { get; } = new();

    [ObservableProperty] private int _currentSourceLine = -1;
    [ObservableProperty] private string _statusMessage = "Pronto";
    [ObservableProperty] private string _currentFileName = "Nuovo file";
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _hasCode;

    private static readonly IBrush CpuStatusRed = new SolidColorBrush(Color.Parse("#F44336"));
    private static readonly IBrush CpuStatusGreen = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush CpuStatusYellow = new SolidColorBrush(Color.Parse("#FFEB3B"));

    // Rosso: programma non in esecuzione. Verde: sospeso sulla prima istruzione.
    // Giallo: sospeso su un'istruzione qualsiasi.
    public IBrush CpuStatusColor =>
        Cpu.stop ? CpuStatusRed : Cpu.IP == 0 ? CpuStatusGreen : CpuStatusYellow;

    private void NotifyCpuStatusChanged() => OnPropertyChanged(nameof(CpuStatusColor));

    partial void OnIsDirtyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasCodeChanged(bool value)
    {
        CompileCommand.NotifyCanExecuteChanged();
        RunCommand.NotifyCanExecuteChanged();
        RunUntilCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        StepIntoCommand.NotifyCanExecuteChanged();
        StepOverCommand.NotifyCanExecuteChanged();
        StepOutCommand.NotifyCanExecuteChanged();
    }

    private bool CanSave() => IsDirty;
    private bool CanRunCode() => HasCode;

    internal void MarkDirty() => IsDirty = true;

    internal void RefreshCodeState() =>
        HasCode = !string.IsNullOrWhiteSpace(_factory.CodeEditor?.SourceText);

    public MainViewModel(SettingsViewModel settings)
    {
        Settings = settings;
        _factory = new EasyCPU.DockFactory(this);
        Layout = _factory.CreateLayout();
        _factory.CurrentLayout = Layout;
        _factory.InitLayout(Layout);
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.Theme))
                NotifyThemeProps();
            if (e.PropertyName == nameof(SettingsViewModel.FormatoDati))
            {
                RefreshDebugViews();
                // la scelta fatta dalla toolbar va salvata subito: altrimenti al prossimo
                // avvio si riparte dall'ultimo formato confermato nella finestra Opzioni
                Storage.SalvaOpzioni();
            }
        };
        Breakpoints.CollectionChanged += (_, _) =>
        {
            SyncBreakpointsToCpu();
            // salvati a ogni modifica: nel browser non esiste un evento di uscita affidabile
            if (!_caricamentoBreakpoint) SaveCurrentBreakpoints();
        };

        // Wiring CPU -> pannello Console: invocato dal thread di esecuzione CPU (Task.Run),
        // quindi il post sulla proprietà bindata va marshalled sul thread UI.
        Cpu.ScriviSuConsole += c =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_factory.Console is not { } cv) return;
                // Backspace: toglie l'ultimo carattere, senza risalire alla riga precedente
                if (c == '\b')
                {
                    if (cv.Output.Length > 0 && cv.Output[^1] != '\n')
                        cv.Output = cv.Output[..^1];
                }
                else
                    cv.Output += c;
            });
        };

        // Apre e seleziona automaticamente il pannello Console a ogni "int" valido.
        Cpu.InterruptRichiesto += () => Dispatcher.UIThread.Post(() =>
        {
            IsConsoleVisible = true;
            if (_factory.Console is { } cv) _factory.SetActiveDockable(cv);
        });

        // Cursore lampeggiante mentre la CPU è bloccata in attesa di un tasto (int 21h AX=1).
        Cpu.AttesaTastieraIniziata += () =>
            Dispatcher.UIThread.Post(() => { if (_factory.Console is { } cv) cv.IsInAttesaInput = true; });
        Cpu.AttesaTastieraTerminata += () =>
            Dispatcher.UIThread.Post(() => { if (_factory.Console is { } cv) cv.IsInAttesaInput = false; });
    }

    // ── Breakpoint helpers ────────────────────────────────────────────────────

    public void ToggleBreakpointLine(int lineNumber)
    {
        if (Breakpoints.Contains(lineNumber))
            Breakpoints.Remove(lineNumber);
        else
            Breakpoints.Add(lineNumber);
    }

    private void SyncBreakpointsToCpu()
    {
        Cpu.Breakpoints.Clear();
        if (Compiler.LineToInstrMap == null) return;
        foreach (int lineNumber in Breakpoints)
        {
            int idx = lineNumber - 1;
            if (idx >= 0 && idx < Compiler.LineToInstrMap.Length)
            {
                int instrIdx = Compiler.LineToInstrMap[idx];
                if (instrIdx >= 0)
                    Cpu.Breakpoints.Add(instrIdx);
            }
        }
    }

    private void UpdateCurrentSourceLine()
    {
        if (Compiler.InstrToLineMap == null || Cpu.stop)
        {
            CurrentSourceLine = -1;
            NotifyCpuStatusChanged();
            return;
        }
        int ip = Cpu.IP;
        if (ip >= 0 && ip < Compiler.InstrToLineMap.Count)
            CurrentSourceLine = Compiler.InstrToLineMap[ip] + 1;
        else
            CurrentSourceLine = -1;
        NotifyCpuStatusChanged();
    }

    // ── Visibilità pannelli ───────────────────────────────────────────────────

    public void OnPanelVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsCodeEditorVisible));
        OnPropertyChanged(nameof(IsDataEditorVisible));
        OnPropertyChanged(nameof(IsRegistersVisible));
        OnPropertyChanged(nameof(IsStackVisible));
        OnPropertyChanged(nameof(IsMemoryVisible));
        OnPropertyChanged(nameof(IsErrorsVisible));
        OnPropertyChanged(nameof(IsConsoleVisible));
    }

    private void SetPanelVisible(IDockable? panel, bool visible)
    {
        if (panel is null) return;
        var container = _factory.ContainerFor(panel);
        if (container is null) return;

        var isVisible = _factory.IsPanelVisible(panel);
        if (isVisible == visible) return;

        if (visible)
            _factory.AddDockable(container, panel);
        else
            _factory.RemoveDockable(panel, collapse: false);

        OnPanelVisibilityChanged();
    }

    public bool IsCodeEditorVisible
    {
        get => _factory.IsPanelVisible(_factory.CodeEditor);
        set => SetPanelVisible(_factory.CodeEditor, value);
    }

    public bool IsDataEditorVisible
    {
        get => _factory.IsPanelVisible(_factory.DataEditor);
        set => SetPanelVisible(_factory.DataEditor, value);
    }

    public bool IsRegistersVisible
    {
        get => _factory.IsPanelVisible(_factory.Registers);
        set => SetPanelVisible(_factory.Registers, value);
    }

    public bool IsStackVisible
    {
        get => _factory.IsPanelVisible(_factory.Stack);
        set => SetPanelVisible(_factory.Stack, value);
    }

    public bool IsMemoryVisible
    {
        get => _factory.IsPanelVisible(_factory.Memory);
        set => SetPanelVisible(_factory.Memory, value);
    }

    public bool IsErrorsVisible
    {
        get => _factory.IsPanelVisible(_factory.Errors);
        set => SetPanelVisible(_factory.Errors, value);
    }

    public bool IsConsoleVisible
    {
        get => _factory.IsPanelVisible(_factory.Console);
        set => SetPanelVisible(_factory.Console, value);
    }

    // ── Modello di memoria del programma ─────────────────────────────────────

    // Modalità x86 fedele (memoria a byte): proprietà del programma, salvata nel file .asj
    [ObservableProperty] private bool _isMemoriaAByte;

    private ModelloMemoria ModelloProgramma => IsMemoriaAByte ? ModelloMemoria.Byte : ModelloMemoria.Parole;

    [RelayCommand]
    private void ToggleMemoriaAByte()
    {
        if (Cpu.Stato == Cpu.StatoCpu.Attiva)
        {
            StatusMessage = "Fermare l'esecuzione prima di cambiare la memoria";
            OnPropertyChanged(nameof(IsMemoriaAByte));      // riallinea la spunta
            return;
        }
        IsMemoriaAByte = !IsMemoriaAByte;
        IsDirty = true;
        StatusMessage = IsMemoriaAByte
            ? "Memoria a byte (x86): ricompilare il programma"
            : "Memoria a parole: ricompilare il programma";
    }

    // Il programma compilato non vale più: CPU e pannelli ripartono vuoti con la nuova memoria
    partial void OnIsMemoriaAByteChanged(bool value)
    {
        AzzeraProgramma();
        if (_factory.Memory is { } mv) mv.Title = value ? "Memoria (byte)" : "Memoria";
    }

    // Programma aperto, nuovo o con un'altra memoria: non resta nulla del precedente.
    // Ferma l'esecuzione, dimentica il programma compilato, azzera registri (se da opzione),
    // memoria e stack, svuota i pannelli Errori e Console e aggiorna subito i pannelli.
    private void AzzeraProgramma()
    {
        Cpu.Stop();
        Compiler.Azzera();
        Cpu.Init(new List<Instruction>(), new int[ModelloProgramma.Dimensione].ToList(),
            Ambiente.InizializzaRegistri, Ambiente.LoopInfinito, ModelloProgramma);
        Cpu.Stop();                 // pronta ma non in esecuzione: stato rosso
        _atBreakpoint = false;
        _pendingFirstStep = true;
        CurrentSourceLine = -1;
        _factory.Errors?.Errors.Clear();
        if (_factory.Console is { } cv)
        {
            cv.Output = "";
            cv.IsInAttesaInput = false;
        }
        RefreshDebugViews();
        NotifyCpuStatusChanged();
    }

    // ── Stato tema (per radio menu) ──────────────────────────────────────────

    public bool IsThemeLight => Settings.Theme == AppTheme.Light;
    public bool IsThemeDark  => Settings.Theme == AppTheme.Dark;

    private void NotifyThemeProps()
    {
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeDark));
    }

    // ── File ─────────────────────────────────────────────────────────────────

    private static Window? GetOwnerWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private static TopLevel? GetTopLevel() => Avalonia.Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
        ISingleViewApplicationLifetime singleView when singleView.MainView is not null
            => TopLevel.GetTopLevel(singleView.MainView),
        _ => null
    };

    private static string GetDisplayPath(IStorageFile file)
    {
        // Su Android l'URI SAF (content://) di alcuni provider non riporta l'estensione
        // reale nel LocalPath (es. document id opachi): in quel caso ci si affida a
        // file.Name, che Avalonia garantisce coincidere col nome file effettivo.
        try
        {
            var local = file.Path.LocalPath;
            return string.IsNullOrEmpty(Path.GetExtension(local)) ? file.Name : local;
        }
        catch { return file.Name; }
    }

    private void SetEditorText(CodeEditorViewModel? vm, string text)
    {
        if (vm is null) return;
        vm.SourceText = text;
        vm.SetSourceTextAction?.Invoke(text);
        // Se la vista dell'editor Codice non è ancora stata creata (tab Dati attiva),
        // Document.Changed non scatta: HasCode va aggiornato qui.
        RefreshCodeState();
    }

    private static void SetEditorText(DataEditorViewModel? vm, string text)
    {
        if (vm is null) return;
        vm.SourceText = text;
        vm.SetSourceTextAction?.Invoke(text);
    }

    private async Task LoadFromStreamAsync(string path, Stream stream, ISourceSerializer? ser = null)
    {
        ser ??= ISourceSerializer.ForPath(path);
        var (code, data, memoria) = await ser.LoadAsync(stream);
        if (_currentFilePath is not null) SaveBreakpoints(_currentFilePath);
        SetEditorText(_factory.CodeEditor, string.Join("\n", code));
        SetEditorText(_factory.DataEditor, string.Join("\n", data));
        _currentFilePath = path;
        CurrentFileName = Path.GetFileName(path);
        _isLegacyFile = !path.EndsWith(".asj", StringComparison.OrdinalIgnoreCase);
        IsMemoriaAByte = ModelloMemoria.DaNomeFile(memoria) == ModelloMemoria.Byte;
        RicaricaBreakpoints(path);
        await AddToRecentFilesAsync(path);
        IsDirty = false;
        StatusMessage = IsMemoriaAByte
            ? $"Aperto: {Path.GetFileName(path)} (memoria a byte)"
            : $"Aperto: {Path.GetFileName(path)}";

        // il file appena aperto non è ancora stato compilato/eseguito
        AzzeraProgramma();
    }

    private async void OpenFileFromPath(string path)
    {
        _currentFile = null;
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                // nel browser il file non è riapribile dal percorso: si usa la copia conservata
                var json = Storage.Archivio.Leggi(ArchivioFile.PrefissoProgramma + path);
                if (json is null)
                {
                    StatusMessage = $"File non trovato: {Path.GetFileName(path)}";
                    RimuoviRecente(path);
                    return;
                }
                using var copia = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await LoadFromStreamAsync(path, copia, new EasyFileSerializer());
                return;
            }
            using var stream = File.OpenRead(path);
            await LoadFromStreamAsync(path, stream);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore apertura: {ex.Message}";
        }
    }

    [ObservableProperty] private bool _isConfirmDiscardOpen;
    private TaskCompletionSource<RispostaSalvataggio>? _confirmDiscardTcs;

    // Se ci sono modifiche non salvate, chiede all'utente cosa fare.
    // Restituisce true se l'operazione (Nuovo/Apri) deve proseguire.
    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!IsDirty) return true;

        var owner = GetOwnerWindow();
        RispostaSalvataggio risposta;
        if (owner is not null)
        {
            risposta = await new ConfermaSalvataggioWindow().ShowDialog<RispostaSalvataggio>(owner);
        }
        else
        {
            _confirmDiscardTcs = new TaskCompletionSource<RispostaSalvataggio>();
            IsConfirmDiscardOpen = true;
            risposta = await _confirmDiscardTcs.Task;
        }

        switch (risposta)
        {
            case RispostaSalvataggio.Annulla:
                return false;
            case RispostaSalvataggio.Salva:
                await Save();
                return !IsDirty;
            default:
                return true;
        }
    }

    [RelayCommand]
    private void ConfirmDiscardRespond(RispostaSalvataggio risposta)
    {
        IsConfirmDiscardOpen = false;
        _confirmDiscardTcs?.TrySetResult(risposta);
    }

    [ObservableProperty] private bool _isSospendiOpen;
    private TaskCompletionSource<ModoSospendi>? _sospendiTcs;

    // Mostra il dialog di loop infinito: finestra modale su desktop, overlay su mobile/browser.
    private async Task<ModoSospendi> ShowSospendiAsync()
    {
        var owner = GetOwnerWindow();
        if (owner is not null)
            return await new SospendiWindow().ShowDialog<ModoSospendi>(owner);

        _sospendiTcs = new TaskCompletionSource<ModoSospendi>();
        IsSospendiOpen = true;
        return await _sospendiTcs.Task;
    }

    [RelayCommand]
    private void SospendiRespond(ModoSospendi modo)
    {
        IsSospendiOpen = false;
        _sospendiTcs?.TrySetResult(modo);
    }

    [RelayCommand]
    private async Task New()
    {
        if (!await ConfirmDiscardChangesAsync()) return;

        if (_currentFilePath is not null) SaveBreakpoints(_currentFilePath);
        SetEditorText(_factory.CodeEditor, "");
        SetEditorText(_factory.DataEditor, "");
        _caricamentoBreakpoint = true;
        try { Breakpoints.Clear(); }
        finally { _caricamentoBreakpoint = false; }
        _currentFilePath = null;
        _currentFile = null;
        _isLegacyFile = false;
        IsMemoriaAByte = false;
        AzzeraProgramma();
        CurrentFileName = "Nuovo file";
        IsDirty = false;
        StatusMessage = "Nuovo file";
    }

    [RelayCommand]
    private async Task Open()
    {
        if (!await ConfirmDiscardChangesAsync()) return;

        var owner = GetTopLevel();
        if (owner is null) return;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Apri",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Easy CPU (*.asj; *.as)") { Patterns = ["*.asj", "*.as"] },
                new FilePickerFileType("Tutti i file")           { Patterns = ["*.*"] }
            }
        });
        if (files.Count == 0) return;

        var file = files[0];
        try
        {
            await using var stream = await file.OpenReadAsync();
            await LoadFromStreamAsync(GetDisplayPath(file), stream);
            _currentFile = file;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore apertura: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        // nel browser un programma riaperto dai recenti non ha un file associato: si chiede dove salvarlo
        if (_currentFilePath is null || _isLegacyFile || (OperatingSystem.IsBrowser() && _currentFile is null))
            await SaveToPickedPath();
        else
            await SaveToPath(_currentFilePath);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAs() => await SaveToPickedPath();

    private async Task SaveToPickedPath()
    {
        var owner = GetTopLevel();
        if (owner is null) return;

        var suggested = _currentFilePath is not null
            ? Path.GetFileNameWithoutExtension(_currentFilePath)
            : "file1";

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salva come",
            DefaultExtension = ".asj",
            SuggestedFileName = suggested,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Easy CPU JSON (*.asj)") { Patterns = ["*.asj"] }
            }
        });
        if (file is null) return;

        _currentFile = file;
        _currentFilePath = GetDisplayPath(file);
        CurrentFileName = Path.GetFileName(_currentFilePath);
        _isLegacyFile = false;
        await SaveToPath(_currentFilePath);
    }

    private async Task SaveToPath(string path)
    {
        try
        {
            var code = Righe(_factory.CodeEditor?.SourceText);
            var data = Righe(_factory.DataEditor?.SourceText);

            await using (var stream = _currentFile is not null
                ? await _currentFile.OpenWriteAsync()
                : File.Create(path))
            {
                if (stream.CanSeek) stream.SetLength(0);
                await new EasyFileSerializer().SaveAsync(stream, code, data, ModelloProgramma.NomeFile);
            }

            SaveBreakpoints(path);
            await AddToRecentFilesAsync(path);
            IsDirty = false;
            StatusMessage = $"Salvato: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore salvataggio: {ex.Message}";
        }
    }

    // Dati e codice su carta. Compilatore a parte: non tocca il programma caricato nella CPU.
    // Se i dati non compilano si stampa comunque, senza la colonna degli indirizzi.
    [RelayCommand]
    private async Task Print()
    {
        var codice = Righe(_factory.CodeEditor?.SourceText);
        var dati = Righe(_factory.DataEditor?.SourceText);

        var compilatore = new Compiler { Modello = ModelloProgramma };
        List<CompilerError> errori = null!;
        compilatore.CompilaDati([.. dati], ref errori, [.. codice]);

        var versione = typeof(Stampa).Assembly.GetName().Version?.ToString(2);
        var sottotitolo = $"EasyCPU {versione} · memoria a {(IsMemoriaAByte ? "byte" : "parole")} · {DateTime.Now:g}";
        var html = Stampa.GeneraHtml(CurrentFileName, sottotitolo, dati, errori is null ? compilatore.CelleDati : null,
            codice, App.EasyCpuHighlighting(ThemeVariant.Light));

        try
        {
            if (!await Stampa.ApriAsync(GetTopLevel(), html))
                StatusMessage = "Impossibile stampare";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore stampa: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    // ── File recenti ──────────────────────────────────────────────────────────

    public ObservableCollection<RecentFileItem> RecentFileItems { get; } = new();

    public bool HasRecentFiles => RecentFileItems.Count > 0;

    private static string[] Righe(string? testo) =>
        (testo ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

    private async Task AddToRecentFilesAsync(string path)
    {
        var precedenti = Ambiente.FileRecenti.ToList();
        Ambiente.AggiungiRecenti(path);
        if (OperatingSystem.IsBrowser())
            await ConservaProgrammaAsync(path, precedenti);
        SalvaRecenti();
        RefreshRecentFileItems();
    }

    // Nel browser conserva una copia del programma per poterlo riaprire dai recenti,
    // ed elimina le copie dei programmi usciti dall'elenco.
    private async Task ConservaProgrammaAsync(string path, List<string> precedenti)
    {
        try
        {
            using var copia = new MemoryStream();
            await new EasyFileSerializer().SaveAsync(copia, Righe(_factory.CodeEditor?.SourceText),
                Righe(_factory.DataEditor?.SourceText), ModelloProgramma.NomeFile);
            Storage.Archivio.Scrivi(ArchivioFile.PrefissoProgramma + path, Encoding.UTF8.GetString(copia.ToArray()));
            foreach (var uscito in precedenti.Except(Ambiente.FileRecenti))
                Storage.Archivio.Elimina(ArchivioFile.PrefissoProgramma + uscito);
        }
        catch { }
    }

    private void RimuoviRecente(string path)
    {
        Ambiente.FileRecenti.Remove(path);
        if (OperatingSystem.IsBrowser())
            Storage.Archivio.Elimina(ArchivioFile.PrefissoProgramma + path);
        SalvaRecenti();
        RefreshRecentFileItems();
    }

    private static void SalvaRecenti()
    {
        try { Storage.SalvaFileRecenti(); }
        catch { }
    }

    public void RefreshRecentFileItems()
    {
        RecentFileItems.Clear();
        foreach (var path in Ambiente.FileRecenti)
            RecentFileItems.Add(new RecentFileItem(path, OpenFileFromPath));
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    [RelayCommand]
    private void OpenRecentFile(string path)
    {
        if (!File.Exists(path))
        {
            StatusMessage = $"File non trovato: {Path.GetFileName(path)}";
            RimuoviRecente(path);
            return;
        }
        OpenFileFromPath(path);
    }

    // ── Layout persistence ────────────────────────────────────────────────────

    internal void SaveLayout()
    {
        try
        {
            if (Layout is null) return;
            var node = _factory.ToDockNode(Layout);
            Storage.Archivio.Scrivi("layout", JsonSerializer.Serialize(node, LayoutJsonContext.Default.DockNode));
        }
        catch { }
    }

    internal void LoadLayout()
    {
        try
        {
            var json = Storage.Archivio.Leggi("layout");
            if (json is null) return;
            var node = JsonSerializer.Deserialize(json, LayoutJsonContext.Default.DockNode);
            if (node is null) return;

            var all = new Dictionary<string, IDockable?>
            {
                ["CodeEditor"] = _factory.CodeEditor,
                ["DataEditor"] = _factory.DataEditor,
                ["Registers"]  = _factory.Registers,
                ["Stack"]      = _factory.Stack,
                ["Memory"]     = _factory.Memory,
                ["Errors"]     = _factory.Errors,
                ["Console"]    = _factory.Console,
            };

            var newLayout = _factory.RebuildLayout(node, all);
            if (newLayout is null) return;
            Layout = newLayout;
            _factory.CurrentLayout = Layout;
            _factory.InitLayout(Layout);
        }
        catch { }
    }

    // ── Breakpoint persistence ────────────────────────────────────────────────

    // true mentre l'elenco dei breakpoint viene svuotato o ricaricato: non va salvato
    private bool _caricamentoBreakpoint;

    private void SaveBreakpoints(string filePath)
    {
        try
        {
            var chiave = ArchivioFile.PrefissoBreakpoint + filePath;
            if (Breakpoints.Count == 0)
                Storage.Archivio.Elimina(chiave);
            else
                Storage.Archivio.Scrivi(chiave, string.Join("\n", Breakpoints.Select(l => l.ToString())));
        }
        catch { }
    }

    private void RicaricaBreakpoints(string filePath)
    {
        _caricamentoBreakpoint = true;
        try
        {
            Breakpoints.Clear();
            var testo = Storage.Archivio.Leggi(ArchivioFile.PrefissoBreakpoint + filePath);
            if (testo is null) return;
            foreach (var line in testo.Split('\n'))
                if (int.TryParse(line.Trim(), out int lineNum) && lineNum > 0)
                    Breakpoints.Add(lineNum);
        }
        catch { }
        finally { _caricamentoBreakpoint = false; }
    }

    internal void SaveCurrentBreakpoints()
    {
        if (_currentFilePath is not null)
            SaveBreakpoints(_currentFilePath);
    }

    internal void SaveAll()
    {
        SaveLayout();
        SaveCurrentBreakpoints();
        Storage.SalvaFileRecenti();
    }

    // ── Modifica ──────────────────────────────────────────────────────────────

    [RelayCommand] private void Undo()      => _factory.CodeEditor?.UndoAction?.Invoke();
    [RelayCommand] private void Redo()      => _factory.CodeEditor?.RedoAction?.Invoke();
    [RelayCommand] private void Cut()       => _factory.CodeEditor?.CutAction?.Invoke();
    [RelayCommand] private void Copy()      => _factory.CodeEditor?.CopyAction?.Invoke();
    [RelayCommand] private void Paste()     => _factory.CodeEditor?.PasteAction?.Invoke();
    [RelayCommand] private void SelectAll() => _factory.CodeEditor?.SelectAllAction?.Invoke();
    [RelayCommand] private void Find()      => _factory.CodeEditor?.FindAction?.Invoke();

    // ── Esegui ───────────────────────────────────────────────────────────────

    private bool DoCompile()
    {
        if (_factory.Console is { } cv) cv.Output = "";

        var codeEditor = _factory.CodeEditor;
        if (codeEditor == null) return false;

        var codeLines = codeEditor.SourceText
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Split('\n')
            .ToList();

        var dataLines = (_factory.DataEditor?.SourceText ?? "")
            .Replace("\r\n", "\n").Replace("\r", "\n")
            .Split('\n')
            .ToList();

        // i dati prima del codice: il codice usa i nomi definiti nella sezione dati;
        // le etichette del codice sono lette per prime, i dati possono usarle (tab DW caso0, caso1)
        List<CompilerError> dataErrors = null!;
        Compiler.Modello = ModelloProgramma;
        var memory = Compiler.CompilaDati(dataLines, ref dataErrors, codeLines);

        List<CompilerError> codeErrors = null!;
        var instructions = Compiler.CompilaCodice(codeLines, ref codeErrors);

        var ev = _factory.Errors;
        if (ev != null)
        {
            ev.Errors.Clear();
            var sorted = (codeErrors ?? []).Select(e => new CompilerErrorAdapter(e))
                .Concat((dataErrors ?? []).Select(e => new CompilerErrorAdapter(e)))
                .OrderBy(a => a.RigaDisplay)
                .ThenBy(a => a.TipoDisplay);
            foreach (var a in sorted) ev.Errors.Add(a);
        }

        if (instructions == null || memory == null)
        {
            int n = ev?.Errors.Count ?? 0;
            StatusMessage = n == 1 ? "Compilazione: 1 errore" : $"Compilazione: {n} errori";
            IsErrorsVisible = true;
            if (_factory.Errors is { } ep) _factory.SetActiveDockable(ep);
            _factory.Registers?.Svuota();
            _factory.Memory?.Svuota();
            _factory.Stack?.Svuota();
            Cpu.Stop();
            CurrentSourceLine = -1;
            NotifyCpuStatusChanged();
            return false;
        }

        StatusMessage = IsMemoriaAByte ? "Compilazione completata (memoria a byte)" : "Compilazione completata";
        _atBreakpoint = false;
        _pendingFirstStep = true;
        // Celle inizializzate dalla sezione dati evidenziate già al caricamento, anche quelle a 0:
        // la base del confronto è la stessa memoria con ogni cella dichiarata alterata.
        var baseConfronto = memory.ToList();
        foreach (var celle in Compiler.CelleDati)
            if (celle is var (inizio, n))
                for (int i = inizio; i < inizio + n; i++) baseConfronto[i] ^= 1;
        Cpu.Init(instructions, baseConfronto, Ambiente.InizializzaRegistri, Ambiente.LoopInfinito, Compiler.Modello);
        RefreshDebugViews();
        Cpu.Init(instructions, memory, Ambiente.InizializzaRegistri, Ambiente.LoopInfinito, Compiler.Modello);
        SyncBreakpointsToCpu();
        RefreshDebugViews(evidenzia: true);
        NotifyCpuStatusChanged();
        return true;
    }

    [ObservableProperty] private bool _isCompileResultOpen;
    [ObservableProperty] private string _compileResultMessage = "";

    private async Task ShowCompileMessageAsync(string message)
    {
        var owner = GetOwnerWindow();
        if (owner is not null)
        {
            await new MessageBoxWindow(message).ShowDialog(owner);
            return;
        }

        CompileResultMessage = message;
        IsCompileResultOpen = true;
    }

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private async Task Compile()
    {
        bool success = DoCompile();
        if (!success)
            await ShowCompileMessageAsync("Compilazione con errori");
    }

    [RelayCommand]
    private void CloseCompileResult() => IsCompileResultOpen = false;

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private async Task Run()
    {
        if (_atBreakpoint)
        {
            _atBreakpoint = false;
            try { await Cpu.StepInto(); }
            catch (CpuException e) { ShowRuntimeError(e); return; }
            if (Cpu.stop)
            {
                UpdateCurrentSourceLine();
                RefreshDebugViews(evidenzia: true);
                StatusMessage = "Esecuzione terminata";
                return;
            }
        }
        else
        {
            if (!DoCompile())
            {
                await ShowCompileMessageAsync("Compilazione con errori");
                return;
            }
        }

        _pendingFirstStep = false;
        while (true)
        {
            try
            {
                await Cpu.Run();
                break;
            }
            catch (CpuTrapException) { _atBreakpoint = true; break; }
            catch (CpuLoopException)
            {
                var modo = await ShowSospendiAsync();
                if (modo == ModoSospendi.Continua) continue;
                if (modo == ModoSospendi.Pausa) { _atBreakpoint = true; break; }
                Cpu.Stop();
                break;
            }
            catch (CpuException e) { ShowRuntimeError(e); return; }
        }

        UpdateCurrentSourceLine();
        RefreshDebugViews(evidenzia: true);
        if (_atBreakpoint)
            StatusMessage = CurrentSourceLine > 0
                ? $"Breakpoint — riga {CurrentSourceLine}"
                : "Breakpoint raggiunto";
        else if (Cpu.stop)
            StatusMessage = "Esecuzione terminata";
    }

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private async Task RunUntil()
    {
        int line = _factory.CodeEditor?.CurrentLine ?? 0;
        if (line <= 0) return;

        if (_atBreakpoint)
        {
            _atBreakpoint = false;
            try { await Cpu.StepInto(); }
            catch (CpuException e) { ShowRuntimeError(e); return; }
            if (Cpu.stop)
            {
                UpdateCurrentSourceLine();
                RefreshDebugViews(evidenzia: true);
                StatusMessage = "Esecuzione terminata";
                return;
            }
        }
        else
        {
            if (!DoCompile())
            {
                await ShowCompileMessageAsync("Compilazione con errori");
                return;
            }
        }

        int idx = line - 1;
        int instrIdx = (Compiler.LineToInstrMap != null && idx >= 0 && idx < Compiler.LineToInstrMap.Length)
            ? Compiler.LineToInstrMap[idx]
            : -1;
        bool tempBreakpoint = instrIdx >= 0 && Cpu.Breakpoints.Add(instrIdx);

        _pendingFirstStep = false;
        while (true)
        {
            try
            {
                await Cpu.Run();
                break;
            }
            catch (CpuTrapException) { _atBreakpoint = true; break; }
            catch (CpuLoopException)
            {
                var modo = await ShowSospendiAsync();
                if (modo == ModoSospendi.Continua) continue;
                if (modo == ModoSospendi.Pausa) { _atBreakpoint = true; break; }
                Cpu.Stop();
                break;
            }
            catch (CpuException e)
            {
                if (tempBreakpoint)
                    Cpu.Breakpoints.Remove(instrIdx);
                ShowRuntimeError(e);
                return;
            }
        }

        if (tempBreakpoint)
            Cpu.Breakpoints.Remove(instrIdx);

        UpdateCurrentSourceLine();
        RefreshDebugViews(evidenzia: true);
        if (_atBreakpoint)
            StatusMessage = CurrentSourceLine > 0
                ? $"Breakpoint — riga {CurrentSourceLine}"
                : "Breakpoint raggiunto";
        else if (Cpu.stop)
            StatusMessage = "Esecuzione terminata";
    }

    // Se la CPU non è avviata (mai compilata, o esecuzione terminata), la prepara
    // posizionandola sulla prima riga senza eseguire alcun passo.
    // Restituisce true se il chiamante deve procedere con lo step effettivo.
    private async Task<bool> PrepareForStepAsync()
    {
        bool alreadyCompiled = Compiler.InstrToLineMap != null && !Cpu.stop;

        if (alreadyCompiled && !_pendingFirstStep)
            return true;

        if (!alreadyCompiled && !DoCompile())
        {
            await ShowCompileMessageAsync("Compilazione con errori");
            return false;
        }

        // pannelli già aggiornati dalla compilazione, con i dati evidenziati fino al primo passo
        _pendingFirstStep = false;
        UpdateCurrentSourceLine();
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
        return false;
    }

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private async Task StepInto()
    {
        if (!await PrepareForStepAsync()) return;
        _atBreakpoint = false;
        try
        {
            await Cpu.StepInto();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuException e) { ShowRuntimeError(e); return; }
        UpdateCurrentSourceLine();
        RefreshDebugViews(evidenzia: true);
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
    }

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private async Task StepOver()
    {
        if (!await PrepareForStepAsync()) return;
        _atBreakpoint = false;
        try
        {
            await Cpu.StepOver();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuException e) { ShowRuntimeError(e); return; }
        UpdateCurrentSourceLine();
        RefreshDebugViews(evidenzia: true);
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
    }

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private async Task StepOut()
    {
        if (!await PrepareForStepAsync()) return;
        _atBreakpoint = false;
        try
        {
            await Cpu.StepOut();
        }
        catch (CpuTrapException) { _atBreakpoint = true; }
        catch (CpuException e) { ShowRuntimeError(e); return; }
        UpdateCurrentSourceLine();
        RefreshDebugViews(evidenzia: true);
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
    }

    // Mostra nel pannello Errori l'errore rilevato dalla CPU, sulla riga dell'istruzione che l'ha causato
    private void ShowRuntimeError(CpuException e)
    {
        int ip = Cpu.IP;
        var map = Compiler.InstrToLineMap;
        int riga = (map != null && ip >= 0 && ip < map.Count) ? map[ip] : -1;
        var err = new CompilerError(Errori.Msg(e.err), riga, 0, CompilerError.ESECUZIONE);

        if (_factory.Errors is { } ev)
        {
            ev.Errors.Clear();
            ev.Errors.Add(new CompilerErrorAdapter(err));
            IsErrorsVisible = true;
            _factory.SetActiveDockable(ev);
        }
        UpdateCurrentSourceLine();
        RefreshDebugViews(evidenzia: true);
        StatusMessage = riga >= 0
            ? $"Errore di esecuzione alla riga {riga + 1}: {err.Msg}"
            : $"Errore di esecuzione: {err.Msg}";
    }

    [RelayCommand(CanExecute = nameof(CanRunCode))]
    private void Stop()
    {
        Cpu.Stop();
        _atBreakpoint = false;
        CurrentSourceLine = -1;
        NotifyCpuStatusChanged();
        RefreshDebugViews();
        StatusMessage = "Esecuzione interrotta";
    }

    [RelayCommand]
    private void ToggleBreakpoint()
    {
        int line = _factory.CodeEditor?.CurrentLine ?? 0;
        if (line > 0)
            ToggleBreakpointLine(line);
    }

    // ── Finestre ─────────────────────────────────────────────────────────────

    [RelayCommand] private void ToggleCodeEditor() => IsCodeEditorVisible = !IsCodeEditorVisible;
    [RelayCommand] private void ToggleDataEditor()  => IsDataEditorVisible = !IsDataEditorVisible;
    [RelayCommand] private void ToggleRegisters()   => IsRegistersVisible  = !IsRegistersVisible;
    [RelayCommand] private void ToggleStack()       => IsStackVisible      = !IsStackVisible;
    [RelayCommand] private void ToggleMemory()      => IsMemoryVisible     = !IsMemoryVisible;
    [RelayCommand] private void ToggleErrors()      => IsErrorsVisible     = !IsErrorsVisible;
    [RelayCommand] private void ToggleConsole()     => IsConsoleVisible    = !IsConsoleVisible;

    [RelayCommand]
    private void ResetLayout()
    {
        Layout = _factory.CreateLayout();
        _factory.InitLayout(Layout!);
        OnPropertyChanged(nameof(Layout));
        OnPanelVisibilityChanged();
    }

    // ── Strumenti ────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isOptionsOpen;
    [ObservableProperty] private OpzioniViewModel? _opzioniVm;

    [RelayCommand]
    private async Task ShowOptions()
    {
        var owner = GetOwnerWindow();
        if (owner is not null)
        {
            var vm = new OpzioniViewModel(Settings);
            var dialog = new OpzioniWindow { DataContext = vm };
            var ok = await dialog.ShowDialog<bool>(owner);
            if (ok)
            {
                vm.ApplyTo(Settings);
                Storage.SalvaOpzioni();
                RefreshDebugViews();
            }
            return;
        }

        // Nessuna Window disponibile (Browser/iOS/Android, single-view lifetime):
        // mostra le opzioni come overlay all'interno di MainView.
        OpzioniVm = new OpzioniViewModel(Settings);
        IsOptionsOpen = true;
    }

    [RelayCommand]
    private void ConfirmOptions()
    {
        if (OpzioniVm is null) return;
        OpzioniVm.ApplyTo(Settings);
        Storage.SalvaOpzioni();
        RefreshDebugViews();
        IsOptionsOpen = false;
        OpzioniVm = null;
    }

    [RelayCommand]
    private void CancelOptions()
    {
        IsOptionsOpen = false;
        OpzioniVm = null;
    }

    [ObservableProperty] private bool _isAboutOpen;

    [RelayCommand]
    private async Task ShowAbout()
    {
        var owner = GetOwnerWindow();
        if (owner is not null)
        {
            await new AboutWindow().ShowDialog(owner);
            return;
        }

        // Nessuna Window disponibile (Browser/iOS/Android, single-view lifetime):
        // mostra le informazioni come overlay all'interno di MainView.
        IsAboutOpen = true;
    }

    [RelayCommand]
    private void CloseAbout() => IsAboutOpen = false;

    // F1: guida aperta sull'istruzione sotto il caret dell'editor codice, sulla sezione
    // dati dall'editor dati, altrimenti dall'inizio. Parole senza ancora aprono l'inizio.
    [RelayCommand]
    private async Task ShowGuida()
    {
        var top = GetTopLevel();
        string? ancora = null;
        if (top?.FocusManager?.GetFocusedElement() is Avalonia.Visual focus)
        {
            if (focus.FindAncestorOfType<CodeEditorView>(includeSelf: true) is not null)
                ancora = _factory.CodeEditor?.WordAtCaretFunc?.Invoke().ToLowerInvariant();
            else if (focus.FindAncestorOfType<DataEditorView>(includeSelf: true) is not null)
                ancora = "la-sezione-dati";
        }
        if (!await Guida.ApriAsync(top, GetOwnerWindow() is not null, ancora))
            StatusMessage = "Impossibile aprire la guida";
    }

    [RelayCommand] private void SetThemeLight() => Settings.Theme = AppTheme.Light;
    [RelayCommand] private void SetThemeDark()  => Settings.Theme = AppTheme.Dark;

    // ── Debug views ───────────────────────────────────────────────────────────

    // evidenzia: dopo un passo o un'esecuzione, i valori cambiati hanno uno sfondo diverso
    private void RefreshDebugViews(bool evidenzia = false)
    {
        var regs = Cpu.DumpRegs();
        _factory.Registers?.Aggiorna(
            [.. regs, $"C={(Cpu.FlagCarry ? 1 : 0)}  Z={(Cpu.FlagZero ? 1 : 0)}  S={(Cpu.FlagSegno ? 1 : 0)}  O={(Cpu.FlagOverflow ? 1 : 0)}  D={(Cpu.FlagDirezione ? 1 : 0)}"],
            evidenzia);

        // memoria a byte: 16 byte per riga; memoria a parole: 8 celle per riga
        var mem = Cpu.Modello == ModelloMemoria.Byte
            ? Cpu.DumpByte(0, Cpu.Modello.InizioStack, 16)
            : Cpu.DumpMemoria(0, Cpu.Modello.InizioStack, 8);
        if (mem is not null && Compiler.Simboli.Count > 0)
            mem.AddRange(["", "Simboli:", .. Cpu.DumpSimboli(Compiler.Simboli)]);
        _factory.Memory?.Aggiorna(mem ?? [], evidenzia);

        var stack = Cpu.DumpMemoria(Cpu.Modello.InizioStack, Cpu.Modello.Dimensione, Ambiente.ColonneStack);
        _factory.Stack?.Aggiorna(stack ?? [], evidenzia);
    }

    public void NavigateToError(CompilerError err)
    {
        if (err.Riga < 0) return;
        int lineNumber = err.Riga + 1;
        if (err.Tipo != CompilerError.DATI)     // errori di codice e di esecuzione
        {
            if (_factory.CodeEditor is not { } editor) return;
            _factory.SetActiveDockable(editor);
            editor.NavigateToLineAction?.Invoke(lineNumber);
        }
        else
        {
            if (_factory.DataEditor is not { } editor) return;
            _factory.SetActiveDockable(editor);
            editor.NavigateToLineAction?.Invoke(lineNumber);
        }
    }
}
