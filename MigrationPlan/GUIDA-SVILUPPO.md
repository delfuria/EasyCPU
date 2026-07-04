# EasyCPU — Guida allo sviluppo

> Runbook operativo per la migrazione da WinForms ad Avalonia 12. Ogni fase
> ha: **obiettivo**, **passi**, **verifica** (come capire che è andata bene) e
> **problemi probabili** con mitigazione. Da leggere insieme a
> `README.reviewed.md` (analisi e correzioni) e `README.md` (piano originale).
>
> Tutti i riferimenti a file sono relativi a `VS Solution/`. Tutti i fatti sono
> stati verificati sul sorgente al 27/06/2026.

---

## Come usare questa guida

- Esegui le fasi **in ordine**: ognuna è un prerequisito della successiva.
- Non passare alla fase successiva se la sezione **Verifica** non è verde.
- Lavora a piccoli commit: un commit per checkbox quando possibile.
- Tieni `EasyCpu.Win` come **riferimento di comportamento** (anche se rimosso dalla soluzione, il codice resta nella cartella e in git) finché la IDE Avalonia non replica le stesse funzioni.

### Criteri trasversali di "fatto" (Definition of Done)

| Criterio | Comando / controllo |
|---|---|
| La soluzione compila | `dotnet build EasyCPU.slnx -c Debug` → 0 errori |
| Nessun warning di downgrade pacchetti | nessun `NU1605` (è impostato `warnAsError` su NU1605) |
| I test del core passano | `dotnet test` (dopo aver creato il progetto di test, Fase 1) |
| L'app desktop parte | `dotnet run --project EasyCPU.Desktop` apre la finestra |

---

## Prerequisiti d'ambiente

- **.NET SDK 10.0.101** installato (verrà bloccato da `global.json`). Verifica: `dotnet --version`.
- Workload per gli head mobili/web (necessari solo quando si compilano quegli head):
  - `dotnet workload install android ios wasm-tools`
- Per Android: SDK/JDK configurati (nel repo ci sono già i log `sdkmanager`).
- Per iOS: build solo su macOS con Xcode.
- Editor: Rider o VS 2022/VS Code con estensione Avalonia.

> Nota: negli `obj/` ci sono artefatti sia `net9.0` sia `net10.0`. Dopo aver
> aggiunto `global.json`, esegui una pulizia completa (vedi Fase 0) per evitare
> che restino in giro binari compilati con SDK diversi.

---

## Quadro di partenza (stato reale verificato)

- Tutti gli head Avalonia sono su **11.3.9**; `Directory.Build.props` → `AvaloniaVersion 11.3.8`. Target: **12.0.5**.
- `EasyCPU` **è il progetto demo di AvaloniaEdit** (decine di `Resources/SampleFiles/*`, `ThemeViewModel`, `MainWindowViewModel`/`MainEditorView`/`MainView` demo). Va svuotato.
- Il core (`Cpu`, `Compiler`, `Parser`) è **interamente statico**.
- Esiste già un meccanismo trap a 1 breakpoint (`Instruction.Trap`, `Cpu.SetTrap`, `inTrap`, `rigaTrap`, `CpuTrapException`): verrà **sostituito** da `HashSet<int> Breakpoints`.
- `EasyCpu.Win`/`Win.Controls` usano il core staticamente: vengono **rimossi** dalla soluzione.
- Pacchetti target (tutti reali su nuget.org, giugno 2026): `Avalonia 12.0.5`, `Avalonia.AvaloniaEdit 12.0.0`, `AvaloniaEdit.TextMate 12.0.0`, `Dock.Avalonia` + sotto-pacchetti `12.0.0.2` (con `Dock.Model.Mvvm`), `CommunityToolkit.Mvvm 8.4.0`. **Non si usa ReactiveUI.**

---

## FASE 0 — Pulizia, versioni e setup

**Obiettivo:** soluzione snella, su Avalonia 12, che compila ancora (con `EasyCPU` ridotto a scheletro).

### Passi

1. Creare `global.json` nella root di `VS Solution/`:

   ```json
   { "sdk": { "version": "10.0.101", "rollForward": "latestPatch" } }
   ```

2. Rimuovere dalla soluzione (`.sln`) **e** dal disco i progetti morti/legacy:
   - `AvaloniaEdit/` e `AvaloniaEdit.TextMate/` (sorgenti → sostituiti dai NuGet)
   - `EasyCpu.Core/` (copia vecchia di `Common`, non referenziata da nessuno)
   - `EasyCpu.Win/` e `EasyCpu.Win.Controls/`
   - Eliminare anche le solution folder rimaste vuote: `Avalonia Edit`, `Frontend Legacy`, `Backend` (se vuota).

3. `Directory.Build.props`: `<AvaloniaVersion>12.0.5</AvaloniaVersion>`.

4. Allineare **tutti** i `.csproj` ad Avalonia 12.0.5 e CommunityToolkit.Mvvm 8.4.0:
   - `EasyCPU`: `Avalonia`, `Avalonia.Themes.Fluent` → 12.0.5; togliere i `ProjectReference` ad AvaloniaEdit; aggiungere `Avalonia.AvaloniaEdit 12.0.0`, `AvaloniaEdit.TextMate 12.0.0`, `CommunityToolkit.Mvvm 8.4.0`, e i pacchetti Dock 12.0.0.2 (`Dock.Avalonia`, `Dock.Model.Mvvm`, `Dock.Serializer.SystemTextJson`, `Dock.Avalonia.Themes.Fluent`).
   - `.Desktop`: `Avalonia`, `Avalonia.Desktop`, `Avalonia.Fonts.Inter` → 12.0.5; **rimuovere il `ProjectReference` duplicato** a `EasyCPU` (compare due volte).
   - `.Browser`: `Avalonia.Browser`, `Avalonia.Fonts.Inter` → 12.0.5.
   - `.Android`: `Avalonia.Android`, `Avalonia.Fonts.Inter` → 12.0.5.
   - `.iOS`: `Avalonia.iOS`, `Avalonia.Fonts.Inter` → 12.0.5.
   - Rimuovere `ReactiveUI.Avalonia` da tutti i `.csproj` (sostituito da CommunityToolkit.Mvvm; non serve nei head perché non è un requisito Avalonia).

5. In `App.xaml`, aggiornare lo `StyleInclude` del tema AvaloniaEdit: oggi punta al progetto sorgente `avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml`. Con il NuGet l'assembly resta `AvaloniaEdit`, quindi l'URI **dovrebbe** restare valido: verificarlo all'avvio (vedi Problemi).

6. Svuotare il demo da `EasyCPU`: rimuovere `Resources/SampleFiles/*` e il relativo blocco `EmbeddedResource`/`Compile Remove` nel `.csproj`, eliminare `ThemeViewModel`, e ridurre `MainWindowViewModel`/`MainView`/`MainEditorView` a un guscio vuoto (li ricostruiremo nelle fasi 2–5). Mantenere `App.xaml(.cs)`, `Program.cs`, la struttura lifetime desktop/single-view.

7. Pulizia build: `git clean -xdf` (o cancellare tutti i `bin/` `obj/`), poi `dotnet restore`.

### Verifica

- `dotnet build EasyCPU.slnx -c Debug` → 0 errori, **0 NU1605**.
- `dotnet run --project EasyCPU.Desktop` → finestra vuota/scheletro che si apre senza crash.
- `git status` mostra rimossi solo i progetti previsti; `.sln` non contiene più i GUID dei progetti eliminati.

### Problemi probabili

- **NU1605 (downgrade)**: un head è rimasto a 11.3.9 o un pacchetto Dock/AvaloniaEdit tira una versione diversa di Avalonia. Allineare tutto a 12.0.5; usare eventualmente `Directory.Packages.props` (Central Package Management) per centralizzare le versioni.
- **`avares://AvaloniaEdit/...` non risolto** dopo il passaggio al NuGet: se il tema non carica, l'editor appare senza stile o l'app lancia `XamlLoadException`. Verificare il nome reale della risorsa nel pacchetto (Rider → "Assembly Explorer", oppure provare `avares://Avalonia.AvaloniaEdit/...`).
- **Riferimenti rotti** dopo la rimozione dei progetti: cercare in tutta la soluzione `EasyCpu.Win`, `AvaloniaEdit\\` (ProjectReference), `EasyCpu.Core`.
- **Workload mancanti**: la `.sln` completa non compila se mancano android/ios/wasm. In sviluppo quotidiano si può lavorare su una solution filter (`.slnf`) con i soli `EasyCPU` + `.Desktop` + business logic.
- **Build misti net9/net10** residui: se compaiono errori strani, ripulire `bin/obj` (passo 7).

---

## FASE 1 — Refactoring BusinessLogic (da statico a istanze)

**Obiettivo:** `Cpu`, `Compiler`, `Parser` istanziabili, breakpoint multipli, core testabile e cross-platform.

### Passi

1. **`Parser`** (`Parsing/Parser.cs`): rimuovere `static` da stato e metodi.
   - `static string riga` → `string _riga`; `static int indcar` → `int _indcar`; `IndCar` proprietà d'istanza.
   - `SetCode` può restare `static readonly` (è una tabella costante).

2. **`Compiler`** (`Parsing/Compiler.cs`):
   - Rendere i metodi d'istanza; il `Compiler` crea/usa un proprio `Parser`.
   - `TabellaDebug` → `InstrToLineMap` (proprietà `List<int>`, istruzione→riga).
   - Aggiungere `int[] LineToInstrMap` (riga→indice istruzione), costruita a fine compilazione; valore **-1** per righe non eseguibili (vuote, solo-commento, solo-etichetta).
   - **Rimuovere** il parametro `rigaTrap`, la variabile `messoTrap` e `istr.Trap = true`. Firma nuova: `List<Instruction> CompilaCodice(List<string> code, ref List<CompilerError> errori)`.

3. **`Cpu`** (`Processore/Cpu.cs`):
   - Tutti i campi (`ax..ip`, `flags`, `stop`, `loopInfinito`, `memoria`, `Code`, `curIstruzione`, `Stato`, `inTrap`) → d'istanza.
   - Esporre `public short SP => sp;` (oggi manca).
   - Aggiungere `public HashSet<int> Breakpoints { get; } = new();`
   - Sostituire il controllo `Trap()` in `Run()` con `if (Breakpoints.Contains(ip)) throw new CpuTrapException();` (vedi pseudo-codice in `README.reviewed.md` §2).
   - Aggiungere `StepOver()`, `StepOut()`, `RunWhileInside(short limite)` con la logica corretta per **stack discendente** (vedi sotto, "Motore di debug").
   - Rimuovere `SetTrap`, `inTrap`, `Trap()`.

4. **`Instruction`** (`Parsing/Instruction.cs`): rimuovere il campo `Trap` (non più usato). Mantenere `Code`, `indRiga`, operandi.

5. **`Storage`** (`Backend/Local/Storage.cs`):
   - Rimuovere `using System.Drawing;` e i metodi `SalvaStatoFinestre`/`LeggiStatoFinestre`.
   - Può restare `static` (è un servizio I/O senza stato), ma le **path** vanno corrette (vedi punto 6).

6. **Cross-platform delle path (NON nel piano originale, ma necessario):**
   `Common/Ambiente.cs` costruisce le path con separatori Windows hardcoded
   (`+ "\\"`, `+ "\\EasyCPU\\"`, `PATHPROGETTI = "EasyCPU Progetti\\"`). Su
   macOS/Linux questo rompe il salvataggio di opzioni/recenti. Sostituire con
   `Path.Combine(...)` / `Path.DirectorySeparatorChar`. Rimuovere anche il
   `using System.Drawing;` di `Ambiente.cs` (residuo, non più usato dopo la
   rimozione dei riferimenti a `Font`).

7. **Progetto di test**: creare `EasyCpu.Assembler.Tests` (xUnit, `net10.0`) con riferimento ad `Assembler`/`Common`.

### Verifica

- `dotnet build` dei tre progetti business → 0 errori.
- `dotnet test` verde. Test minimi obbligatori:
  - **Step Into**: programma `mov ax,5 / inc ax / stop` → dopo 2 step `AX==6`.
  - **Step Over su `call`**: il `call` e tutta la subroutine vengono eseguiti in un colpo e **`SP` torna al valore pre-call** (controlla il verso dello stack).
  - **Step Out**: partendo da dentro la subroutine, esegue fino al `ret` e `SP` risale di 1.
  - **Breakpoint multipli**: due breakpoint in punti diversi; `Run()` si ferma al primo, poi al secondo.
  - **`LineToInstrMap`**: su un sorgente con righe vuote, commenti e un'etichetta su riga propria, le righe non eseguibili mappano a -1 e le altre all'indice corretto.
  - **Determinismo istanze**: due `Cpu` istanziate in parallelo non condividono stato (regressione contro il vecchio `static`).

### Problemi probabili

- **Verso dello stack invertito**: è l'errore del piano originale. Lo stack **cresce verso il basso** (`Init` SP=256, push `sp--`, pop `sp++`). Se Step Over/Out "scappano" fino a fine programma o non si fermano, è perché le condizioni sono invertite.
- **`StepOver` guarda l'istruzione sbagliata**: dopo uno step `curIstruzione` è quella **già eseguita**; per decidere se è un `call` guardare **`code[ip]`**, non `curIstruzione`.
- **Loop infinito non rilevato durante step**: `RunWhileInside` deve replicare i controlli `IPOverRun` e loop-infinito presenti in `Run()`, altrimenti la `SospendiWindow` non scatta.
- **`Stato` statico residuo** o altri campi dimenticati `static`: causano "stato condiviso" tra istanze. Cercare la keyword `static` in `Cpu.cs` a fine refactor.
- **`Ram` resta con costanti `static readonly`** (`MASSIMO_INDIRIZZO`, `INDIRIZZO_STACK`): va bene, sono costanti; ma `memoria` è già d'istanza — assicurarsi che `Cpu` ne crei una nuova per ogni `Init`.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-28)

> Queste note documentano scelte fatte durante l'implementazione che non erano
> esplicitate nel piano originale e che impattano le fasi successive.

1. **Contratto `StepInto` dopo un breakpoint (impatta Fase 3 — UI)**
   `Run()` controlla `Breakpoints.Contains(ip)` **prima** di `Fetch`+`Execute`. Quando scatta
   una `CpuTrapException`, `ip` rimane sull'istruzione del breakpoint (non ancora eseguita).
   Per riprendere l'esecuzione, la UI (ViewModel, Fase 3) deve:
   - Chiamare `StepInto()` una volta (avanza oltre il breakpoint senza ri-verifica).
   - Poi chiamare `Run()` o un altro comando di debug.
   Non fare `Run()` direttamente dopo il trap: re-triggherebbe immediatamente lo stesso breakpoint.

2. **`RunWhileInside(short limite)` — semantica: `sp < limite`**
   L'helper privato esegue passi finché `sp < limite`.
   - `StepOver` lo chiama con `limite = S` (SP pre-call): esegue mentre si è dentro la subroutine (`sp < S`), si ferma quando il `ret` riporta `sp == S`.
   - `StepOut` lo chiama con `limite = S + 1` (SP corrente + 1): equivale a "finché `sp ≤ S`", si ferma quando un `ret` porta `sp > S`.
   Questa simmetria (`StepOut = RunWhileInside(sp + 1)`) non era nel piano originale ma è verificata dai test.

3. **`PreparaRiga` invariato — marker commento `//` confermato**
   `AdattaRiga` in `Parser.cs` usava `'` come marcatore di commento (dead code rimosso).
   Il vero marker di commento è `//`, gestito da `Compiler.PreparaRiga`. La logica è stata
   mantenuta identica all'originale. Il `'` dentro `PreparaRiga` serve solo a rilevare
   costanti carattere (per non convertire in minuscolo il char tra apici).

4. **Mapping 0-based/1-based da gestire in Fase 3**
   `indRiga` nel compilatore e `InstrToLineMap`/`LineToInstrMap` usano indici **0-based**.
   AvaloniaEdit usa righe **1-based** (Fase 3, `BreakpointMargin` e `DebugCurrentLineRenderer`).
   La conversione `lineaAvaloniaEdit = indRiga + 1` va fatta **una sola volta** nell'adapter
   UI, mai nel core. Non introdurre offset nei test del core.

---

## FASE 2 — Dock + struttura ViewModel

**Obiettivo:** shell della IDE con pannelli dockabili e ViewModel collegati al core istanziato.

### Passi

- `DockFactory : Factory` che costruisce il layout iniziale (vedi `README.md` §8).
- ViewModel per pannello: `CodeEditorViewModel`, `DataEditorViewModel`, `RegistersViewModel`, `MemoryViewModel`, `StackViewModel`, `ErrorsViewModel`.
- `MainViewModel` con `[RelayCommand]` (CommunityToolkit) per i comandi IDE/debug; possiede l'istanza `Cpu` + `Compiler`.
- Creare qui il **`SettingsViewModel` singleton** (sorgente osservabile delle opzioni, vedi Fase 6) e iniettarlo in `MainViewModel`/pannelli: serve già da Fase 5 per la formattazione dei dump.
- Registrare i `DataTemplate` View↔ViewModel (`ViewLocator` o `DataTemplates` in `App.xaml`).
- Serializzazione layout con `Dock.Serializer.SystemTextJson`.

### Verifica

- L'app mostra i pannelli (anche vuoti) nel layout previsto; i pannelli si possono trascinare/agganciare.
- Spostando un pannello, chiudendo e riaprendo l'app, il layout si ripristina (persistenza, vedi Fase 7 — qui basta che il serializer non lanci).

### Dock 12: architettura con CommunityToolkit.Mvvm

Il progetto usa **`Dock.Model.Mvvm`** — il sotto-package che basa tutte le classi Dock su `ObservableObject` di CommunityToolkit, coerente con la scelta di non usare ReactiveUI.

> Nota: esiste anche `Dock.Model.ReactiveUI` (basa le classi su `ReactiveObject`). Sono alternative mutualmente esclusive: mescolarle genera conflitti di tipo `IFactory` a runtime, non a compile-time. Non aggiungere mai `Dock.Model.ReactiveUI`.

**Regola operativa (corretta dopo verifica sorgenti):**
- Tipi strutturali Dock (Document, Tool, RootDock, ProportionalDock, DocumentDock, ToolDock, ProportionalDockSplitter) → `using Dock.Model.Mvvm.Controls;`
- `Factory` (classe base DockFactory) → `using Dock.Model.Mvvm;`
- **`IRootDock`** → `using Dock.Model.Controls;` (**NON** `Dock.Model.Core` — errore frequente)
- `IDockable`, `IFactory` → `using Dock.Model.Core;`
- `Orientation` per ProportionalDock → `using Dock.Model.Core;` (enum `Orientation.Horizontal/Vertical`)

#### Tipo Dock per ogni pannello

I ViewModel pannello **ereditano** da `Tool` o `Document` (da `Dock.Model.Mvvm.Controls`) — sono gli item Dock, non li contengono. La classe deve essere `partial` per i source generator di CommunityToolkit.

| ViewModel | Tipo Dock | Motivo |
|---|---|---|
| `CodeEditorViewModel` | `Document` | area editing principale; supporta tab multipli |
| `DataEditorViewModel` | `Document` | idem per sezione `.DATA` |
| `RegistersViewModel` | `Tool` | pannello ausiliario, agganciabile ai lati |
| `MemoryViewModel` | `Tool` | idem |
| `StackViewModel` | `Tool` | idem |
| `ErrorsViewModel` | `Tool` | idem (come "Output" in VS) |

`MainViewModel : ObservableObject` — coordina, non è un item Dock.

`DockFactory` eredita da `Factory` di `Dock.Model.Mvvm`:

```csharp
using Dock.Model.Controls;   // IRootDock
using Dock.Model.Core;       // IDockable, Orientation
using Dock.Model.Mvvm;       // Factory
using Dock.Model.Mvvm.Controls; // DocumentDock, ToolDock, ProportionalDock, ecc.

public class DockFactory : Factory
{
    public override IRootDock CreateLayout()
    {
        var codeEditor = new CodeEditorViewModel { Id = "CodeEditor", Title = "Codice" };
        var documentDock = new DocumentDock
        {
            CanCreateDocument = false,
            Proportion = 0.65,
            ActiveDockable = codeEditor,
            VisibleDockables = CreateList<IDockable>(codeEditor, dataEditor)
        };
        // Root: CreateRootDock() factory method; layout: new ProportionalDock(Orientation.Horizontal)
        // Splitter tra dockables: new ProportionalDockSplitter()
    }
}
```

Panel VM con CommunityToolkit — classe `partial`, proprietà con `[ObservableProperty]`:

```csharp
public partial class RegistersViewModel : Tool
{
    [ObservableProperty]
    private string _dump = "";
}
```

`MainViewModel` usa `[RelayCommand]` al posto di `ReactiveCommand`:

```csharp
public partial class MainViewModel : ObservableObject
{
    [RelayCommand] private void StepInto() { /* ... */ }
    [RelayCommand] private void Run() { /* ... */ }
}
```

#### Confine serializzazione (obbligatorio per la persistenza di Fase 7)

`Dock.Serializer.SystemTextJson` serializza i `Tool`/`Document` come parte del layout. Questo impone un confine netto:

- **Nei ViewModel pannello (`Tool`/`Document`) NON mettere**: riferimenti a `Cpu`, liste di `CompilerError`, qualsiasi stato runtime.
- **Mettere solo**: proprietà che descrivono l'aspetto (titolo, visibilità, dimensioni) e comandi che delegano a `MainViewModel`.

Lo stato vivo (dump registri/memoria/stack, errori) vive in `MainViewModel`; i pannelli lo raggiungono via binding/subscribe. Senza questo confine il deserializer crasha al riavvio su riferimenti non serializzabili.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-28)

1. **`IRootDock` è in `Dock.Model.Controls`** — stessa dll di `Dock.Model.Mvvm` via dipendenza transitiva `Dock.Model`. `Dock.Model.Core` contiene solo interfacce di base (`IDockable`, `IFactory`, `Orientation`). Il codice guida sopra aveva il namespace sbagliato.

2. **Tema Dock via classe XAML** — `<dockTheme:DockFluentTheme />` con `xmlns:dockTheme="clr-namespace:Dock.Avalonia.Themes.Fluent;assembly=Dock.Avalonia.Themes.Fluent"`. La classe è `Dock.Avalonia.Themes.Fluent.DockFluentTheme`, risorsa interna `/DockFluentTheme.axaml`.

3. **`DockControl` richiede sia `Layout` che `Factory` bindati** — senza `Factory` le operazioni di drag/drop non funzionano. Esporre `DockFactory` come proprietà `IFactory` su `MainViewModel`.

4. **ViewLocator `Match`** — limitato a `Tool or Document` (non tutta `ObservableObject`) per evitare che Dock tenti di risolvere le viste per i propri tipi interni.

5. **`SettingsViewModel` write-through rimandato** — per Fase 2, il singleton inizializza solo le proprietà da `Ambiente.*`. I callback `partial void OnXxxChanged` sono Fase 6.

6. **`#nullable enable`** — aggiunto nei file che usano annotazioni nullable (`?`) senza abilitarle nel csproj globale (per non toccare i warning del codice core pre-esistente).

### Problemi probabili

- **Dock 12 + ReactiveUI**: vedi sezione dedicata sopra. Il setup attuale è già corretto.
- **Tema Dock mancante**: includere `Dock.Avalonia.Themes.Fluent` in `App.xaml`, altrimenti i separatori/tab sono invisibili.
- **ViewLocator non trova le View**: i pannelli appaiono come testo "Not Found". Verificare convenzione di naming `XxxViewModel`→`XxxView` e namespace.
- **Serializzazione**: costruttori senza parametri obbligatori per tutti i `Tool`/`Document`; niente riferimenti ciclici nelle proprietà serializzate. Lo stato runtime della CPU non va nei pannelli Dock (vedi confine serializzazione sopra).

---

## FASE 2b — Menu IDE, Toolbar e Temi

**Obiettivo:** chrome completo della finestra desktop (menu + toolbar + temi), toolbar touch per mobile/browser. Tutte le voci sono cablate a comandi già presenti o stub che saranno completati nelle fasi successive.

### Passi

1. **`AppTheme.cs` — enum + variante Blue**
   - Nuovo file `AppTheme.cs` in `EasyCPU/`: `enum AppTheme { Light, Dark, Blue }`.
   - Stessa classe: `static class AppThemeVariants` con `public static readonly ThemeVariant Blue = new ThemeVariant("Blue", ThemeVariant.Dark)`.
   - In `App.axaml`, aggiungere `ResourceDictionary.ThemeDictionaries`: chiave `{x:Static local:AppThemeVariants.Blue}` sovrascrive `SystemAccentColor` con `#007ACC` (VS Code blue), più `Dark1`/`Dark2`/`Light1` attorno.

2. **`SettingsViewModel` — aggiungere `Theme`**
   - `[ObservableProperty] private AppTheme _theme = AppTheme.Light;`
   - Callback: `partial void OnThemeChanged(AppTheme value) => App.ApplyTheme(value);`
   - `App.ApplyTheme(AppTheme)` (metodo `static`) → `Application.Current!.RequestedThemeVariant = ...`

3. **`MainViewModel` — nuovi comandi stub**
   - File: `New`, `Open`, `Save`, `SaveAs`, `Exit`
   - Modifica: `Undo`, `Redo`, `Cut`, `Copy`, `Paste`, `SelectAll`, `Find` — tutti `{ }`, collegati all'editor in Fase 3
   - Esegui: aggiungere `RunUntil`, `Stop`, `ToggleBreakpoint` — stub
   - Strumenti: `ShowOptions` — stub (Fase 6)
   - Tema: `[RelayCommand] private void SetTheme(AppTheme t) => Settings.Theme = t;`

4. **Finestre — visibilità pannelli**
   - `DockFactory`: aggiungere proprietà per i sei ViewModel pannello (`CodeEditor`, `DataEditor`, `Registers`, `Stack`, `Memory`, `Errors`) e valorizzarle durante `CreateLayout()`.
   - `MainViewModel`: sei proprietà bool `Is*Visible` che leggono/scrivono `IDockable.IsVisible`; sei `[RelayCommand] private void Toggle*()` che le invertono.

5. **`MainWindow.xaml` — menu in-window + NativeMenu + toolbar**
   - Avvolgere il `DockControl` in un `DockPanel`; aggiungere `<Menu DockPanel.Dock="Top">` con le cinque voci.
   - Aggiungere `<NativeMenu.Menu>` con la stessa struttura (barra di sistema macOS).
   - Aggiungere `<ToolBar DockPanel.Dock="Top">` (sotto il Menu): bottoni testo `Nuovo / Apri / Salva | Compila / Avvia / Ferma / Step Into / Step Over / Step Out | Chiaro / Scuro / Blue`.

6. **`MainView.axaml` — toolbar touch (mobile/browser, stile Rider)**
   - Aggiungere un `Border` con `StackPanel` orizzontale in cima: `[File ▼] [Compila] [Avvia] [Ferma] [→ Into] [→→ Over] [↑ Out]`
   - Bottoni: `MinWidth="60" Padding="8,4"` per target touch ≥ 48 dp.
   - `[File ▼]` → `OpenCommand` (stub; in Fase 6 si sostituisce con ActionSheet).

### Struttura menu completa (da replicare in Menu in-window e NativeMenu)

| Menu | Voci |
|---|---|
| **File** | Nuovo `Ctrl+N` / Apri... `Ctrl+O` / Salva `Ctrl+S` / Salva come... `Ctrl+Shift+S` / ─ / Recenti ▶ *(stub)* / ─ / Stampa `Ctrl+P` / ─ / Esci `Alt+F4` |
| **Modifica** | Annulla `Ctrl+Z` / Ripristina `Ctrl+Y` / ─ / Taglia `Ctrl+X` / Copia `Ctrl+C` / Incolla `Ctrl+V` / ─ / Seleziona tutto `Ctrl+A` / ─ / Trova... `Ctrl+F` |
| **Esegui** | Compila `Ctrl+B` / ─ / Avvia `F5` / Avvia fino a... `Ctrl+F5` / ─ / Esegui istruzione `F11` / Passo `F10` / Passo uscita `Shift+F11` / ─ / Ferma `F8` / ─ / Imposta/Rimuovi breakpoint `F9` |
| **Finestre** | ✓ Editor codice / ✓ Editor dati / ✓ Registri / ✓ Stack / ✓ Memoria / ✓ Errori / ─ / Ripristina layout |
| **Strumenti** | Opzioni... *(stub → Fase 6)* / ─ / Tema ▶ Chiaro / Scuro / Blue (VS Code) |

### Verifica

- `dotnet build` 0 errori.
- App desktop: menu in-window visibile; su macOS visibile anche nella barra di sistema.
- Cambio tema (Chiaro/Scuro/Blue) aggiorna visivamente l'app senza riavvio.
- Voci Finestre: il checkmark riflette la visibilità del pannello; click lo toglie/aggiunge.
- App mobile/browser: compare la touch bar, non il menu classico.
- Tutti i comandi stub cliccabili senza crash.

### Problemi probabili

- **`NativeMenu` binding vuoto su macOS**: i `NativeMenuItem` usano il `DataContext` della finestra (MainViewModel). Se le voci appaiono grigie o non reagiscono, verificare che `DataContext` sia già settato prima di `InitializeComponent()`.
- **`MenuItem.IsChecked` non si aggiorna al click**: in Avalonia il click esegue il Command ma non togla automaticamente `IsChecked`. Usare `IsChecked="{Binding IsCodeEditorVisible, Mode=OneWay}"` e il Command separato per invertire il bool.
- **`IDockable.IsVisible` non nasconde il pannello**: se il tab rimane visibile, `DockableBase.IsVisible` potrebbe richiedere una notifica esplicita al DockControl; come fallback usare `Factory.RemoveDockable`/`AddDockable`. Annotare l'assunzione nel report.
- **`ThemeVariant Blue` non applicato**: se l'accento non cambia, verificare che la chiave del `ThemeDictionary` sia `{x:Static local:AppThemeVariants.Blue}` (non una stringa) e che `RequestedThemeVariant` sia impostato dopo che il framework è inizializzato.
- **Target touch troppo piccoli su Android**: usare `MinHeight="48"` sul StackPanel della touch toolbar.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-28)

1. **`NativeMenuItem` non supporta compiled bindings per `ToggleType`/`IsChecked`**: il precompiler XAML (AVLN3000) rifiuta `ToggleType="CheckMark"` + `IsChecked="{Binding ...}"` su `NativeMenuItem`. Soluzione: `NativeMenu` contiene solo `Header`, `Command`, `Gesture`; i checkmark (`ToggleType="CheckBox"/"Radio"` + `IsChecked` OneWay) vivono solo nell'`<Menu>` in-window di Avalonia.

2. **Blue theme interamente in C# — niente `ThemeDictionaries` XAML**: usare `{x:Static}` come `x:Key` in `Application.Resources.ThemeDictionaries` causa un crash silenzioso del precompiler XAML (`XamlLoadException: No precompiled XAML found` a runtime, ma 0 errori a build). Soluzione adottata: `ApplyTheme(AppTheme.Blue)` in `App.xaml.cs` imposta `RequestedThemeVariant = Dark` e inietta i cinque `SystemAccentColor*` direttamente in `Application.Current.Resources` a runtime.

3. **`IDockable.IsVisible` non esiste in Dock 12**: la proprietà non è definita in `IDockable`/`DockableBase` nella versione 12.0.0.2. La visibilità si gestisce con `IFactory.HideDockable(IDockable)` / `IFactory.RestoreDockable(IDockable)` e si controlla via `IDockable.DockingState == DockingWindowState.Hidden`.

4. **Avalonia 12 non ha `<ToolBar>`**: il tipo `ToolBar` non esiste nel namespace `https://github.com/avaloniaui` (AVLN2000 a build). La toolbar è stata implementata come `<Border>` + `<StackPanel Orientation="Horizontal">`, identico al pattern già usato per la touch toolbar in `MainView.axaml`.

5. **File `.xaml` vanno dichiarati `<AvaloniaXaml>` nel csproj**: `<AvaloniaResource Include="**\*.xaml"/>` embeds i file come risorse grezze senza precompilazione. Sostituito con `<AvaloniaXaml Include="**\*.xaml"/>` per permettere al precompiler di generare il bytecode. I file `.axaml` sono auto-inclusi dall'Avalonia SDK.

---

## FASE 3 — Editor con debug integrato

**Obiettivo:** editor AvaloniaEdit con margine breakpoint cliccabile ed evidenziazione riga corrente.

### Passi

- `BreakpointMargin : AbstractMargin` (margine a sinistra dei numeri di riga): click → toggle in `MainViewModel.Breakpoints` (numeri di riga).
- `DebugCurrentLineRenderer : IBackgroundRenderer`: evidenzia `MainViewModel.CurrentSourceLine`; quando vale -1 non disegna.
- Portare la logica di `EasyEditor` (vedi `README.md` §9) su AvaloniaEdit via `TextArea.TextEntering`/`TextEntered` (Tab = spazi fino al margine, Enter = rientro di `Ambiente.MargineSinistro` spazi) e le conversioni riga↔offset con `Document.GetLineByNumber`/`CaretOffset`.

### Verifica

- Click nel margine aggiunge/toglie il pallino e aggiorna `Breakpoints`.
- Durante il debug, la riga corrente è evidenziata e si sposta a ogni step.
- Tab/Enter rispettano `MargineSinistro` (default 7).
- Un breakpoint su una riga **non eseguibile** non blocca o viene rimappato (coerenza con `LineToInstrMap == -1`).

### Problemi probabili

- **Mapping riga↔indice**: AvaloniaEdit usa righe **1-based**; `indRiga` del compilatore è **0-based**. Sbagliare l'offset di 1 sposta breakpoint ed evidenziazione di una riga. Definire la convenzione una volta e testarla.
- **Invalidazione del renderer**: senza `TextView.InvalidateLayer`/`InvalidateVisual` al cambio di `CurrentSourceLine`, l'evidenziazione "resta indietro". Collegare via `WhenAnyValue(x => x.CurrentSourceLine)`.
- **TextMate vs renderer custom**: l'highlighting TextMate e il `IBackgroundRenderer` lavorano su layer diversi; verificare l'ordine di disegno (il giallo della riga non deve coprire il testo).
- **AvaloniaEdit 12 API**: alcune firme (`AbstractMargin`, `VisualLine`) sono cambiate rispetto a esempi vecchi (11.x). Usare la documentazione 12.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-29)

1. **`AbstractMargin` non ha `Background` — hit testing su aree vuote**
   `AbstractMargin` estende `Control`, non `TemplatedControl`: la proprietà `Background` non è disponibile. Senza contenuto disegnato nel `Render()`, Avalonia non supera l'hit test sull'area vuota del margine: `OnPointerPressed` viene chiamato solo cliccando su un pallino già disegnato, non sull'area vuota. Fix: disegnare un fill quasi-trasparente (`new SolidColorBrush(Color.FromArgb(1, 0, 0, 0))`) all'inizio di `Render()` per coprire l'intera area, rendendo il controllo hit-testable ovunque. NON usare `Background` (non disponibile su `Control`).

2. **Click handling breakpoint — solo `BreakpointMargin.OnPointerPressed`**
   Il toggle va gestito esclusivamente dall'override di `OnPointerPressed` in `BreakpointMargin`. Aggiungere handler aggiuntivi sul `LineNumberMargin` (via `AddHandler` con `handledEventsToo: true`) causa double-toggle e attivazione senza disattivazione. L'unico punto di ingresso per il toggle da UI è `BreakpointMargin.OnPointerPressed`.

3. **`TextView.VisualLinesChanged` — obbligatorio per ridisegno pallini**
   Senza sottoscrivere `TextView.VisualLinesChanged` e chiamare `InvalidateVisual()` nel handler, i pallini non vengono ridisegnati dopo che AvaloniaEdit ricostruisce le `VisualLines` (es. al primo click o al resize). Override di `OnTextViewChanged` per gestire subscribe/unsubscribe sull'old/new TextView.

4. **Calcolo numero di riga — aritmetica diretta**
   `textView.GetVisualLineFromVisualTop(y)` può restituire `null` nelle aree di gap tra righe visuali. Calcolo robusto: `int lineNumber = (int)(docY / lineHeight) + 1`, dove `docY = e.GetPosition(this).Y + textView.VerticalOffset`. Usare `textView.DefaultLineHeight` (non `DefaultLineHeight` da VisualLines, che richiede righe già costruite).

5. **Clipboard — API completamente cambiata in Avalonia 12.0.5**
   `IClipboard.SetTextAsync(string)` e `GetTextAsync()` non esistono più. Scrittura: `DataTransferItem.CreateText(text)` + `new DataTransfer()` + `clipboard.SetDataAsync(transfer)`. Lettura: `await clipboard.TryGetDataAsync()` + `await AsyncDataTransferExtensions.TryGetTextAsync(data)`. Tutto in `Avalonia.Input`. `GetCurrentPoint` accetta `Visual?` (non `IInputElement`): passare `null` per coordinate assolute.

6. **`SearchPanel.Install()` prende `TextEditor`, non `TextArea`**
   In AvaloniaEdit 12, la firma corretta è `SearchPanel.Install(TextEditor editor)`. Passare `_editor.TextArea` causa `CS1503` a compile time.

7. **Testo pannello perso su hide/show — causa Dock.Avalonia**
   Dock.Avalonia ricrea `CodeEditorView` ogni volta che il pannello viene nascosto e rimostrato. Fix: `_editor.Document.Changed` sincronizza il testo in `CodeEditorViewModel.SourceText`; `OnDataContextChanged` ripristina `_editor.Document.Text = vm.SourceText` al riattach. Stessa logica in `DataEditorView`.

8. **DataEditorView — editor AvaloniaEdit completo + separazione compilazione**
   Il pannello `.DATA` ha un editor AvaloniaEdit autonomo (senza `BreakpointMargin` né `DebugCurrentLineRenderer`). `Compile()` in `MainViewModel` legge `_factory.CodeEditor.SourceText` e `_factory.DataEditor.SourceText` separatamente; non usa più lo split sulla stringa `.DATA` dentro il pannello codice.

---

## FASE 4 — Syntax highlighting (subset x86 reale)

**Obiettivo:** evidenziazione corretta del linguaggio EasyCPU.

### Passi

- File `.xshd` con il **set istruzioni reale** (vedi Appendice A — 35 opcode), i registri (`ax bx cx dx si di bp sp ip`), l'**indirizzamento indiretto** (`[bx] [bp] [si] [di]` con offset), commenti `//`, costanti char tra apici `'…'`, etichette `token:`, numeri decimali ed esadecimali (suffisso `h`), e il marcatore sezione dati `.DATA`.
- Registrare l'`.xshd` come `EmbeddedResource` e caricarlo all'avvio dell'editor.

### Verifica

- Tutti e 35 gli opcode si colorano; nessuna istruzione "dimenticata" (confronto con Appendice A).
- `//` colora il commento fino a fine riga; `'A'` è trattato come costante char; le etichette e gli indirizzi indiretti sono riconosciuti.

### Problemi probabili

- **Set istruzioni incompleto**: il piano originale (§4) elencava gli opcode a memoria. La fonte autoritativa è `Parser.SetCode` (Appendice A). Mancano facilmente `movs`, `jcxz`, `pushf`, `popf`.
- **Distinzione dati/codice**: il file ha due sezioni separate da `.DATA`; l'highlighting non distingue le sezioni — accettabile, ma documentarlo.
- **Numeri hex**: il parser usa `HexToInt`/suffisso `h`; allineare la regex `.xshd` a quel formato, non a `0x…`.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-29)

1. **36 opcode in `Parser.SetCode`, non 35**
   Conteggio esatto dal sorgente: 36 voci (`mov movs add sub cmp and or xor not neg mul div inc dec push pop pushf popf call jcxz je jg jl jle jge jmp jne jo jno js jns ret nop stop shl shr`). L'Appendice A di questa guida diceva 35 — va corretta.

2. **Registrazione highlighting tramite `HighlightingManager`**
   L'`.xshd` è un `EmbeddedResource` in `EasyCPU/Resources/EasyCPU.xshd`. Viene caricato in `App.OnFrameworkInitializationCompleted()` prima di tutto il resto (prima di `Ambiente.Inizializza`), così le view lo trovano già registrato al momento del loro `SetupEditor()`. Il nome registrato è `"EasyCPU"`, le estensioni associate `.as` e `.asj`.

3. **`new XmlTextReader(stream)`, non `XmlReader.Create()`**
   `HighlightingLoader.Load()` di AvaloniaEdit 12 accetta `XmlReader`. Si usa `XmlTextReader` (che eredita da `XmlReader`) per evitare configurazioni aggiuntive di `DtdProcessing`. `XmlReader.Create()` richiederebbe `DtdProcessing.Ignore` per non sollevare warning.

4. **Nome risorsa embedded**
   Il RootNamespace del progetto è `EasyCPU` (inferred dall'SDK, nessun `<RootNamespace>` esplicito nel csproj). Il file è in `Resources/EasyCPU.xshd`. Il nome manifest risultante è `EasyCPU.Resources.EasyCPU.xshd`. Se il nome cambia (es. in seguito a rename del progetto), aggiornare la stringa in `App.RegisterEasyCpuHighlighting()`.

5. **`DataEditorView` — stesso highlighting applicato**
   Il pannello dati contiene numeri, etichette e costanti hex con la stessa sintassi del pannello codice. Applicare lo stesso `.xshd` è corretto e conveniente. Gli opcode non appariranno nel pannello dati in uso normale, ma non danno fastidio se presenti.

6. **Palette colori finale — opcode differenziati per categoria**
   Prima iterazione: tutti gli opcode con un unico colore blu. Seconda iterazione: 7 categorie con colori distinti, ma Aritmetica/Salti/Stack erano tutti nel range arancio-rosso-marrone e Movimento/Confronto entrambi nel range blu-viola. Palette finale distribuita sull'intero cerchio cromatico (ottimizzata per tema Light, default dell'app):

   | Categoria | Opcode | Colore |
   |---|---|---|
   | Movimento | `mov movs` | `#1565C0` blu |
   | Aritmetica | `add sub mul div inc dec neg` | `#5D2906` marrone scuro |
   | Logica/shift | `and or xor not shl shr` | `#0F766E` teal |
   | Confronto | `cmp` | `#AD1457` rosa scuro |
   | Stack | `push pop pushf popf` | `#4527A0` indaco |
   | Salti/controllo | `jmp j* call ret jcxz` | `#C62828` rosso |
   | Varie | `nop stop` | `#546E7A` grigio-blu |

   Su tema Dark/Blue i colori rimangono leggibili ma non ottimali — miglioramento estetico rimandato a scelta futura.

7. **Distinzione sezione codice/dati nell'highlighting**
   L'`.xshd` non distingue la sezione codice dalla sezione dati (sopra/sotto `.DATA`): entrambi i pannelli usano le stesse regole. Accettabile: i due pannelli sono editor separati e il contesto è implicito.

---

## FASE 5 — Pannelli dockabili (contenuto)

**Obiettivo:** Registri, Memoria, Stack, Errori popolati e reattivi.

### Stato degli stub (verificato al 2026-06-29)

Tutti e quattro i pannelli hanno ViewModel e View già creati in Fase 2, ma completamente vuoti:

| ViewModel | Stato attuale | View attuale |
|---|---|---|
| `RegistersViewModel` | `Tool` + `[ObservableProperty] string _dump` | `TextBlock Text="Registri"` |
| `MemoryViewModel` | `Tool` + `[ObservableProperty] string _dump` | `TextBlock Text="Memoria"` |
| `StackViewModel` | `Tool` + `[ObservableProperty] string _dump` | `TextBlock Text="Stack"` |
| `ErrorsViewModel` | `Tool` + `ObservableCollection<CompilerError> Errors` | `TextBlock Text="Errori"` |

`MainViewModel` ha già i commenti `// TODO Fase 5` nel punto esatto dove andranno i dump:
- In `Compile()` riga ~212: `if (instructions == null) return; // TODO Fase 5: mostrare errori`
- In `Run()` riga ~241: `catch (CpuException) { } // TODO Fase 5: mostrare errore`

`_factory.Registers`, `_factory.Memory`, `_factory.Stack`, `_factory.Errors` sono già esposti da `DockFactory` — `MainViewModel` li raggiunge via `_factory`.

### API del core (firme verificate)

```csharp
// Registri — array di 9 stringhe "AX = 42" formattate per Ambiente.FR
string[] Cpu.DumpRegs()

// Registri — singolo registro con formato esteso (hex/dec/bin/char)
string Cpu.DumpReg(int indReg)   // 0=AX … 8=IP

// Memoria — List<string> di righe "indirizzo: val val val …"
// Ritorna null se Cpu non è ancora inizializzata (guard prima di usarla)
List<string>? Cpu.DumpMemoria(int da, int a, int colonne)

// Costanti Ram
Ram.INDIRIZZO_STACK   // 240
Ram.MASSIMO_INDIRIZZO // 255

// Colonne per il dump Stack
Ambiente.ColonneStack  // default 1

// Flag — proprietà pubbliche già presenti (flags è private)
bool Cpu.FlagZero      // ZF
bool Cpu.FlagSegno     // SF
bool Cpu.FlagOverflow  // OF
```

**Nota `DumpReg` vs `DumpRegs`**: `DumpRegs()` produce righe brevi (`AX = 42`), utile per un pannello compatto. `DumpReg(i)` produce la riga estesa (`AX = [HEX: 002Ah] [BIN: 000…] [CAR: *]`), utile se si vuole espandere un registro. Usare `DumpRegs()` per il display normale.

**Attenzione `DumpReg`**: in `Cpu.DumpReg`, se `Ambiente.FormatoDati != Hex` la riga mostra `[HEX: …]`, altrimenti `[DEC: …]` — sembra invertito rispetto al nome. Verificare su `EasyCpu.Win` se è il comportamento atteso o un bug storico; documentare la decisione prima di usarlo.

### Errori di compilazione

```csharp
// CompilerError — campi pubblici
string Msg
int    Riga     // 0-based (indice compilatore) — display: Riga + 1
int    Colonna  // 0-based
int    Tipo     // CompilerError.CODICE = 0, CompilerError.DATI = 1

// Formati ToString
error.ToString()        // "riga+1: messaggio"
error.ToString("T", null) // "[Codice] (riga+1) messaggio"
```

### Passi

**1. `RefreshDebugViews()` in `MainViewModel`**

Aggiungere il metodo privato e chiamarlo dove già c'è `UpdateCurrentSourceLine()` (dopo ogni step/run/stop). Senza questo, i dump non si aggiornano mai.

```csharp
private void RefreshDebugViews()
{
    // Registri
    var regs = Cpu.DumpRegs();
    if (_factory.Registers is { } rv)
        rv.Dump = string.Join("\n", regs) +
                  $"\nZ={(Cpu.FlagZero ? 1 : 0)}  S={(Cpu.FlagSegno ? 1 : 0)}  O={(Cpu.FlagOverflow ? 1 : 0)}";

    // Memoria (0..239, 8 colonne — nessun setting per le colonne memoria, valore fisso)
    var mem = Cpu.DumpMemoria(0, Ram.INDIRIZZO_STACK, 8);
    if (_factory.Memory is { } mv)
        mv.Dump = mem is null ? "" : string.Join("\n", mem);

    // Stack (240..255)
    var stack = Cpu.DumpMemoria(Ram.INDIRIZZO_STACK, Ram.MASSIMO_INDIRIZZO + 1, Ambiente.ColonneStack);
    if (_factory.Stack is { } sv)
        sv.Dump = stack is null ? "" : string.Join("\n", stack);
}
```

Chiamarlo da: `StepInto`, `StepOver`, `StepOut`, `Run` (dopo ogni operazione, prima del return), `Stop`, `Compile` (reset dump a stringa vuota).

**2. Errori di compilazione in `ErrorsViewModel`**

In `Compile()`, dopo aver chiamato `CompilaCodice` e `CompilaDati`, popolare `_factory.Errors.Errors`:

```csharp
var ev = _factory.Errors;
if (ev != null)
{
    ev.Errors.Clear();
    foreach (var e in codeErrors ?? []) ev.Errors.Add(e);
    foreach (var e in dataErrors ?? []) ev.Errors.Add(e);
}
```

Rimuovere i commenti `// TODO Fase 5` e il `return` anticipato su `instructions == null` — mostrare invece gli errori e tornare senza inizializzare la CPU.

**3. View: `RegistersView.axaml`**

Sostituire il `TextBlock` placeholder con un `ScrollViewer` + `TextBlock` bound a `Dump`:
```xml
<ScrollViewer>
    <TextBlock Text="{Binding Dump}" FontFamily="Courier New,Monospace" FontSize="12"
               Margin="4" TextWrapping="NoWrap" />
</ScrollViewer>
```

Uguale per `MemoryView.axaml` e `StackView.axaml`.

**4. View: `ErrorsView.axaml`**

`DataGrid` con colonne esplicite bound a `CompilerError`:
```xml
<DataGrid ItemsSource="{Binding Errors}" IsReadOnly="True"
          SelectionMode="Single" DoubleTapped="OnErrorDoubleTapped">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Tipo" Binding="{Binding Tipo}" Width="60"/>
        <DataGridTextColumn Header="Riga" Binding="{Binding Riga, StringFormat={}{0}}" Width="50"/>
        <DataGridTextColumn Header="Messaggio" Binding="{Binding Msg}" Width="*"/>
    </DataGridTextColumn>
    </DataGrid.Columns>
</DataGrid>
```

Il `Riga` nella colonna va mostrato come `Riga + 1` (display 1-based) — usare un converter o esporre una proprietà calcolata `RigaDisplay => Riga + 1` su `CompilerError`. Non modificare `CompilerError` se è nel core; preferire un converter Avalonia.

**5. Doppio click su errore → naviga alla riga**

Nel code-behind di `ErrorsView.axaml.cs`:
```csharp
private void OnErrorDoubleTapped(object? sender, TappedEventArgs e)
{
    if (DataContext is not ErrorsViewModel vm) return;
    if (sender is not DataGrid grid) return;
    if (grid.SelectedItem is not CompilerError err) return;
    // Notify MainViewModel: naviga a err.Riga+1 in CodeEditor o DataEditor
    // (vm ha bisogno di un riferimento a MainViewModel, pattern uguale a CodeEditorViewModel.MainVm)
}
```

Esporre `MainVm` su `ErrorsViewModel` con lo stesso pattern di `CodeEditorViewModel` (passato dal `DockFactory` in `CreateLayout()`).

### Verifica

- Eseguendo passo-passo, registri/memoria/stack cambiano coerentemente con `EasyCpu.Win` sullo stesso sorgente (confronto a vista).
- Compilando un programma con errori, la `ErrorsView` si popola; compilando correttamente, si svuota.
- Doppio click su un errore porta alla riga giusta nell'editor.
- Fermando l'esecuzione, i dump mostrano lo stato finale della CPU.

### Problemi probabili

- **`Cpu.flags` è private** — usare le proprietà pubbliche già presenti: `Cpu.FlagZero`, `Cpu.FlagSegno`, `Cpu.FlagOverflow` (bool). Non accedere a `flags` direttamente.
- **`DumpMemoria` ritorna `null`** se la CPU non è inizializzata (guard `if (mem is null)`).
- **Formato dati Dec/Hex/Car**: `DumpRegs()` e `DumpMemoria` usano `Ambiente.FR`/`FI`/`FD`. Quando `SettingsViewModel.FormatoDati` cambia (Fase 6), bisogna richiamare `RefreshDebugViews()` — collegare via `PropertyChanged` su `Settings` nel costruttore di `MainViewModel` (già presente per il tema, stesso pattern).
- **`DumpReg` etichette `[HEX]/[DEC]` invertite**: verificare su `EasyCpu.Win` prima di usare `DumpReg` — potrebbe essere un bug storico. `DumpRegs()` non ha questo problema (non stampa l'etichetta del formato).
- **Stack range e direzione**: lo stack vive in 240–255 e cresce verso il basso; `DumpMemoria(240, 256, ...)` mostra gli indirizzi dall'alto verso il basso. Non invertire l'ordine delle righe.
- **Colonne memoria**: `DumpMemoria` per la memoria principale (0–239) non ha un `Ambiente.*` di riferimento. Usare un valore fisso (es. 8) e documentarlo; la configurabilità è rimandabile a Fase 6.
- **`CompilerError.Riga` 0-based nel DataGrid**: la colonna Riga deve mostrare `Riga + 1`. Opzioni: converter `IValueConverter`, proprietà calcolata `RigaDisplay` su `CompilerError` (tocca il core — evitare), o usare `StringFormat` con una classe di wrapping. Il converter è la soluzione più pulita in Avalonia 12.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-30)

1. **`Avalonia.Controls.DataGrid` — installato `ProDataGrid 12.0.4`**
   `DataGrid` non è nell'assembly `Avalonia.Controls.dll` di Avalonia 12.0.5. È stato installato `ProDataGrid 12.0.4` (NuGet), che distribuisce l'assembly `Avalonia.Controls.DataGrid.dll` con la stessa API standard del DataGrid Avalonia. `ErrorsView` usa `DataGrid` con `CanUserResizeColumns="True"`, `AutoGenerateColumns="False"` e tre `DataGridTextColumn` (Tipo / Riga / Messaggio, Width="*" sull'ultima). Il tema si include in `App.xaml` con `<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml"/>` — usare `Fluent.v2.xaml`, non `Fluent.xaml` (quest'ultimo ha solo `NamespaceInfo` precompilato, non un `Build:` compilato, e causa AVLN2000 a build). Gli errori vengono ordinati per `RigaDisplay` ascendente (secondario: `TipoDisplay`) tramite LINQ in `Compile()` prima di popolare la collection.

2. **`CompilerError` usa campi pubblici, non proprietà — pattern `CompilerErrorAdapter`**
   `CompilerError.Msg`, `.Riga`, `.Tipo` sono dichiarati `public string/int` (campi), non proprietà. Il precompiler Avalonia XAML (AVLN2000) non riesce a risolverli con `x:DataType`. Tentativi precedenti con `x:CompileBindings="False"` erano inaffidabili con `AvaloniaUseCompiledBindingsByDefault=true` globale (i binding di riflessione su campi ritornavano valori vuoti a runtime). Soluzione definitiva: `CompilerErrorAdapter` — wrapper UI-layer con proprietà C# regolari `TipoDisplay` (stringa "Codice"/"Dati"), `RigaDisplay` (1-based), `Msg`, `Source` (il `CompilerError` originale). `ErrorsViewModel.Errors` è `ObservableCollection<CompilerErrorAdapter>`; il `DataTemplate` usa `x:DataType="vm:CompilerErrorAdapter"` con compiled bindings standard. File `Views/Converters.cs` (con `Int32PlusOneConverter` e `CompilerErrorTipoConverter`) è rimasto ma non è più usato da `ErrorsView`.

3. **`Cpu.DumpMemoria` ritorna `null` a runtime nonostante la firma non-nullable**
   `Cpu.DumpMemoria` è dichiarato `List<string>` (non `List<string>?`) ma restituisce `null` se `this.memoria == null` (CPU non inizializzata). Con `#nullable enable` nel ViewModel il compilatore non avverte. Guard esplicito: `mem is null ? "" : string.Join(...)` in `RefreshDebugViews()`.

4. **`RefreshDebugViews()` — punto di chiamata**
   Chiamata da: `StepInto`, `StepOver`, `StepOut`, `Run` (incluso il ramo `_atBreakpoint`), `Stop`, `Compile` (dopo `Cpu.Init()` in caso di successo; clear esplicito di `Dump = ""` in caso di errore di compilazione). Non chiamata su `ToggleBreakpoint` né `SyncBreakpointsToCpu` (non cambiano lo stato della CPU).

5. **`ErrorsViewModel` ora richiede `MainViewModel` nel costruttore**
   Pattern identico a `CodeEditorViewModel`. `DockFactory.CreateLayout()` aggiornato: `new ErrorsViewModel(_mainVm)`. Il serializer layout Dock 12 non tenta di deserializzare i ViewModel (la serializzazione riguarda solo la struttura Dock); il costruttore con parametro non causa problemi.

6. **`NavigateToError`, `NavigateToLineAction` e attivazione pannello**
   `MainViewModel.NavigateToError(CompilerError)` seleziona `CodeEditor` o `DataEditor` in base a `err.Tipo`, poi:
   - Chiama `_factory.SetActiveDockable(editor)` per portare il tab in primo piano nel `DocumentDock`.
   - Invoca `NavigateToLineAction(lineNumber)` (tipo `Action<int>?`) wired in `SetupEditor` dei rispettivi code-behind.
   Il lambda in `SetupEditor` usa `Document.GetLineByNumber(n)` + `GetText` + `TrimStart()` per posizionare il caret sul primo carattere non-spazio della riga (se la riga è vuota o tutta spazi, caret a colonna 1). Il `Focus()` è differito con `Dispatcher.UIThread.Post(() => _editor.TextArea.Focus())` perché `SetActiveDockable` aggiorna solo la proprietà in modo sincrono — il rendering del tab avviene nel ciclo dispatcher successivo e `Focus()` prima di esso verrebbe ignorato. Se il pannello è nascosto (view non ancora creata), l'azione è `null` e la navigazione viene ignorata silenziosamente.

7. **`Compile()` ora esegue sempre entrambe le compilazioni**
   Nella versione precedente, se `CompilaCodice` falliva si ritornava prima di chiamare `CompilaDati`. Ora entrambe vengono sempre eseguite, e tutti gli errori (codice + dati) vengono mostrati nella `ErrorsView` in un'unica operazione. Il comportamento dell'utente migliora: si vedono tutti gli errori in un colpo solo.

8. **Colonne memoria (hardcoded a 8)**
   Il range principale (0–239, 240 celle) viene dumped con 8 colonne (`DumpMemoria(0, 240, 8)`). Non esiste un `Ambiente.*` per questo valore. Deciso di usare 8 colonne fisse, coerentemente con la visualizzazione tipica dei debugger. La configurabilità è rimandabile a Fase 6.

9. **Double-click su riga `DataGrid` — `DoubleTapped` in XAML non funziona**
   Registrare `DoubleTapped="handler"` direttamente sul `DataGrid` in XAML non produce l'invocazione dell'handler: il `DataGrid` marca `PointerPressed` come **handled** internamente (per gestire la selezione della riga), impedendo la propagazione agli handler XAML bubble. Soluzione: nell'`ErrorsView.axaml.cs` il costruttore registra il handler in codice con `AddHandler(InputElement.PointerPressedEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true)`. Il flag `handledEventsToo: true` bypassa il filtro sull'evento handled. Il `PointerPressedEventArgs.ClickCount == 2` distingue il double-click dal singolo. Il `SelectedItem` è già aggiornato al secondo click (la selezione avviene al primo).

10. **Ordinamento errori nel `DataGrid`**
    Gli errori di codice e di dati vengono raccolti in `Compile()`, ordinati via LINQ per `RigaDisplay` ascendente (secondario: `TipoDisplay` per stabilità), poi aggiunti a `ev.Errors`. Ordinamento lato ViewModel, non nel `DataGrid` (le colonne `CanUserSortColumns="False"` per semplicità — la lista è già pre-ordinata).

11. **Barra di stato — `StatusMessage` in `MainViewModel`**
    Aggiunta proprietà `[ObservableProperty] private string _statusMessage = "Pronto"` in `MainViewModel`. La barra è un `Border` con `DockPanel.Dock="Bottom"` aggiunto in **entrambi** `MainWindow.xaml` (percorso desktop) e `MainView.axaml` (percorso single-view/mobile), posizionato **prima** del `DockControl` (nel `DockPanel` l'ordine conta: l'ultimo figlio senza `Dock` esplicito riempie lo spazio residuo). Messaggi impostati in: `DoCompile()` (errori e successo), `Run()` (terminato / breakpoint / ciclo infinito), `StepInto/Over/Out` (riga corrente), `Stop()` (interrotto).

12. **"Avvia" riprende l'esecuzione da un breakpoint senza ricompilare**
    Se `_atBreakpoint == true`, il comando `Run` **non** chiama `DoCompile()`. Invece: (a) chiama `Cpu.StepInto()` per avanzare oltre l'istruzione che ha causato il trap — `StepInto` non controlla i breakpoint, evitando il ri-trap immediato sullo stesso indirizzo; (b) se dopo lo step la CPU non è ferma (`!Cpu.stop`), chiama `Cpu.Run()` normalmente per proseguire fino al prossimo breakpoint o alla fine. Se il programma termina durante lo step, si aggiornano le viste e si imposta il messaggio di stato senza chiamare `Run()`. Il ramo `else` (compilazione da zero) rimane invariato.

---

## FASE 6 — Dialog e gestione file ✅ COMPLETATA (2026-06-30)

**Obiettivo:** Opzioni, finestra loop infinito, apri/salva cross-platform.

### Cosa è stato implementato

- **`SettingsViewModel`** completato con write-through completo su `Ambiente.*` via `partial void OnXxxChanged`. Tutte le proprietà configurabili ora hanno callback; il core legge `Ambiente` staticamente e rimane coerente.
- **`OpzioniWindow`** + **`OpzioniViewModel`** (copia del singleton per editing isolato). Su OK: `vm.ApplyTo(Settings)` + `Storage.SalvaOpzioni()` + `RefreshDebugViews()`.
- **`SospendiWindow`** con enum `ModoSospendi { Arresta=0, Pausa, Continua }` e `Run()` async che fa loop su `CpuLoopException`.
- **`ISourceSerializer`** / **`EasyFileSerializer`** (JSON `.asj`, default) / **`LegacyAsSerializer`** (sola lettura) in `EasyCpu.Backend/Serializers/`.
- **Apri/Salva** con `IStorageProvider` (cross-platform). `DefaultExtension = ".asj"` sul dialog di salvataggio.
- **`FontPanelliSize`**: nuovo campo `Ambiente.FontPanelliSize` (float, default 12) persisto in `Storage`; font applicato live a `RegistersView`, `StackView`, `MemoryView` (TextBlock `"DumpText"`) ed `ErrorsView` (DataGrid `"ErrorsGrid"`) via sottoscrizione a `SettingsViewModel.PropertyChanged`.
- **`SetSourceTextAction`**: callback `Action<string>?` in `CodeEditorViewModel`/`DataEditorViewModel`, wired in `SetupEditor()`, usata da `New()`/`Open()` per aggiornare il contenuto dell'editor direttamente (AvaloniaEdit non espone `Document.Text` come proprietà bindabile).

### Gestione di `Ambiente` e delle opzioni in MVVM

`Ambiente` è una classe a soli **campi statici pubblici** (es. `public static int MaxNumErrori;`), senza proprietà né `INotifyPropertyChanged`: Avalonia non può fare binding bidirezionale affidabile su campi statici. Inoltre il **core legge le opzioni staticamente** (`Cpu.DumpReg`/`DumpMemoria` usano `Ambiente.FormatoDati`, `Ambiente.FI/FD/FR`): non possiamo eliminare `Ambiente` senza riscrivere il core. Soluzione a basso impatto, pienamente MVVM:

**`SettingsViewModel` — sorgente osservabile unica delle opzioni (singleton).**

1. `SettingsViewModel : ObservableObject` (classe `partial`, CommunityToolkit) con una proprietà `[ObservableProperty]` per ogni opzione: `FormatoDati`, `FormatoCarZero`, `MaxNumErrori`, `ColonneStack`, `InizializzaRegistri`, `LoopInfinito`, `MargineSinistro`, `MostraMemoria`, `PienoSchermo`, `FontEditorNome`, `FontEditorSize` (float), `FontEditorStyle` (int), `FontPanelliSize` (float), `Theme`.
2. È un **singleton** (`SettingsViewModel.Instance`), primo accesso in `App.OnFrameworkInitializationCompleted()` dopo `Ambiente.Inizializza()` e `Storage.LeggiOpzioni()`. Tutta la UI fa binding su questo VM, **non** su `Ambiente`.
3. **Caricamento**: ordine critico — `Ambiente.Inizializza()` → `Storage.LeggiOpzioni()` → primo accesso a `SettingsViewModel.Instance` (che legge da `Ambiente.*` nel costruttore privato).
4. **Write-through verso il core**: ogni proprietà implementa il write-through nella callback parziale (`partial void OnFormatoDatiChanged(FormatoValore value) => Ambiente.FormatoDati = value;`). Il parametro della callback **deve chiamarsi `value`** (non `v` o altro) per evitare CS8826 con il source generator di CommunityToolkit.
5. **Reattività dei pannelli**: `RefreshDebugViews()` su `MainViewModel` è chiamata da ogni comando (step/run/stop/compile). I pannelli di output (`RegistersView` ecc.) si sottoscrivono a `PropertyChanged` per il font.
6. **Persistenza**: `Storage.SalvaOpzioni()` chiamata esplicitamente da `ShowOptions()` su OK.

**`OpzioniWindow` — editing con pattern OK/Annulla.**

7. La finestra opzioni lavora su **`OpzioniViewModel`** (copia snapshot del `SettingsViewModel`), non sul singleton.
8. Su **OK** (`ShowDialog<bool?>` ritorna `true`): `vm.ApplyTo(Settings)` → write-through automatico su `Ambiente.*` → `Storage.SalvaOpzioni()` → `RefreshDebugViews()`.
9. Su **Annulla** o chiusura via X: nessun effetto sul singleton.
10. **`x:CompileBindings="False"`** su `OpzioniWindow` obbligatorio: `NumericUpDown.Value` è `decimal?` ma le proprietà sono `int`/`float`. I compiled binding Avalonia non gestiscono la coercizione; il binding riflessivo la gestisce automaticamente.

> `Ambiente` resta **store di persistenza** (DTO statico). In una fase futura
> si potrà iniettare le opzioni nel core come istanza, ma **non è richiesto ora**.

### Formato file: nuovo formato di default + apertura del legacy

**Decisione presa:** estensione **`.asj`** per il nuovo formato JSON; il vecchio `.as` resta apribile (sola lettura).

**`ISourceSerializer`** (interfaccia in `EasyCpu.Backend/Serializers/`):
```
(string[] code, string[] data) Load(string path)
void Save(string path, string[] code, string[] data)
bool CanWrite
static ISourceSerializer ForPath(string path)   // factory: .asj → Easy, else → Legacy
```

**Autodetect basato sull'estensione** (non sul contenuto): `ForPath()` controlla `EndsWith(".asj")` — semplice e senza lettura anticipata del file.

**`EasyFileSerializer`**: JSON `{ "version": 1, "code": [...], "data": [...] }`. `CanWrite = true`.

**`LegacyAsSerializer`**: delega a `Storage.Apri()` (formato testuale legacy). `CanWrite = false`; `Save()` lancia `InvalidOperationException`.

**Comportamento dialogs** (`IStorageProvider`):
- Apertura: filtro `*.asj` + `*.as` (tutti i file supportati). Autodetect via `ForPath()`.
- Salvataggio: `DefaultExtension = ".asj"`, `SuggestedFileName` senza estensione (es. `"file1"` non `"file1.asj"`) — il dialog aggiunge l'estensione automaticamente. Se il nome contiene già `.asj`, usare `Path.GetFileNameWithoutExtension()`.
- Aprire un `.as` legacy imposta `_isLegacyFile = true`: il comando "Salva" reindirizza a "Salva con nome" (non sovrascrive il file legacy originale).

### Assunzioni e decisioni

1. **`GetOwnerWindow()` via lifecycle**: `NativeMenuItem` non supporta `CommandParameter` (AVLN3000), quindi i comandi File/Opzioni non possono ricevere la `Window` come parametro. Si recupera `IClassicDesktopStyleApplicationLifetime.MainWindow` direttamente.
2. **`ModoSospendi.Arresta = 0`** (primo nell'enum): `default(ModoSospendi)` = Arresta — comportamento sicuro quando il dialog viene chiuso con la X.
3. **`SetSourceTextAction`** in `CodeEditorViewModel`/`DataEditorViewModel`: `AvaloniaEdit.TextEditor.Document.Text` non è bindabile. La view espone un `Action<string>?` wired in `SetupEditor()`. `New()`/`Open()` chiama sia `vm.SourceText = text` (fallback se la view non è visibile) che `vm.SetSourceTextAction?.Invoke(text)` (aggiornamento diretto dell'editor quando è montato).
4. **`SuggestedFileName` senza estensione**: passare `"file1.asj"` causerebbe `"file1.asj.asj"` nel dialog (il `DefaultExtension` si aggiunge sempre). Usare `"file1"` o `Path.GetFileNameWithoutExtension(...)`.

### Verifica

- Cambiare `FormatoDati` da Dec a Hex → OK: dump cambiano subito, persistiti al riavvio; Annulla: nessun cambio.
- Provocare un loop infinito (`jmp` su sé stesso) → compare `SospendiWindow`; Continua riprende, Pausa congela, Arresta ferma.
- Salva → file `.asj` (JSON); riaprilo → codice e dati identici.
- Apri un vecchio `.as` → si apre; "Salva" propone nome nuovo senza sovrascrivere il legacy.
- Cambia `FontEditorSize` o `FontPanelliSize` → font aggiornato live nell'editor/pannelli senza riavvio.

---

## FASE 7 — Persistenza e file recenti ✅ COMPLETATA (2026-06-30)

**Obiettivo:** layout Dock persistente, file recenti, breakpoint persistenti.

### Cosa è stato implementato

**File recenti:**
- Corretti `Ambiente.AggiungiRecenti()` (troncamento a `MAXFILERECENTI`), `Storage.SalvaFileRecenti()` (foreach + Take) e `Storage.ApriFileRecenti()` (using + limite + skip righe vuote).
- Nuovo `ViewModels/RecentFileItem.cs` — wrapper con `Header` (path) e `Command` (RelayCommand con path catturata).
- `MainViewModel.RecentFileItems` (`ObservableCollection<RecentFileItem>`) + `RefreshRecentFileItems()` + `AddToRecentFiles()`.
- `[RelayCommand] OpenRecentFile(string path)` con guard `File.Exists`.
- `MainWindow.xaml`: voce "Recenti" nel menu File in-window con `ItemContainerTheme`/`ReflectionBinding` (obbligatorio per `AvaloniaUseCompiledBindingsByDefault=true`).
- `MainWindow.xaml.cs`: `SetupNativeRecentiMenu()` + `UpdateNativeRecentiMenu()` per popolare dinamicamente il submenu NativeMenu "Recenti" su macOS; aggiornato automaticamente via `CollectionChanged`.

**Persistenza breakpoint:**
- `SaveBreakpoints(string filePath)` / `LoadBreakpoints(string filePath)` / `SaveCurrentBreakpoints()` in `MainViewModel`.
- Sidecar `nomefile.bkpt` adiacente al sorgente; un numero di riga (1-based) per riga.
- Caricamento tollerante: file assente o righe non numeriche vengono ignorati.
- Integrati in `OpenFileFromPath()` (save old + load new), `New()` (save + clear), `SaveToPath()` (save).

**Persistenza layout:**
- `ViewModels/LayoutState.cs`: gerarchia polimorfica `DockNode` (`RootNode`/`PropNode`/`DocNode`/`ToolNode`/`SplitterNode`) serializzata con `[JsonPolymorphic]`/`[JsonDerivedType]` (discriminatore `"t"`).
- `DockFactory.ToDockNode(IDockable)`: walker ricorsivo dell'intero albero `IRootDock` — gestisce tutti i nodi compresi quelli creati dinamicamente da Dock quando l'utente sposta pannelli in nuove posizioni.
- `DockFactory.RebuildLayout(DockNode, Dictionary<string,IDockable?>)`: ricostruisce l'albero Dock da JSON, reinserendo le istanze reali dei ViewModel per ID. Aggiorna `DocumentContainer`/`ToolContainer` al primo `DocumentDock`/`ToolDock` incontrato.
- `MainViewModel.SaveLayout()` / `LoadLayout()` con `JsonSerializerOptions { NumberHandling = AllowNamedFloatingPointLiterals }` (Dock usa `double.PositiveInfinity` internamente per alcune proporzioni).
- `MainViewModel.SaveAll()`: metodo centralizzato che chiama `SaveLayout` + `SaveCurrentBreakpoints` + `Storage.SalvaFileRecenti`.

**Lifecycle e salvataggio:**
- `App.xaml.cs`: aggiunto campo `_mainViewModel`; al bootstrap chiama `Storage.ApriFileRecenti()` → `LoadLayout()` → `RefreshRecentFileItems()`; sottoscrive `desktop.Exit += OnDesktopExit` → `SaveAll()`.
- `MainWindow.xaml.cs`: aggiunto handler `Closing` → `SaveAll()`. **Critico per macOS**: chiudere la finestra con il pulsante rosso non attiva `desktop.Exit` (l'app rimane nel dock), quindi senza `Window.Closing` il salvataggio non avverrebbe mai su macOS.

**Operazioni file integrate con la persistenza:**
- Nuovo `OpenFileFromPath(string path)` in `MainViewModel`: consolida tutta la logica di apertura — salva breakpoint del file corrente, carica codice/dati, carica breakpoint del nuovo file, aggiunge ai recenti. `Open()` e `OpenRecentFile` lo delegano entrambi.
- `New()` aggiornato: salva breakpoint del file corrente, azzera breakpoint.
- `SaveToPath()` aggiornato: salva breakpoint, aggiunge ai recenti.

**Bug fix `ContainerFor` (pannello Errori duplicato su compilazione):**
- `DockFactory.ContainerFor(IDockable?)` ora percorre **ricorsivamente l'intero albero** `CurrentLayout` (via `FindContainerInTree`) invece di controllare solo i due container fissi. Se il pannello è stato spostato dall'utente in un nuovo `ToolDock` creato da Dock, viene trovato nel posto corretto e `IsPanelVisible` restituisce `true` → `SetPanelVisible(Errors, true)` è un no-op invece di aggiungere un duplicato nel container originale.
- `DockFactory.CurrentLayout` (property interna): aggiornata dal `MainViewModel` dopo ogni assegnazione di `Layout` (costruttore + `LoadLayout`).

### Verifica

- Chiudere/riaprire: layout identico (proporzioni, posizioni pannelli, tab attiva), breakpoint presenti, file recenti aggiornati.
- Spostare il pannello Errori in una nuova posizione → compilare con errori → il pannello Errori è attivato nella sua posizione corrente, non duplicato.
- File recenti: aprire 3 file diversi → tutti e 3 nel menu Recenti, ordinati per ultimo usato.
- Layout `.json` di formato vecchio (o assente): fallback al layout di default senza crash.

### Assunzioni e decisioni (registrate durante l'esecuzione — 2026-06-30)

1. **Serializzazione layout: albero ricorsivo custom invece di `Dock.Serializer.SystemTextJson`**
   `Dock.Serializer.SystemTextJson` richiede: (a) attributi source-gen `[DockJsonSerializable]` su tutti i tipi, (b) costruttori senza parametri su tutti i `Tool`/`Document`, (c) re-iniezione post-deserializzazione di `MainVm` e delle callback `Action`. Per evitare questa complessità si è scelto un walker ricorsivo custom (`ToDockNode`/`RebuildLayout`) che serializza la **struttura** del dock tree (tipi, proporzioni, ID pannelli) e al caricamento sostituisce gli ID con le istanze reali dei ViewModel già costruiti. Nessuna modifica ai ViewModel; nessun costruttore senza parametri richiesto.

2. **`JsonNumberHandling.AllowNamedFloatingPointLiterals` obbligatorio**
   Avalonia Dock usa `double.PositiveInfinity` come valore di proporzione per alcuni nodi (es. `RootDock`). `JsonSerializer.Serialize` di default rifiuta `Infinity` con `ArgumentException`. L'opzione `AllowNamedFloatingPointLiterals` consente di serializzarlo come stringa JSON `"Infinity"` e di deserializzarlo con la stessa opzione. Deve essere usata sia in `SaveLayout` che in `LoadLayout`.

3. **Salvataggio su `Window.Closing`, non solo su `desktop.Exit`**
   Su macOS, premere il pulsante rosso chiude la finestra ma l'applicazione rimane attiva nel dock: `IClassicDesktopStyleApplicationLifetime.Exit` non scatta mai. `Window.Closing` scatta invece in ogni caso (pulsante rosso, Cmd+Q, `ExitCommand`). L'evento `desktop.Exit` è mantenuto come backup per altri scenari (Cmd+Q, `ExitCommand`), ma `Window.Closing` è il punto di salvataggio primario su macOS.

4. **`ContainerFor` deve percorrere l'intero albero**
   Quando l'utente trascina un pannello in una nuova posizione, Dock crea nuovi nodi `ProportionalDock`/`ToolDock` nell'albero — non tracciati da `DocumentContainer`/`ToolContainer`. Cercare solo nei due container fissi fa sì che il pannello risulti "non visibile" → `AddDockable` lo aggiunge al container originale creando un duplicato. La ricerca ricorsiva su `CurrentLayout` trova il pannello ovunque si trovi nell'albero.

5. **File recenti nel `NativeMenu` macOS: inserimento dinamico in codice**
   I `NativeMenuItem` non supportano `ItemsSource` (proprietà non esiste su `NativeMenu`). Il submenu "Recenti" viene creato programmaticamente in `MainWindow.xaml.cs`: `SetupNativeRecentiMenu()` inserisce `Sep` + `NativeMenuItem("Recenti")` nel menu File a indici 4 e 5; `UpdateNativeRecentiMenu()` ripopola il submenu a ogni cambio di `RecentFileItems`. I lambda catturano `path` per evitare il problema `CommandParameter` assente su `NativeMenuItem`.

6. **`ReflectionBinding` per `ItemContainerTheme` del menu Recenti**
   Con `AvaloniaUseCompiledBindingsByDefault=true`, i binding all'interno di `ControlTheme`/`ItemContainerTheme` richiedono `x:DataType` per la compilazione. Poiché `RecentFileItem` è una classe custom non facilmente dichiarabile come DataType in un tema inline, si usa `{ReflectionBinding Header}` e `{ReflectionBinding Command}` — il binding riflessivo funziona correttamente a runtime per questo caso d'uso.

7. **Numeri di riga persistiti (non indici istruzione)**
   I breakpoint sono salvati come numeri di riga 1-based (convenzione AvaloniaEdit), non come indici istruzione. Gli indici cambiano a ogni compilazione; le righe sono stabili. La conversione riga→indice avviene solo in `SyncBreakpointsToCpu()` al momento di passarli alla CPU via `Compiler.LineToInstrMap`.

---

## Motore di debug — dettaglio tecnico (criticità n.1)

Questa è la parte più facile da sbagliare. Riferimento completo in `README.reviewed.md` §2.

**Fatti del simulatore (verificati):**

- Memoria 0–255; area stack 240–255; `SP` iniziale = 256 (`MASSIMO_INDIRIZZO + 1`).
- **Stack discendente**: `push` fa `sp--`, `pop` fa `sp++`, `call`→push, `ret`→pop.
- `ip` viene incrementato **alla fine** di `Execute()`. Dopo uno step, `curIstruzione` è l'istruzione **appena eseguita**; quella **da eseguire** è `code[ip]`.

**Regole corrette:**

- **Step Over**: se `code[ip].Code == "call"`, salva `S = sp`, esegui la call, poi continua **finché `sp < S`** (sei dentro la subroutine); ti fermi quando `sp` torna a `S`. Altrimenti = Step Into.
- **Step Out**: salva `S = sp`, esegui **finché `sp <= S`**; ti fermi quando un `ret` porta `sp` a `S+1`.
- In entrambi i loop: controlla `Breakpoints.Contains(ip)` a ogni ciclo, gestisci `IPOverRun` e il loop infinito come in `Run()`.

**Test di non-regressione del debug (obbligatori):**

```
; step_over_call: SP deve tornare uguale prima/dopo
        mov ax, 0
        call inc_ax      ; <-- Step Over qui: esegue tutta la subroutine
        stop
inc_ax: inc ax
        ret
```

Atteso: dopo lo Step Over sul `call`, `AX==1`, `IP` punta a `stop`, e `SP` è
identico al valore che aveva prima del `call`. Se `SP` è diverso o l'IP è finito
nella subroutine, la logica è invertita.

---

## Aspetti cross-platform da sorvegliare (trasversale)

| Aspetto | Rischio | Mitigazione |
|---|---|---|
| Path con `\\` hardcoded in `Ambiente.cs` | Salvataggi rotti su macOS/Linux | `Path.Combine` / `DirectorySeparatorChar` (Fase 1.6) |
| `using System.Drawing` in `Storage.cs` e `Ambiente.cs` | `System.Drawing.Common` non è cross-platform | Rimuoverli (sono residui dopo la rimozione di `Rectangle`/`Font`) |
| `OpenFileDialog`/WinForms | Non esiste fuori Windows | `IStorageProvider` Avalonia |
| Accesso file su WASM | Sandbox browser | Fallback download/upload, niente path persistenti |
| Font `Courier new` (default in `Ambiente`) | Potrebbe non esistere su tutte le piattaforme | Font monospace di fallback / Inter già inclusa |
| Workload android/ios/wasm | Build `.sln` fallisce se mancano | `.slnf` per lo sviluppo desktop quotidiano |

---

## Segnali che qualcosa non va (red flags)

- Step Over/Out non si fermano o entrano nella subroutine → **verso stack invertito** o si guarda `curIstruzione` invece di `code[ip]`.
- Due esecuzioni "si ricordano" lo stato precedente → campi `static` dimenticati nel core.
- Breakpoint o riga evidenziata sfasati di una riga → confusione 0-based (compiler) vs 1-based (AvaloniaEdit).
- `NU1605` in build → versioni Avalonia non allineate tra head/pacchetti.
- Opzioni non si salvano su Mac/Linux → path Windows non corrette.
- L'editor compare senza colori/stile → `StyleInclude` AvaloniaEdit o `.xshd` non caricati.
- Pannelli "Not Found" → ViewLocator/DataTemplate non registrati.
- `OpzioniWindow` non aggiorna `Ambiente` → binding su campi statici senza wrapper VM.

---

## Matrice di verifica finale (gate per dichiarare "fatto")

| # | Gate | Come verificare |
|---|---|---|
| 1 | Soluzione compila, 0 NU1605 | `dotnet build` |
| 2 | Core testato | `dotnet test` verde (Step Into/Over/Out, breakpoint, mapping) |
| 3 | Desktop avvia e apre/salva `.as` | run manuale |
| 4 | Debug end-to-end | breakpoint + step su un programma con `call`, confronto con `EasyCpu.Win` |
| 5 | Pannelli reattivi | registri/memoria/stack/errori coerenti a ogni step |
| 6 | Persistenza | layout + recenti + breakpoint sopravvivono al riavvio |
| 7 | Cross-platform | opzioni si salvano su macOS/Linux; Desktop + (almeno) Browser girano |
| 8 | Opzioni in MVVM | `SettingsViewModel` osservabile; cambio `FormatoDati` OK/Annulla funziona e persiste |
| 9 | Formato file | salva nel **nuovo** formato di default; apre `.asj` e vecchi `.as` (autodetect); round-trip senza perdite |
| 10 | Breakpoint persistenti | i breakpoint sopravvivono a chiusura/riapertura del file |

---

## Appendice A — Set istruzioni reale (`Parser.SetCode`)

**36 opcode** (numero operandi tra parentesi — il titolo originale diceva 35, ma il sorgente ne ha 36):

`mov`(2) `movs`(0) `add`(2) `sub`(2) `cmp`(2) `and`(2) `or`(2) `xor`(2)
`not`(1) `neg`(1) `mul`(1) `div`(1) `inc`(1) `dec`(1) `push`(1) `pop`(1)
`pushf`(0) `popf`(0) `call`(1) `jcxz`(1) `je`(1) `jg`(1) `jl`(1) `jle`(1)
`jge`(1) `jmp`(1) `jne`(1) `jo`(1) `jno`(1) `js`(1) `jns`(1) `ret`(0)
`nop`(0) `stop`(0) `shl`(2) `shr`(2)

Registri: `ax bx cx dx si di bp sp ip`.
Indirizzamento: diretto, costante, memoria, etichetta, indiretto `[si] [di] [bx] [bp]` (con offset).
Flag: `ZF`(1) `SF`(2) `OF`(4).

## Appendice B — Opzioni utente (`Ambiente`)

| Proprietà | Chiave file `.opt` | Default |
|---|---|---|
| `FormatoDati` | `FORMATO_DATI` | `Dec` |
| `FormatoCarZero` | `FORMATO_CAR_ZERO` | `\0` |
| `MaxNumErrori` | `MAX_ERRORI` | 5 |
| `ColonneStack` | `COLONNE_STACK` | 1 |
| `InizializzaRegistri` | `INIZIALIZZA_REGISTRI` | true |
| `LoopInfinito` | `LOOP_INFINITO` | 65535 |
| `MargineSinistro` | `MARGINE_SINISTRO` | 7 |
| `MostraMemoria` | `MOSTRA_MEMORIA` | true |
| `FontEditorNome/Size/Style/Zoom` | `FONT_EDITOR_*` | Courier new / 14 / 0 / 1.0 |
| `PienoSchermo` | `PIENOSCHERMO` | false |

## Appendice C — Costanti memoria e formato file

- `Ram.MASSIMO_INDIRIZZO = 255`, `Ram.INDIRIZZO_STACK = 240`. SP iniziale = 256.
- File sorgente `.as`: righe di codice, poi marcatore `.DATA`, poi righe dati (gestito da `Storage.Apri/Salva`).
- Commenti `//` (troncati da `PreparaRiga`); costanti char tra apici `'…'`; etichette `nome:`.
- Sidecar breakpoint: `nomefile.as.bkpt` (un numero di riga per riga).

---

## Sequenza riassuntiva

```
Fase 0   Pulizia + Avalonia 12 + svuota demo       → build verde
Fase 1   Core a istanze + breakpoint + path fix    → dotnet test verde
Fase 2   Dock + ViewModel                           → shell dockabile
Fase 2b  Menu IDE + Toolbar + Temi                  → chrome completo, temi funzionanti
Fase 3   Editor + margine + renderer debug          → breakpoint cliccabili, riga evidenziata
Fase 4   Syntax highlighting (.xshd)                → 35 opcode colorati
Fase 5   Pannelli (reg/mem/stack/errori)            → reattivi a ogni step
Fase 6   Dialog + IStorageProvider                  → opzioni/loop/apri-salva
Fase 7   Persistenza (layout/recenti/bkpt)          → tutto sopravvive al riavvio
```
