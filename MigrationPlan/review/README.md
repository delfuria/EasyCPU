# EasyCPU — Piano di Migrazione da WinForms ad Avalonia UI

## Indice

1. [Contesto e obiettivi](#1-contesto-e-obiettivi)
2. [Stato attuale del codice](#2-stato-attuale-del-codice)
3. [Stack tecnologico target](#3-stack-tecnologico-target)
4. [Architettura della nuova soluzione](#4-architettura-della-nuova-soluzione)
5. [Sistema di debug moderno](#5-sistema-di-debug-moderno)
6. [Fasi di implementazione](#6-fasi-di-implementazione)
7. [Dettaglio refactoring del core](#7-dettaglio-refactoring-del-core)
8. [Layout Dock](#8-layout-dock)
9. [Componenti editor custom](#9-componenti-editor-custom)
10. [Pacchetti NuGet](#10-pacchetti-nuget)

---

## 1. Contesto e obiettivi

EasyCPU è un simulatore didattico di un subset di istruzioni x86 in linguaggio assembly. La versione attuale (`EasyCpu.Win`) è implementata con Windows Forms ed è quindi limitata a Windows. L'obiettivo è creare `EasyCPU`: una IDE multipiattaforma (Windows, macOS, Linux, tablet Android/iPadOS ≥ 11", browser WASM) usando **Avalonia UI v12**.

**Requisiti funzionali della nuova IDE:**

- Editor assembly con syntax highlighting per il subset x86 supportato
- Breakpoint cliccabili nel margine dell'editor
- Debug passo-passo: Step Into, Step Over, Step Out, Run to Cursor
- Pannelli dockabili per Registri, Flags, Memoria, Stack
- Layout personalizzabile e persistente (stile Visual Studio / VS Code)
- Gestione file (Nuovo, Apri, Salva, File recenti)
- Dialog opzioni (formato dati Dec/Hex/Car, margine sinistro, font, loop infinito, ecc.)
- Supporto tablet da 11" in su (layout identico al desktop)

---

## 2. Stato attuale del codice

### Progetti da mantenere invariati (o con modifiche minime)

| Progetto | Descrizione | Stato |
|---|---|---|
| `EasyCpu.Common` | Configurazione utente (`Ambiente`), enumerazioni, messaggi di errore | ✅ Riutilizzabile as-is |
| `EasyCpu.Assembler` | CPU, Compiler, Parser, Ram | ⚠️ Refactoring da statico a istanziabile |
| `EasyCpu.Backend` | Storage: I/O file sorgente, opzioni, file recenti | ⚠️ Rimuovere `System.Drawing.Rectangle` |

### Progetti da eliminare dalla soluzione Avalonia

| Progetto | Motivo |
|---|---|
| `AvaloniaEdit/` (sorgente) | Rimpiazzato dal NuGet `Avalonia.AvaloniaEdit` |
| `AvaloniaEdit.TextMate/` (sorgente) | Rimpiazzato dal NuGet `AvaloniaEdit.TextMate` |
| `EasyCpu.Core/` | Duplicato vuoto di `EasyCpu.Common` (stessi file, progetto morto) |

### Progetti WinForms (restano nella soluzione ma non vengono portati)

| Progetto | Note |
|---|---|
| `EasyCpu.Win` | Frontend Windows Forms — rimane funzionante per riferimento |
| `EasyCpu.Win.Controls` | Controlli custom WinForms (`EasyEditor`, `DebugListBox`, `IconaCpu`, ecc.) |

### Problemi da risolvere nel backend

- `Storage.cs` usa `System.Drawing.Rectangle` per la persistenza delle posizioni finestre → il layout finestre viene delegato a `Dock.Serializer.SystemTextJson`; `Storage` si occupa solo di codice sorgente, dati e opzioni utente.
- `Compiler`, `Parser`, `Cpu` usano tutti campi `static` → vanno resi istanziabili (vedi §7).

---

## 3. Stack tecnologico target

### Runtime e SDK

- **.NET SDK**: `10.0.101` (specificato in `global.json`)
- **Target framework**: `net10.0`

### Pacchetti NuGet

| Pacchetto | Versione | Ruolo |
|---|---|---|
| `Avalonia` | 12.0.5 | Framework UI core |
| `Avalonia.Desktop` | 12.0.5 | Target Desktop |
| `Avalonia.Diagnostics` | 12.0.5 | Dev tools (solo Debug) |
| `Avalonia.Fonts.Inter` | 12.0.5 | Font di default |
| `Avalonia.Themes.Fluent` | 12.0.5 | Tema Fluent |
| `Avalonia.AvaloniaEdit` | 12.0.0 | Editor di codice (porta di AvalonEdit) |
| `AvaloniaEdit.TextMate` | 12.0.0 | Integrazione TextMate per syntax highlighting |
| `Dock.Avalonia` | 12.0.0.2 | Sistema di layout dockabile |
| `Dock.Model.ReactiveUI` | 12.0.0.2 | Model Dock con ReactiveUI |
| `Dock.Serializer.SystemTextJson` | 12.0.0.2 | Persistenza layout in JSON |
| `Dock.Avalonia.Themes.Fluent` | 12.0.0.2 | Tema Fluent per Dock |
| `ReactiveUI.Avalonia` | (compatibile Avalonia 12) | MVVM + binding reattivi |

> **Nota sul nome del pacchetto AvaloniaEdit**: il progetto incluso nella soluzione attuale si chiamava `AvaloniaEdit` (sorgente). Il pacchetto NuGet ufficiale per Avalonia 12 si chiama **`Avalonia.AvaloniaEdit`** — nome diverso, stessa libreria.

---

## 4. Architettura della nuova soluzione

```
EasyCPU.sln
│
├── [Solution Folder] BusinessLogic
│   ├── EasyCpu.Common          → settings, enumerazioni (invariato)
│   ├── EasyCpu.Assembler       → Cpu, Compiler, Parser, Ram (refactored)
│   └── EasyCpu.Backend         → Storage I/O (fix Rectangle)
│
├── [Solution Folder] Frontend — Avalonia
│   ├── EasyCPU           → UI condivisa: Views, ViewModels, componenti editor
│   ├── EasyCPU.Desktop   → Entry point Desktop (Win/Mac/Linux)
│   ├── EasyCPU.Browser   → Entry point WASM
│   ├── EasyCPU.Android   → Entry point Android (tablet)
│   └── EasyCPU.iOS       → Entry point iOS/iPadOS (tablet)
│
└── [Solution Folder] Frontend Legacy — WinForms
    ├── EasyCpu.Win             → UI Windows Forms (invariato, solo riferimento)
    └── EasyCpu.Win.Controls    → Controlli custom WinForms (invariato)
```

### Pattern MVVM

Il progetto Avalonia usa **ReactiveUI** con il pattern standard Avalonia:

- **Model**: `EasyCpu.Assembler` (Cpu, Compiler, Ram) + `EasyCpu.Common` (Ambiente)
- **ViewModel**: classi in `EasyCPU/ViewModels/`
- **View**: AXAML + code-behind in `EasyCPU/Views/`

---

## 5. Sistema di debug moderno

Il debug WinForms sostituiva l'editor con una `ListBox` durante l'esecuzione passo-passo. Il nuovo sistema mantiene l'editor sempre visibile e aggiunge layer sovrapposti.

### Comandi di debug

| Comando | Tasto | Descrizione |
|---|---|---|
| **Run / Continue** | F5 | Esegue fino al prossimo breakpoint o alla fine |
| **Step Into** | F11 | Esegue una singola istruzione |
| **Step Over** | F10 | Esegue una singola istruzione; se è `CALL`, esegue l'intera subroutine |
| **Step Out** | Shift+F11 | Esegue fino al `RET` che chiude la subroutine corrente |
| **Run to Cursor** | Ctrl+F10 | Esegue fino alla riga del cursore (breakpoint temporaneo) |
| **Stop** | Shift+F5 | Ferma l'esecuzione e resetta la CPU |

### Implementazione Step Over e Step Out

**Step Over**: se l'istruzione corrente è `CALL`, esegui in loop finché `SP > SP_pre_call` (la return address è stata estratta dallo stack). Altrimenti è identico a Step Into.

**Step Out**: esegui in loop finché `SP > SP_corrente`. Il controllo si basa sul fatto che `RET` incrementa SP.

In entrambi i casi il loop controlla i breakpoint ad ogni ciclo, quindi un breakpoint nella subroutine interrompe l'esecuzione normalmente.

### Breakpoint

I breakpoint sono memorizzati come insiemi di **numeri di riga sorgente** nel ViewModel. Al momento della compilazione viene costruita una mappa inversa `riga_sorgente → indice_istruzione` che permette di convertirli in indici usabili dalla CPU.

La CPU mantiene un `HashSet<int> Breakpoints` (indici istruzione) e lo controlla all'inizio di ogni ciclo `Run()`.

I breakpoint vengono salvati in un file sidecar `nomefile.as.bkpt` (lista di numeri di riga, uno per riga) affiancato al file sorgente.

---

## 6. Fasi di implementazione

### Fase 0 — Pulizia e setup (prerequisito di tutto)

- [ ] Aggiungere `global.json` con `sdk.version: "10.0.101"`
- [ ] Rimuovere dalla soluzione: `AvaloniaEdit/`, `AvaloniaEdit.TextMate/`, `EasyCpu.Core/`
- [ ] Aggiornare `EasyCPU.csproj`: sostituire `ProjectReference` AvaloniaEdit con i NuGet
- [ ] Aggiornare `EasyCPU.Desktop.csproj`: versioni Avalonia 12.0.5
- [ ] Aggiornare `EasyCPU.Browser.csproj`, `.Android`, `.iOS`

### Fase 1 — Refactoring BusinessLogic

- [ ] `Parser`: da statico a istanziabile (stato interno: `riga`, `indcar`)
- [ ] `Compiler`: da statico a istanziabile; aggiungere `LineToInstrIndex` (mappa inversa); rinominare `TabellaDebug` in `InstrToLineMap` per chiarezza
- [ ] `Cpu`: da statico a istanziabile; esporre `SP` pubblicamente; aggiungere `Breakpoints`, `StepOver()`, `StepOut()`
- [ ] `Storage`: rimuovere dipendenza `System.Drawing.Rectangle`; eliminare `SalvaStatoFinestre`/`LeggiStatoFinestre` (delegato a Dock)

### Fase 2 — Setup Dock e struttura ViewModel

- [ ] Creare `DockFactory : Factory` che costruisce il layout iniziale
- [ ] Creare i ViewModel per ogni pannello: `CodeEditorViewModel`, `DataEditorViewModel`, `RegistersViewModel`, `MemoryViewModel`, `StackViewModel`, `ErrorsViewModel`
- [ ] Creare `MainViewModel` con `ReactiveCommand` per tutti i comandi IDE e debug
- [ ] Configurare serializzazione layout con `Dock.Serializer.SystemTextJson`

### Fase 3 — Editor con debug integrato

- [ ] Creare `BreakpointMargin : AbstractMargin` (margine sinistro cliccabile)
- [ ] Creare `DebugCurrentLineRenderer : IBackgroundRenderer` (evidenziazione riga IP)
- [ ] Portare la logica di `EasyEditor` in AvaloniaEdit: gestione Tab (inserisce spazi al margine sinistro configurato), gestione Enter (inserisce spazi del margine), `CaretToLinea()` / `LineaToCaret()`
- [ ] Integrare i due componenti in `CodeEditorView`

### Fase 4 — Syntax highlighting

- [ ] Creare file `.xshd` (formato AvaloniaEdit) per il subset x86 EasyCPU:
  - Istruzioni: `mov`, `add`, `sub`, `mul`, `div`, `cmp`, `jmp`, `je`, `jne`, `jg`, `jge`, `jl`, `jle`, `push`, `pop`, `call`, `ret`, `inc`, `dec`, `and`, `or`, `xor`, `not`, `neg`, `shl`, `shr`, `nop`, `stop`, `movs`, `pushf`, `popf`, `jcxz`, `jo`, `jno`, `js`, `jns`
  - Registri: `ax`, `bx`, `cx`, `dx`, `si`, `di`, `bp`, `sp`
  - Commenti: `//` fino a fine riga
  - Label: token seguito da `:`
  - Costanti numeriche (decimali e hex con suffisso `h`)
- [ ] Registrare la definizione come risorsa embedded e caricarla all'avvio dell'editor

### Fase 5 — Pannelli dockabili

- [ ] `RegistersView`: ListBox con i 9 registri (AX–IP) + display flag ZF/SF/OF
- [ ] `MemoryView`: ListBox con dump memoria (stesso formato di `Cpu.DumpMemoria`)
- [ ] `StackView`: ListBox con dump stack (dal basso verso l'alto)
- [ ] `ErrorsView`: DataGrid con errori di compilazione; doppio click → posiziona il cursore sulla riga/colonna dell'errore
- [ ] Aggiornamento reattivo di tutti i pannelli dopo ogni step/run

### Fase 6 — Dialog

- [ ] `OpzioniWindow`: dialog modale con binding bidirezionale su `Ambiente.*` (MaxNumErrori, FormatoDati, ColonneStack, InizializzaRegistri, LoopInfinito, MargineSinistro, FormatoCarZero)
- [ ] `SospendiWindow`: dialog modale per loop infinito rilevato (3 pulsanti: Pausa / Continua / Arresta)
- [ ] Open/Save file: usare `IStorageProvider` (API Avalonia 12, cross-platform, rimpiazza `OpenFileDialog` WinForms)

### Fase 7 — Persistenza e file recenti

- [ ] Salvare/caricare il layout Dock in JSON (`layout.json` nella cartella dati app)
- [ ] Menu File recenti: costruito dinamicamente da `Ambiente.FileRecenti`
- [ ] Salvataggio opzioni utente: il formato `.opt` esistente è già cross-platform (testo chiave=valore)

---

## 7. Dettaglio refactoring del core

### `Parser` (da statico a istanziabile)

```csharp
// Prima (statico)
public class Parser {
    static string riga;
    static int indcar;
    public static int IndCar { get { return indcar; } }
    public static Instruction Compila(string s, out string etichetta) { ... }
}

// Dopo (istanziabile)
public class Parser {
    private string _riga;
    private int _indcar;
    public int IndCar => _indcar;
    public Instruction Compila(string s, out string etichetta) { ... }
    public List<int> CompilaDati(string s, int indRiga, out int indirizzo) { ... }
}
```

### `Compiler` (da statico a istanziabile)

```csharp
// Prima (statico)
public class Compiler {
    public static List<int> TabellaDebug;  // indice istruzione → riga sorgente
    public static List<Instruction> CompilaCodice(...) { ... }
}

// Dopo (istanziabile)
public class Compiler {
    public List<int> InstrToLineMap { get; private set; }  // indice istruzione → riga sorgente
    public int[] LineToInstrMap { get; private set; }      // riga sorgente → indice istruzione (mappa inversa)

    public List<Instruction> CompilaCodice(List<string> code, ref List<CompilerError> errori) { ... }
    public List<int> CompilaDati(List<string> data, ref List<CompilerError> errori) { ... }
}
```

La mappa inversa `LineToInstrMap` è costruita al termine della compilazione e serve per convertire i breakpoint (numeri di riga) in indici istruzione.

### `Cpu` (da statico a istanziabile)

```csharp
// Dopo (istanziabile)
public class Cpu {
    // stato (prima erano tutti static)
    private short ax, bx, cx, dx, si, di, bp, sp, ip;
    private short flags;
    private bool stop;
    private int inTrap;
    private Ram memoria;
    private List<Instruction> code;
    private Instruction curIstruzione;
    private int loopInfinito;

    // breakpoints: indici nell'array istruzioni
    public HashSet<int> Breakpoints { get; } = new();

    // proprietà pubbliche
    public short IP => ip;
    public short SP => sp;
    public StatoCpu Stato { get; private set; } = StatoCpu.Ferma;
    public bool FlagZero    => TestFlag(ZF);
    public bool FlagSegno   => TestFlag(SF);
    public bool FlagOverflow => TestFlag(OF);
    public bool IPOverRun   => ip > code.Count || ip < 1;

    // metodi esistenti (ora su istanza)
    public void Init(List<Instruction> codice, List<int> memoriaDati, bool initRegs, int loopInfinito) { ... }
    public void Run(int startIP) { ... }
    public void Debug() { }  // Step Into — esegue una singola istruzione

    // nuovi metodi
    public void StepOver() {
        if (curIstruzione?.Code == "call") {
            short targetSP = (short)(sp - 1);
            RunUntilSP((short)(targetSP + 1));
        } else {
            Debug();
        }
    }

    public void StepOut() {
        RunUntilSP((short)(sp + 1));
    }

    private void RunUntilSP(short targetSP) {
        Stato = StatoCpu.Attiva;
        while (sp < targetSP && !stop) {
            Fetch();
            if (Breakpoints.Contains(ip) && ip != inTrap) {
                inTrap = ip;
                throw new CpuTrapException();
            }
            Execute();
            if (ip == code.Count) Stop();
        }
    }

    public void Stop() { stop = true; Stato = StatoCpu.Ferma; }
}
```

### `Storage` (fix cross-platform)

Rimuovere i metodi `SalvaStatoFinestre` e `LeggiStatoFinestre` (delegati a Dock). Rimuovere l'`using System.Drawing`. Il resto (`Salva`, `Apri`, `SalvaOpzioni`, `LeggiOpzioni`, `SalvaFileRecenti`, `ApriFileRecenti`) rimane invariato — usa solo `System.IO` che è cross-platform.

---

## 8. Layout Dock

### Struttura iniziale

```
RootDock
└── ProportionalDock [Horizontal]
    ├── DocumentDock  [proporzione: 70%]
    │   ├── "Code"   → CodeEditorView   (AvaloniaEdit + BreakpointMargin + DebugRenderer)
    │   └── "Data"   → DataEditorView   (AvaloniaEdit, sfondo diverso, no debug)
    └── ProportionalDock [Vertical, 30%]
        ├── ToolDock [50%]
        │   └── "Registri"  → RegistersView
        └── ProportionalDock [Horizontal, 50%]
            ├── "Memoria"   → MemoryView
            └── "Stack"     → StackView

ToolDock [Bottom, collassabile]
└── "Errori"  → ErrorsView  (visibile solo dopo compilazione fallita)
```

### Persistenza

Il layout viene serializzato in `layout.json` nella cartella `%APPDATA%/EasyCPU/` (o equivalente cross-platform via `Environment.GetFolderPath(SpecialFolder.ApplicationData)`). Se il file non esiste, viene usato il layout default sopra definito.

---

## 9. Componenti editor custom

### `BreakpointMargin : AbstractMargin`

Margine cliccabile sul lato sinistro dell'editor (prima del margine numero di riga).

```
[●][ 1]  mov ax, 5
[ ][ 2]  mov bx, 3
[●][ 3]  add ax, bx
```

- `●` rosso pieno = breakpoint attivo
- Click su una riga = toggle breakpoint
- Notifica `MainViewModel.Breakpoints` (HashSet di numeri di riga)
- La riga è trasformata in indice istruzione tramite `Compiler.LineToInstrMap` e inviata a `Cpu.Breakpoints`

### `DebugCurrentLineRenderer : IBackgroundRenderer`

Sovrapposto all'editor durante il debug.

```
[●][ 1]  mov ax, 5
[▶][ 2]  mov bx, 3   ← sfondo giallo, freccia nel margine
[ ][ 3]  add ax, bx
```

- Rettangolo giallo semitrasparente sulla riga corrente (`MainViewModel.CurrentSourceLine`)
- Freccia `▶` disegnata nel margine breakpoint
- Si invalida e ridisegna via `MainViewModel.WhenAnyValue(x => x.CurrentSourceLine)`
- Quando `CurrentSourceLine == -1` (CPU ferma o non avviata) non disegna nulla

### Logica `EasyEditor` portata in AvaloniaEdit

In WinForms `EasyEditor : RichTextBox` gestiva:

- **Tab** → inserisce spazi fino alla prossima colonna multipla di `Ambiente.MargineSinistro` (non tab character)
- **Enter** → inserisce `Ambiente.MargineSinistro` spazi all'inizio della nuova riga
- `LineaToCaret(riga, colonna)` → posiziona il cursore a una specifica riga/colonna
- `CaretToLinea()` → restituisce il numero di riga del cursore
- `Testo` (property) → restituisce le righe filtrando commenti e righe vuote se `MostraSoloCodice`

In AvaloniaEdit questi comportamenti si implementano via:
- `TextArea.TextEntering` event → intercetta Tab e applica la logica spazi
- `TextArea.TextEntered` event → intercetta Enter e inserisce il rientro
- `TextEditor.Document` API → `GetLineByNumber`, `GetOffset`, `CaretOffset` per le conversioni riga↔offset

---

## 10. Pacchetti NuGet

### `EasyCPU.csproj` (progetto UI condiviso)

```xml
<ItemGroup>
  <PackageReference Include="Avalonia" Version="12.0.5" />
  <PackageReference Include="Avalonia.Themes.Fluent" Version="12.0.5" />
  <PackageReference Include="Avalonia.Diagnostics" Version="12.0.5" />
  <PackageReference Include="Avalonia.Fonts.Inter" Version="12.0.5" />
  <PackageReference Include="Avalonia.AvaloniaEdit" Version="12.0.0" />
  <PackageReference Include="AvaloniaEdit.TextMate" Version="12.0.0" />
  <PackageReference Include="Dock.Avalonia" Version="12.0.0.2" />
  <PackageReference Include="Dock.Model.ReactiveUI" Version="12.0.0.2" />
  <PackageReference Include="Dock.Serializer.SystemTextJson" Version="12.0.0.2" />
  <PackageReference Include="Dock.Avalonia.Themes.Fluent" Version="12.0.0.2" />
  <PackageReference Include="ReactiveUI.Avalonia" Version="..." />
</ItemGroup>
```

### `EasyCPU.Desktop.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Avalonia.Desktop" Version="12.0.5" />
</ItemGroup>
```

### `global.json`

```json
{
  "sdk": {
    "version": "10.0.101",
    "rollForward": "latestPatch"
  }
}
```

---

## Note aggiuntive

### Cosa NON viene portato in questa fase

- **Call stack panel** — tecnicamente fattibile (ispezione stack + TabellaDebug), rimandato a una versione successiva
- **Watch panel** — non previsto nel subset x86 supportato
- **Conditional breakpoints** — rimandato, complessità aggiuntiva non necessaria per uso didattico

### Compatibilità WinForms

Il progetto `EasyCpu.Win` rimane nella soluzione e continua a funzionare. Le modifiche al core (`EasyCpu.Assembler`, `EasyCpu.Common`, `EasyCpu.Backend`) sono tutte retrocompatibili: le firme pubbliche cambiano solo aggiungendo parametri con default o mantenendo overload compatibili.
