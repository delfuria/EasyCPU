#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
                RefreshDebugViews();
        };
        Breakpoints.CollectionChanged += (_, _) => SyncBreakpointsToCpu();

        // Wiring CPU -> pannello Console: invocato dal thread di esecuzione CPU (Task.Run),
        // quindi il post sulla proprietà bindata va marshalled sul thread UI.
        Cpu.ScriviSuConsole += c =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_factory.Console is { } cv) cv.Output += c;
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

    // ── Stato tema (per radio menu) ──────────────────────────────────────────

    public bool IsThemeLight => Settings.Theme == AppTheme.Light;
    public bool IsThemeDark  => Settings.Theme == AppTheme.Dark;

    private void NotifyThemeProps()
    {
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeDark));
    }

    // ── File ─────────────────────────────────────────────────────────────────

    private static readonly string LayoutFilePath =
        Path.Combine(Ambiente.EasyCPUPath, "layout.json");

    private static readonly JsonSerializerOptions LayoutJsonOpts = new()
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

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

    private static void SetEditorText(CodeEditorViewModel? vm, string text)
    {
        if (vm is null) return;
        vm.SourceText = text;
        vm.SetSourceTextAction?.Invoke(text);
    }

    private static void SetEditorText(DataEditorViewModel? vm, string text)
    {
        if (vm is null) return;
        vm.SourceText = text;
        vm.SetSourceTextAction?.Invoke(text);
    }

    private async Task LoadFromStreamAsync(string path, Stream stream)
    {
        var ser = ISourceSerializer.ForPath(path);
        var (code, data) = await ser.LoadAsync(stream);
        if (_currentFilePath is not null) SaveBreakpoints(_currentFilePath);
        SetEditorText(_factory.CodeEditor, string.Join("\n", code));
        SetEditorText(_factory.DataEditor, string.Join("\n", data));
        _currentFilePath = path;
        CurrentFileName = Path.GetFileName(path);
        _isLegacyFile = !path.EndsWith(".asj", StringComparison.OrdinalIgnoreCase);
        Breakpoints.Clear();
        LoadBreakpoints(path);
        AddToRecentFiles(path);
        IsDirty = false;
        StatusMessage = $"Aperto: {Path.GetFileName(path)}";
    }

    private async void OpenFileFromPath(string path)
    {
        _currentFile = null;
        try
        {
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
        Breakpoints.Clear();
        _currentFilePath = null;
        _currentFile = null;
        _isLegacyFile = false;
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
        if (_currentFilePath is null || _isLegacyFile)
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
            var code = (_factory.CodeEditor?.SourceText ?? "")
                .Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var data = (_factory.DataEditor?.SourceText ?? "")
                .Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            await using (var stream = _currentFile is not null
                ? await _currentFile.OpenWriteAsync()
                : File.Create(path))
            {
                if (stream.CanSeek) stream.SetLength(0);
                await new EasyFileSerializer().SaveAsync(stream, code, data);
            }

            SaveBreakpoints(path);
            AddToRecentFiles(path);
            IsDirty = false;
            StatusMessage = $"Salvato: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore salvataggio: {ex.Message}";
        }
    }

    [RelayCommand] private void Print() { }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    // ── File recenti ──────────────────────────────────────────────────────────

    public ObservableCollection<RecentFileItem> RecentFileItems { get; } = new();

    private void AddToRecentFiles(string path)
    {
        Ambiente.AggiungiRecenti(path);
        RefreshRecentFileItems();
    }

    public void RefreshRecentFileItems()
    {
        RecentFileItems.Clear();
        foreach (var path in Ambiente.FileRecenti)
            RecentFileItems.Add(new RecentFileItem(path, OpenFileFromPath));
    }

    [RelayCommand]
    private void OpenRecentFile(string path)
    {
        if (!File.Exists(path))
        {
            StatusMessage = $"File non trovato: {Path.GetFileName(path)}";
            Ambiente.FileRecenti.Remove(path);
            RefreshRecentFileItems();
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
            Directory.CreateDirectory(Ambiente.EasyCPUPath);
            var node = _factory.ToDockNode(Layout);
            File.WriteAllText(LayoutFilePath, JsonSerializer.Serialize(node, LayoutJsonOpts));
        }
        catch { }
    }

    internal void LoadLayout()
    {
        try
        {
            if (!File.Exists(LayoutFilePath)) return;
            var node = JsonSerializer.Deserialize<DockNode>(File.ReadAllText(LayoutFilePath), LayoutJsonOpts);
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

    private void SaveBreakpoints(string filePath)
    {
        try
        {
            var bkptFile = filePath + ".bkpt";
            if (Breakpoints.Count == 0)
            {
                if (File.Exists(bkptFile)) File.Delete(bkptFile);
            }
            else
            {
                File.WriteAllLines(bkptFile, Breakpoints.Select(l => l.ToString()));
            }
        }
        catch { }
    }

    private void LoadBreakpoints(string filePath)
    {
        try
        {
            var bkptFile = filePath + ".bkpt";
            if (!File.Exists(bkptFile)) return;
            foreach (var line in File.ReadAllLines(bkptFile))
                if (int.TryParse(line.Trim(), out int lineNum) && lineNum > 0)
                    Breakpoints.Add(lineNum);
        }
        catch { }
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

        List<CompilerError> codeErrors = null!;
        var instructions = Compiler.CompilaCodice(codeLines, ref codeErrors);

        List<CompilerError> dataErrors = null!;
        var memory = Compiler.CompilaDati(dataLines, ref dataErrors);

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
            if (_factory.Registers is { } rv) rv.Dump = "";
            if (_factory.Memory is { } mv) mv.Dump = "";
            if (_factory.Stack is { } sv) sv.Dump = "";
            Cpu.Stop();
            CurrentSourceLine = -1;
            NotifyCpuStatusChanged();
            return false;
        }

        StatusMessage = "Compilazione completata";
        _atBreakpoint = false;
        _pendingFirstStep = true;
        Cpu.Init(instructions, memory, Ambiente.InizializzaRegistri, Ambiente.LoopInfinito);
        SyncBreakpointsToCpu();
        RefreshDebugViews();
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
            catch (CpuException) { UpdateCurrentSourceLine(); RefreshDebugViews(); return; }
            if (Cpu.stop)
            {
                UpdateCurrentSourceLine();
                RefreshDebugViews();
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
            catch (CpuException) { break; }
        }

        UpdateCurrentSourceLine();
        RefreshDebugViews();
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
            catch (CpuException) { UpdateCurrentSourceLine(); RefreshDebugViews(); return; }
            if (Cpu.stop)
            {
                UpdateCurrentSourceLine();
                RefreshDebugViews();
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
            catch (CpuException) { break; }
        }

        if (tempBreakpoint)
            Cpu.Breakpoints.Remove(instrIdx);

        UpdateCurrentSourceLine();
        RefreshDebugViews();
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

        _pendingFirstStep = false;
        UpdateCurrentSourceLine();
        RefreshDebugViews();
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
        catch (CpuException) { }
        UpdateCurrentSourceLine();
        RefreshDebugViews();
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
        catch (CpuException) { }
        UpdateCurrentSourceLine();
        RefreshDebugViews();
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
        catch (CpuException) { }
        UpdateCurrentSourceLine();
        RefreshDebugViews();
        StatusMessage = CurrentSourceLine > 0 ? $"Riga {CurrentSourceLine}" : "Esecuzione terminata";
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

    [RelayCommand] private void SetThemeLight() => Settings.Theme = AppTheme.Light;
    [RelayCommand] private void SetThemeDark()  => Settings.Theme = AppTheme.Dark;

    // ── Debug views ───────────────────────────────────────────────────────────

    private void RefreshDebugViews()
    {
        var regs = Cpu.DumpRegs();
        if (_factory.Registers is { } rv)
            rv.Dump = string.Join("\n", regs) +
                      $"\nZ={(Cpu.FlagZero ? 1 : 0)}  S={(Cpu.FlagSegno ? 1 : 0)}  O={(Cpu.FlagOverflow ? 1 : 0)}";

        var mem = Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, 8);
        if (_factory.Memory is { } mv)
            mv.Dump = mem is null ? "" : string.Join("\n", mem);

        var stack = Cpu.DumpMemoria(Ram.INDIRIZZO_STACK, Ram.MASSIMO_INDIRIZZO + 1, Ambiente.ColonneStack);
        if (_factory.Stack is { } sv)
            sv.Dump = stack is null ? "" : string.Join("\n", stack);
    }

    public void NavigateToError(CompilerError err)
    {
        int lineNumber = err.Riga + 1;
        if (err.Tipo == CompilerError.CODICE)
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
