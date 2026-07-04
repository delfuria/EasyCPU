# EasyCPU — Runbook per Claude Code

> Documento operativo **per l'agente** (Claude Code). Lo sviluppatore avvia
> **una fase per volta**; tu esegui solo la fase richiesta, ti fermi al gate di
> verifica e riporti. Non procedere alla fase successiva senza istruzione.
>
> Documenti di riferimento (leggili sempre prima di iniziare):
> - `MigrationPlan/GUIDA-SVILUPPO.md` — runbook umano con fasi, verifiche, problemi.
> - `MigrationPlan/README.reviewed.md` — analisi corretta, pseudo-codice debug.
> - `CLAUDE.md` (root repo) — regole di comportamento da rispettare SEMPRE.

---

## 1. Regole di ingaggio (override su tutto il resto)

Dal `CLAUDE.md` del repo, vincolanti:

1. **Pensa prima di scrivere**: dichiara le assunzioni; se ci sono più interpretazioni, fermati e chiedi invece di scegliere in silenzio.
2. **Semplicità**: il minimo codice che risolve il problema. Niente astrazioni speculative, niente feature non richieste.
3. **Modifiche chirurgiche**: tocca solo ciò che serve alla fase corrente. Non "migliorare" codice adiacente, non riformattare, non rinominare a caso. Ogni riga modificata deve tracciare alla richiesta.
4. **Esecuzione guidata da obiettivi**: trasforma il compito in criteri verificabili (test), poi itera finché passano.

Regole aggiuntive specifiche di questo progetto:

5. **Una fase alla volta.** Non anticipare lavoro di fasi successive. Se noti qualcosa che riguarda un'altra fase, **annotalo** nel report, non implementarlo.
6. **Fermati al gate.** Quando la sezione "Verifica" della fase è verde, **non continuare**: riassumi cosa hai fatto, l'output dei comandi di verifica, e attendi il via per la fase successiva.
7. **Niente git** (commit/add/push/reset/checkout), niente release, niente comandi distruttivi senza richiesta esplicita: ai commit pensa lo sviluppatore (vedi §6).
8. **Se un gate fallisce e non riesci a risolverlo in modo pulito**, fermati e riporta il problema con ipotesi e opzioni, invece di accumulare workaround.
9. **Non disabilitare warning o test** per far passare la build. `NU1605` è errore: va risolto allineando le versioni, non sopprimendolo.

---

## 2. Mappa del progetto

Radice soluzione: `VS Solution/` (contiene `EasyCPU.slnx`).

```
VS Solution/
├── EasyCpu.Common/        Ambiente (opzioni), Errori, Global, enum FormatoValore
├── EasyCpu.Assembler/     Cpu, Compiler, Parser, Ram, Instruction, Exceptions   ← core
├── EasyCpu.Backend/       Storage (I/O file)
├── EasyCPU/         UI condivisa Avalonia (oggi = demo AvaloniaEdit da svuotare)
├── EasyCPU.Desktop/ entry desktop
├── EasyCPU.Browser/ entry WASM
├── EasyCPU.Android/  entry Android
├── EasyCPU.iOS/      entry iOS
└── (da rimuovere in Fase 0) AvaloniaEdit/, AvaloniaEdit.TextMate/, EasyCpu.Core/,
                              EasyCpu.Win/, EasyCpu.Win.Controls/
```

`EasyCpu.Win/` resta in git come **riferimento di comportamento** anche dopo la rimozione dalla `.sln`: consultalo per replicare le funzioni, non modificarlo.

---

## 3. Verità tecniche da non sbagliare

Queste sono già verificate sul sorgente. Non re-inferirle: usale.

- **TFM**: tutti `net10.0` (head con suffisso piattaforma). SDK pin: `global.json` → 10.0.101.
- **Versioni target**: Avalonia `12.0.5`, AvaloniaEdit `12.0.0` (pacchetto `Avalonia.AvaloniaEdit`), TextMate `AvaloniaEdit.TextMate 12.0.0`, Dock `12.0.0.2` (con `Dock.Model.Mvvm`), `CommunityToolkit.Mvvm 8.4.0`. **Non si usa ReactiveUI.** Allinea TUTTO a 12.0.5 per evitare NU1605.
- **Stack discendente**: SP iniziale = 256 (`Ram.MASSIMO_INDIRIZZO+1`); `push` → `sp--`, `pop` → `sp++`; `call` push, `ret` pop.
- **Fetch/Execute**: `ip++` avviene a fine `Execute()`. Dopo uno step, `curIstruzione` è l'istruzione **già eseguita**; quella da eseguire è `code[ip]`.
- **Righe**: il compilatore usa indici **0-based** (`indRiga`); AvaloniaEdit usa righe **1-based**. Converti sempre, una sola volta, in un punto noto.
- **Memoria**: 0–255; stack 240–255 (`INDIRIZZO_STACK=240`).
- **Set istruzioni autoritativo**: `Parser.SetCode` (**36 opcode** — vedi Appendice A di `GUIDA-SVILUPPO.md`; il piano originale diceva 35 per errore). Non fidarti di elenchi a memoria.
- **Breakpoint**: nuovo `HashSet<int> Breakpoints` su indici istruzione; il vecchio `Trap`/`SetTrap`/`rigaTrap`/`Instruction.Trap` va rimosso. `CpuTrapException`/`CpuLoopException` restano.
- **Cross-platform**: `Ambiente.cs` ha path con `\\` hardcoded e `using System.Drawing` residuo; `Storage.cs` idem (`System.Drawing`). Vanno corretti (Fase 1).
- **`Ambiente`**: campi `static` pubblici senza notifiche, **e il core li legge staticamente**. Si gestisce in MVVM con un `SettingsViewModel` singleton osservabile con write-through su `Ambiente` (Fase 6); `Ambiente` resta come store di persistenza. Non rendere `Ambiente` non-statico in questa fase.
- **Formato file (deciso)**: il **default di salvataggio è un nuovo formato** (`EasyFileSerializer`, JSON, estensione `.asj`); i vecchi `.as` testuali restano **apribili** (`LegacyAsSerializer`, sola lettura) con autodetect. Non scrivere più nel formato legacy.

---

## 4. Protocollo di verifica (eseguilo a ogni fase)

Comandi (esegui dalla cartella `VS Solution/`):

```bash
dotnet --version                                   # atteso: 10.0.101.x
dotnet restore EasyCPU.slnx
dotnet build EasyCPU.slnx -c Debug            # 0 errori, 0 NU1605
dotnet test                                        # dalla Fase 1 in poi
dotnet run --project EasyCPU.Desktop         # verifica avvio app (manuale)
```

Se mancano i workload mobile/wasm e bloccano la `.sln`, lavora con una solution
filter sui soli progetti rilevanti (Desktop + UI + business logic) e segnalalo.

Per ogni fase, nel report finale includi: comando eseguito + esito sintetico
(es. "build: 0 errori"; "test: 7 passed"). Niente claim non verificati.

---

## 5. Ordini di lavoro per fase

Per ciascuna fase: **fai solo questa**, rispetta la "Definition of Done", fermati al gate.
I dettagli completi (passi puntuali, problemi noti) sono in `GUIDA-SVILUPPO.md`: leggi la fase corrispondente prima di iniziare.

### Fase 0 — Pulizia, versioni, setup
- **Tocca**: `global.json` (nuovo), `*.csproj` di tutti gli head, `Directory.Build.props`, `App.xaml`, `EasyCPU` (svuota demo), `.sln`.
- **Non toccare**: il core (`Assembler`/`Common`/`Backend`) — solo in Fase 1.
- **DoD**: progetti morti/legacy rimossi; tutto su Avalonia 12.0.5; demo AvaloniaEdit svuotato; soluzione compila.
- **Gate**: `dotnet build` 0 errori, 0 NU1605; `dotnet run` Desktop apre una finestra-scheletro.
- **Attenzione**: `App.xaml` ha uno `StyleInclude` `avares://AvaloniaEdit/...` — verifica che risolva ancora col NuGet; `.Desktop.csproj` ha un `ProjectReference` duplicato da rimuovere.

### Fase 1 — Core a istanze + breakpoint + fix cross-platform ✅ COMPLETATA (2026-06-28)
- **Tocca**: `EasyCpu.Assembler/*` (Cpu, Compiler, Parser, Instruction), `EasyCpu.Backend/Local/Storage.cs`, `EasyCpu.Common/Ambiente.cs`; nuovo progetto `EasyCpu.Assembler.Tests`.
- **Non toccare**: la UI.
- **DoD**: core istanziabile; `HashSet<int> Breakpoints`; `StepOver/StepOut/RunWhileInside` corretti (stack discendente, guarda `code[ip]`); `InstrToLineMap`+`LineToInstrMap`; rimossi `Trap/SetTrap/rigaTrap`; path con `Path.Combine`; rimossi `using System.Drawing`.
- **Gate**: `dotnet test` verde con i test elencati in `GUIDA-SVILUPPO.md` Fase 1 (Step Into/Over/Out, breakpoint multipli, mapping righe, isolamento istanze).
- **Attenzione**: l'errore classico è invertire il verso dello stack — usa il test `step_over_call` come prova del nove.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 1 → "Assunzioni e decisioni"):
  1. Dopo `CpuTrapException`, la UI deve chiamare `StepInto()` prima di `Run()` (non `Run()` direttamente).
  2. `RunWhileInside(limite)` usa `sp < limite`; `StepOut` passa `sp+1`.
  3. Marker commento `//` confermato in `PreparaRiga`; `'` serve solo per costanti char.
  4. Indici core 0-based; conversione a 1-based (AvaloniaEdit) va fatta solo nella UI in Fase 3.

### Fase 2 — Dock + ViewModel ✅ COMPLETATA (2026-06-28)
- **Tocca**: `EasyCPU/` (DockFactory, ViewModel dei pannelli, MainViewModel, registrazione DataTemplate, serializer layout).
- **DoD**: shell con pannelli dockabili (anche vuoti) basata su `Dock.Model.Mvvm`; il serializer layout non lancia.
- **Gate**: app avvia, pannelli agganciabili/spostabili.
- **Attenzione**: non aggiungere `Dock.Model.ReactiveUI` o `ReactiveUI.Avalonia`; includi `Dock.Avalonia.Themes.Fluent`; classi VM `partial` per i source generator CommunityToolkit.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 2 → "Assunzioni e decisioni"):
  1. `IRootDock` è in `Dock.Model.Controls`, non `Dock.Model.Core` (che contiene solo `IDockable`/`IFactory`). Aggiungere `using Dock.Model.Controls;` in tutti i file che usano `IRootDock`.
  2. Il tema Dock si include via classe XAML: `<dockTheme:DockFluentTheme />` (`Dock.Avalonia.Themes.Fluent.DockFluentTheme`), non via `StyleInclude`.
  3. `DockControl` in `Dock.Avalonia.Controls`, assembly `Dock.Avalonia`. Richiede sia `Layout` che `Factory` bindati (senza `Factory`, il drag/drop non funziona).
  4. `Factory.CreateLayout()` è virtuale — override obbligatorio. Le factory methods (`CreateRootDock`, `CreateProportionalDock`, ecc.) vanno richiamate sull'istanza per creare i tipi Mvvm corretti.
  5. `SettingsViewModel`: singleton con costruttore privato, inizializzato da `Ambiente.*` dopo `Ambiente.Inizializza()` in `App.OnFrameworkInitializationCompleted`. Il write-through verso `Ambiente` (callback `partial void OnXxxChanged`) è rimandato a Fase 6.
  6. `MainWindowViewModel.cs` è stato riusato (classe rinominata a `MainViewModel`); il file non è stato spostato.
  7. Aggiunta `#nullable enable` nei file che usano annotazioni nullable (`?`) per evitare CS8632.

### Fase 2b — Menu IDE + Toolbar + Temi ✅ COMPLETATA (2026-06-28)
- **Tocca**: `EasyCPU/App.xaml`, `App.xaml.cs`, `Views/MainWindow.xaml`, `Views/MainView.axaml`, `ViewModels/MainWindowViewModel.cs` (rinominato `MainViewModel`), `ViewModels/SettingsViewModel.cs`, `DockFactory.cs`; nuovo file `AppTheme.cs`; `EasyCPU.csproj` (fix item type).
- **DoD**: menu in-window + `NativeMenu` desktop; touch toolbar mobile/browser; temi Light/Dark/Blue funzionanti senza riavvio; voci Finestre checkable via `IDockable.DockingState`; tutti i nuovi comandi come stub non crashano.
- **Gate**: build 0 errori, 6 test pass, app avviata senza crash.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 2b → "Assunzioni e decisioni"):
  1. `NativeMenuItem` non supporta compiled bindings per `ToggleType`/`IsChecked` (AVLN3000). Il `NativeMenu` ha solo Header+Command+Gesture; i checkmark sono solo sull'`<Menu>` in-window.
  2. Blue theme via `FluentTheme.Palettes[ThemeVariant.Dark] = new ColorPaletteResources { Accent = Color.Parse("#007ACC") }`. L'approccio precedente con `Application.Resources["SystemAccentColor"]` non differenziava Blue da Dark. `ThemeDictionaries` XAML con `{x:Static}` come key causa crash silenzioso del precompiler. `using System.Linq;` richiesto per `Styles.OfType<FluentTheme>()` (altrimenti risolve verso `Avalonia.Styling.Selectors.OfType`).
  3. Dock 12: pannelli chiusi dalla X del tab NON impostano `DockingState = Hidden` — vengono rimossi dall'albero. `RestoreDockable` funziona solo se il pannello è stato nascosto via `HideDockable`. Il menu Finestre usa comandi `ShowXxx` (non toggle con checkmark); per pannelli chiusi dalla X l'unico ripristino è `ResetLayout`.
  4. Avalonia 12 non ha il controllo `ToolBar`. Sostituito con `<Border>` + `<StackPanel Orientation="Horizontal">`.
  5. `App.xaml` (estensione `.xaml`) va incluso come `<AvaloniaXaml>` nel csproj, non `<AvaloniaResource>`: altrimenti il precompiler non genera il bytecode e `AvaloniaXamlLoader.Load()` lancia `XamlLoadException` a runtime.
  6. Su macOS il `<Menu>` in-window viene nascosto a runtime con `FindControl<Menu>("MainMenu").IsVisible = false` (richiede `using System;` per `OperatingSystem.IsMacOS()`). Il `NativeMenu` copre già tutto nella barra di sistema.

### Fase 3 — Editor con debug ✅ COMPLETATA (2026-06-29)
- **Tocca**: `EasyCPU/` (BreakpointMargin, DebugCurrentLineRenderer, CodeEditorView, DataEditorView, DataEditorViewModel, MainWindowViewModel).
- **DoD**: margine breakpoint cliccabile collegato a `MainViewModel.Breakpoints`; evidenziazione riga corrente; Tab/Enter rispettano `MargineSinistro`; testo pannelli persistente su hide/show; Data Editor con editor AvaloniaEdit reale; `Compile()` legge codice e dati da pannelli separati.
- **Gate**: toggle breakpoint funziona cliccando sul margine sinistro; riga corrente si muove a ogni step; build 0 errori.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 3 → "Assunzioni e decisioni"):
  1. `AbstractMargin` non ha `Background`: le aree senza contenuto disegnato non superano l'hit test. Fix: fill quasi-trasparente (`Color.FromArgb(1,0,0,0)`) all'inizio di `Render()`.
  2. Click handling solo in `BreakpointMargin.OnPointerPressed` — nessun handler sul `LineNumberMargin`.
  3. `TextView.VisualLinesChanged` obbligatorio per ridisegnare i pallini dopo rebuild.
  4. Calcolo riga via aritmetica diretta (`(int)(docY / lineHeight) + 1`), non `GetVisualLineFromVisualTop` (può restituire null).
  5. Clipboard Avalonia 12: `DataTransferItem.CreateText` + `DataTransfer` + `SetDataAsync`; lettura: `TryGetDataAsync` + `AsyncDataTransferExtensions.TryGetTextAsync`.
  6. `SearchPanel.Install()` prende `TextEditor`, non `TextArea`.
  7. Dock.Avalonia ricrea la View su hide/show: testo va salvato in `SourceText` del ViewModel e ripristinato in `OnDataContextChanged`.
  8. Pannello dati: `Compile()` legge `_factory.DataEditor.SourceText` direttamente, non più via split `.DATA` sul codice.

### Fase 4 — Syntax highlighting ✅ COMPLETATA (2026-06-29)
- **Tocca**: nuovo `.xshd` (EmbeddedResource) + caricamento nell'editor.
- **DoD**: tutti e 36 gli opcode di `Parser.SetCode` (la guida diceva 35, ma il sorgente ne ha 36), registri, indiretti `[bx]/[bp]/[si]/[di]`, commenti `//`, char `'…'`, etichette `:`, hex con suffisso `h`, marcatore `.DATA`.
- **Gate**: build 0 errori; tutti gli opcode colorati.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 4 → "Assunzioni e decisioni"):
  1. `Parser.SetCode` contiene **36** opcode, non 35 come indicato nella guida (aggiornare Appendice A).
  2. L'`.xshd` è registrato in `App.OnFrameworkInitializationCompleted()` via `HighlightingManager.Instance.RegisterHighlighting` e recuperato nelle view con `GetDefinition("EasyCPU")`.
  3. La registrazione usa `new XmlTextReader(stream)`, non `XmlReader.Create()`, per compatibilità con AvaloniaEdit 12.
  4. Il nome risorsa embedded è `EasyCPU.Resources.EasyCPU.xshd` (RootNamespace `EasyCPU` + cartella `Resources`).
  5. Lo stesso `.xshd` è applicato anche a `DataEditorView` (colora numeri, hex ed etichette nella sezione dati).
  6. Colori pensati per tema Light (default); su tema Dark rimangono leggibili ma non ottimali — miglioramento estetico rimandato a scelta futura.
  7. Colore `OpcodeArith` aggiornato a `#5D2906` (marrone scuro) su richiesta; il valore originale era `#E65100` (arancio). Modifica solo in `EasyCPU.xshd`.

### Fase 5 — Pannelli (contenuto) ✅ COMPLETATA (2026-06-30)
- **Tocca**: `RegistersView/MemoryView/StackView/ErrorsView` (XAML + code-behind) + `RegistersViewModel/MemoryViewModel/StackViewModel/ErrorsViewModel` + `MainViewModel` + `CodeEditorViewModel` + `DataEditorViewModel` + `CodeEditorView.axaml.cs` + `DataEditorView.axaml.cs` + nuovo `Views/Converters.cs`.
- **DoD**: dump reattivi a ogni step; riusa `Cpu.DumpRegs()`/`DumpMemoria()`; errori di compilazione in `ErrorsView`; doppio click errore → riga nell'editor.
- **Gate**: build 0 errori, 6 test pass.
- **Assunzioni registrate** (dettagli in `GUIDA-SVILUPPO.md` §Fase 5 → "Assunzioni e decisioni"):
  1. `Avalonia.Controls.DataGrid` non è nel package `Avalonia 12.0.5`. Installato `ProDataGrid 12.0.4` (distribuisce `Avalonia.Controls.DataGrid.dll` — stessa API standard). `ErrorsView` usa `DataGrid` con `CanUserResizeColumns="True"` e colonne esplicite. Tema incluso in `App.xaml` via `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml"/>` (non `Fluent.xaml` che non ha build precompilato). Gli errori sono ordinati per `RigaDisplay` ascendente (secondario: `TipoDisplay`) prima di aggiungerli alla collection.
  2. `CompilerError` ha campi pubblici (non proprietà) — i compiled binding Avalonia non li supportano. Soluzione: `CompilerErrorAdapter` (UI-layer wrapper con proprietà `TipoDisplay`, `RigaDisplay` 1-based, `Msg`, `Source`). `ErrorsViewModel.Errors` è `ObservableCollection<CompilerErrorAdapter>`. Nessun `x:CompileBindings="False"`.
  3. `Cpu.DumpMemoria` ritorna `List<string>` non-nullable per firma, ma può ritornare `null` a runtime se la CPU non è inizializzata — guard `mem is null` presente in `RefreshDebugViews()`.
  4. `RefreshDebugViews()` viene chiamata da: `StepInto`, `StepOver`, `StepOut`, `Run`, `Stop`, `Compile` (sia su successo che su errore, con clear esplicito in caso di errore).
  5. `ErrorsViewModel` ora ha un costruttore con `MainViewModel mainVm` (come `CodeEditorViewModel`). `DockFactory.CreateLayout()` aggiornato di conseguenza.
  6. `NavigateToError(CompilerError)` su `MainViewModel`: chiama `_factory.SetActiveDockable(editor)` per portare in primo piano il tab, poi invoca `NavigateToLineAction(lineNumber)`. Il caret viene posizionato sul primo carattere non-spazio della riga (via `TrimStart`); il focus all'editor è differito con `Dispatcher.UIThread.Post(() => _editor.TextArea.Focus())` perché il tab switch del Dock è asincrono. Se il pannello è nascosto (view non creata), l'azione è `null` e la navigazione è ignorata silenziosamente.
  7. Double-click su riga in `ErrorsView` — `DoubleTapped="handler"` in XAML non funziona con `DataGrid`: il DataGrid marca `PointerPressed` come handled internamente. Fix: `AddHandler(InputElement.PointerPressedEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true)` registrato in code-behind nel costruttore di `ErrorsView`. Il `PointerPressedEventArgs.ClickCount == 2` distingue il double-click.
  8. **Barra di stato**: `[ObservableProperty] private string _statusMessage` in `MainViewModel`. UI: `Border DockPanel.Dock="Bottom"` con `TextBlock` in **entrambi** `MainWindow.xaml` (desktop) e `MainView.axaml` (single-view), **prima** del `DockControl` (ordine obbligatorio in `DockPanel`). Messaggi in: `DoCompile`, `Run`, `StepInto/Over/Out`, `Stop`.
  9. **"Avvia" riprende da breakpoint**: se `_atBreakpoint`, il comando `Run` chiama `Cpu.StepInto()` (avanza senza controllare breakpoint) poi `Cpu.Run()` (se non ancora `stop`). Evita il ri-trap immediato sullo stesso IP. Se la CPU termina durante lo step si aggiornano le viste senza chiamare `Run()`.

### Fase 6 — Dialog, opzioni (MVVM), formato file ✅ COMPLETATA (2026-06-30)
- **Tocca**: `SettingsViewModel` (singleton) + `OpzioniWindow`, `SospendiWindow`, `ISourceSerializer`/`EasyFileSerializer`/`LegacyAsSerializer` in `EasyCpu.Backend/Serializers/`; `ModoSospendi` enum; `MainWindowViewModel` (New/Open/Save/SaveAs/Run/ShowOptions); `Ambiente.FontPanelliSize`; pannelli di output per font reattivo.
- **DoD**:
  - `SettingsViewModel` con write-through completo su `Ambiente.*` via `partial void OnXxxChanged`; `OpzioniWindow` edita copia (`OpzioniViewModel`) con OK/Annulla; su OK → `ApplyTo(Settings)` + `Storage.SalvaOpzioni()` + `RefreshDebugViews()`.
  - Salvataggio sempre nel **nuovo formato** (`.asj`, JSON); apertura con autodetect di `.asj` (nuovo) e `.as` (legacy, sola lettura); aprendo un `.as` e salvando propone il nuovo formato.
  - Apri/Salva con `IStorageProvider`; loop infinito → `SospendiWindow`; `Run()` async con switch su `ModoSospendi`.
- **Gate**: cambio `FormatoDati` OK/Annulla ok e persistito; salva `.asj` + apri `.as` legacy ok; round-trip senza perdite; font editor/pannelli aggiornati live. VERDE ✅
- **Assunzioni registrate**:
  1. `GetOwnerWindow()` via `IClassicDesktopStyleApplicationLifetime.MainWindow` — `NativeMenuItem` non supporta `CommandParameter`, quindi i comandi file/opzioni non possono ricevere la Window come parametro. Soluzione: i comandi leggono la MainWindow direttamente dal lifecycle. Funzionalmente equivalente.
  2. `x:CompileBindings="False"` su `OpzioniWindow` — `NumericUpDown.Value` è `decimal?` mentre le proprietà sono `int`/`float`; i compiled binding Avalonia non supportano la coercizione numerica. Il binding riflessivo la gestisce automaticamente.
  3. `ModoSospendi.Arresta = 0` (primo nell'enum) — così `default(ModoSospendi)` corrisponde ad Arresta quando la finestra viene chiusa tramite il pulsante X senza selezionare un'opzione esplicita.
  4. `SetSourceTextAction` callback in `CodeEditorViewModel`/`DataEditorViewModel` — `AvaloniaEdit.TextEditor.Document.Text` non è bindabile da ViewModel; la view espone un `Action<string>?` wired in `SetupEditor()` per permettere a `New()`/`Open()` di aggiornare il testo dell'editor da codice. Aggiornare anche `vm.SourceText` direttamente come fallback quando il pannello non è visibile.
  5. `SuggestedFileName` senza estensione — il `FilePickerSaveOptions.SuggestedFileName` non deve contenere l'estensione (es. `"file1"`, non `"file1.asj"`): `DefaultExtension = ".asj"` la aggiunge automaticamente. Passare il nome con estensione causa la doppia estensione `file.asj.asj`.
  6. `Storage.LeggiOpzioni()` prima di `SettingsViewModel.Instance` — l'ordine in `App.OnFrameworkInitializationCompleted()` è critico: `Ambiente.Inizializza()` → `Storage.LeggiOpzioni()` → primo accesso a `SettingsViewModel.Instance` (che legge da `Ambiente.*` nel costruttore).
  7. Font pannelli (`FontPanelliSize`): nuovo campo `Ambiente.FontPanelliSize` (float, default 12) + `CHIAVE_FONTPANNELLISIZE` in `Storage.SalvaOpzioni()`/`LeggiOpzioni()`. Pannelli `RegistersView`/`StackView`/`MemoryView` (su `TextBlock "DumpText"`) ed `ErrorsView` (su `DataGrid "ErrorsGrid"`) si sottoscrivono a `SettingsViewModel.PropertyChanged` per applicare il font live.

### Fase 7 — Persistenza ✅ COMPLETATA (2026-06-30)
- **Tocca**: `ViewModels/LayoutState.cs` (nuovo), `DockFactory.cs` (`ToDockNode`/`RebuildLayout`/`ContainerFor`/`CurrentLayout`), `ViewModels/MainWindowViewModel.cs` (`SaveLayout`/`LoadLayout`/`SaveBreakpoints`/`LoadBreakpoints`/`SaveAll`/`OpenFileFromPath`/`RecentFileItems`), `ViewModels/RecentFileItem.cs` (nuovo), `Views/MainWindow.xaml` (voce Recenti), `Views/MainWindow.xaml.cs` (`Closing`/`SetupNativeRecentiMenu`/`UpdateNativeRecentiMenu`), `App.xaml.cs` (bootstrap persistenza).
- **DoD**: ✅ layout Dock persistente (albero intero, non solo proporzioni scalari); ✅ file recenti sincronizzati in menu in-window e NativeMenu macOS; ✅ breakpoint persistenti su sidecar `.bkpt`; ✅ caricamento tollerante (file assenti, JSON incompatibile → fallback al layout default); ✅ numeri di riga persistiti (non indici istruzione); ✅ pannello Errori non duplicato dopo spostamento + compilazione.
- **Gate**: chiudi/riapri → layout, breakpoint, file recenti ripristinati. Build 0 errori. VERDE ✅
- **Assunzioni registrate**:
  1. **Serializzazione layout: walker ricorsivo custom** (`DockNode` polimorfico con `[JsonPolymorphic]`/`[JsonDerivedType]`) invece di `Dock.Serializer.SystemTextJson` — quest'ultimo richiede source-gen e costruttori senza parametri sui ViewModel; il walker custom serializza solo struttura e ID, ricostruisce reinserendo le istanze reali per ID.
  2. **`JsonNumberHandling.AllowNamedFloatingPointLiterals` obbligatorio** — Dock usa `double.PositiveInfinity` per le proporzioni di `RootDock`; `JsonSerializer` di default lancia `ArgumentException` su valori Infinity. Opzione richiesta sia in `Serialize` che in `Deserialize`.
  3. **Salvataggio su `Window.Closing`, non solo su `desktop.Exit`** — su macOS, premere il pulsante rosso chiude la finestra senza attivare `desktop.Exit` (l'app rimane nel dock). `Window.Closing` è il punto di salvataggio primario; `desktop.Exit` è mantenuto come backup per Cmd+Q / `ExitCommand`.
  4. **`ContainerFor` percorre l'intero albero** (`FindContainerInTree` ricorsivo su `CurrentLayout`) — quando l'utente trascina un pannello in una nuova posizione, Dock crea nuovi nodi container non tracciati da `DocumentContainer`/`ToolContainer`. Cercare solo nei due container fissi causava "pannello non visibile" → duplicazione al prossimo `AddDockable`. La ricerca ricorsiva su `CurrentLayout` risolve entrambi i bug (posizione non salvata + pannello duplicato).
  5. **`DockFactory.CurrentLayout`** (property interna) — aggiornata da `MainViewModel` dopo ogni assegnazione di `Layout` (costruttore + `LoadLayout`), in modo che `ContainerFor` abbia sempre l'albero corrente.
  6. **Sidecar breakpoint `.bkpt`** (non inline nel JSON) — un numero di riga 1-based per riga; caricamento tollerante a file assenti o righe non numeriche. Scelto per compatibilità con il formato legacy `.as` (sola lettura) che non può contenere breakpoint inline.
  7. **File recenti nel NativeMenu macOS: inserimento dinamico in codice** — `NativeMenuItem` non supporta `ItemsSource`; il submenu "Recenti" viene creato programmaticamente in `MainWindow.xaml.cs` con `SetupNativeRecentiMenu()` + `UpdateNativeRecentiMenu()`. Lambda catturano `path` per evitare il problema del `CommandParameter` assente su `NativeMenuItem`.
  8. **`ReflectionBinding` per `ItemContainerTheme` del menu Recenti** — con `AvaloniaUseCompiledBindingsByDefault=true`, i binding in `ControlTheme`/`ItemContainerTheme` inline richiedono `x:DataType`; poiché `RecentFileItem` è una classe custom non dichiarata come DataType in un tema inline, si usa `{ReflectionBinding Header}` / `{ReflectionBinding Command}` che funzionano correttamente a runtime.

---

## 6. Commit — NON automatici

- **Non fare commit in automatico.** Ai commit ci pensa lo sviluppatore, volta per volta.
- A fine fase lascia il working tree con le modifiche **non committate**; nel report indica i file toccati così che lo sviluppatore possa revisionarli e committare lui.
- Non fare `git add`/`git commit`/`git push`/`git reset`/`git checkout` né altri comandi git che alterano lo stato, salvo richiesta esplicita.
- Se serve un messaggio di commit, **proponilo nel report** (in italiano, imperativo, con prefisso fase, es. `fase1: Cpu istanziabile + SP pubblico`) ma non eseguirlo.

---

## 7. Quando fermarti e chiedere

Interrompi e chiedi allo sviluppatore (non decidere da solo) se:

- Un'API Avalonia/Dock/AvaloniaEdit 12 differisce dagli esempi e ci sono più modi ragionevoli di adattarla.
- Una scelta cambia lo **schema di persistenza** oltre il formato già deciso (nuovo `.asj` di default + lettura legacy `.as`), o tocca la compatibilità dei dati utente in modo non previsto.
- Per far passare un gate servirebbe modificare codice fuori dallo scope della fase.
- Emerge un comportamento del core che diverge da `EasyCpu.Win` e non è chiaro quale sia quello voluto (es. il possibile bug etichette `[HEX]/[DEC]` in `DumpReg`).
- Servono workload/SDK non presenti, o un head non compila per ragioni d'ambiente.

In tutti gli altri casi procedi con l'opzione più semplice e conforme a `CLAUDE.md`, dichiarando l'assunzione nel report.

---

## 8. Formato del report di fine fase

Alla fine di ogni fase, produci:

1. **Cosa è stato fatto** (elenco puntuale, mappato ai DoD).
2. **File toccati** (path).
3. **Verifica**: comandi eseguiti + esito (build/test/run).
4. **Assunzioni fatte** e cose annotate per fasi successive.
5. **Stato del gate**: VERDE (pronto per la fase successiva) o BLOCCATO (con motivo e opzioni).

Poi **fermati** e attendi il via per la fase successiva.
