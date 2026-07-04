# EasyCPU — Piano di Migrazione (Revisione)

> Versione rivista del piano originale (`README.md`), aggiornata dopo verifica
> sul codice sorgente attuale (`VS Solution/`). Sostituisce l'originale dove
> indicato. Le sezioni invariate non vengono ripetute: qui si documentano
> **correzioni, assunti errati e miglioramenti**.
>
> Decisioni concordate:
> 1. `EasyCpu.Win` e `EasyCpu.Win.Controls` vengono **rimossi** dalla soluzione → nessun vincolo di retrocompatibilità sul core.
> 2. I breakpoint usano un **nuovo `HashSet<int>`**; il vecchio meccanismo `Trap`/`SetTrap`/`rigaTrap`/`CpuTrapException` viene rimosso/sostituito.
> 3. Tutti gli head Avalonia vengono portati da **11.3.9 → 12.0.5**.

---

## 0. Stato reale del codice (verificato)

Il piano originale conteneva alcuni assunti non allineati al sorgente. Stato effettivo:

| Assunto nel piano originale | Realtà nel codice | Impatto |
|---|---|---|
| Stack tecnologico già su Avalonia 12 | Tutti gli head (Desktop, Browser, Android, iOS) sono su **Avalonia 11.3.9**; `Directory.Build.props` → `AvaloniaVersion 11.3.8` | Il passaggio a 12 è un **upgrade reale**, non lo stato di partenza |
| `EasyCpu.Core` = "duplicato vuoto, stessi file" | I file **differiscono** da `EasyCpu.Common` (namespace `EasyCpu.Business.Common`, `ArrayList FileRecenti`, `MAXFILERECENTI=4`): è una **copia vecchia**, non un duplicato esatto. Nessun `.csproj` lo referenzia | Eliminabile senza rischi (conclusione del piano corretta, motivazione no) |
| Core retrocompatibile, Win "rimane funzionante" | `EasyCpu.Win` chiama `Cpu.`/`Compiler.`/`Parser.` **staticamente** ovunque e referenzia gli stessi progetti → il refactor a istanze **lo romperebbe** | Risolto rimuovendo Win (decisione 1) |
| `EasyCPU` = scheletro UI da popolare | È attualmente il **progetto demo di AvaloniaEdit** (`ThemeViewModel`, `RegistryOptions`, decine di `Resources/SampleFiles/*`, `MainEditorView`, `MainView`) | Va **svuotato del demo** prima di costruire la IDE |
| Meccanismo breakpoint da creare | Esiste già: `Instruction.Trap`, `Cpu.SetTrap`, `inTrap`, `CpuTrapException`, parametro `rigaTrap` di `CompilaCodice` (un solo trap) | Da rimuovere/sostituire col nuovo `HashSet` |

Pacchetti NuGet citati nel piano: **tutti reali e disponibili** (verificati su nuget.org, giugno 2026):
`Avalonia 12.0.5`, `Avalonia.AvaloniaEdit 12.0.0`, `AvaloniaEdit.TextMate 12.0.0`,
`Dock.Avalonia 12.0.0.2` (+ `Dock.Model.ReactiveUI`, `Dock.Serializer.SystemTextJson`, `Dock.Avalonia.Themes.Fluent` 12.0.0.2). Dock 12 richiede **Avalonia ≥ 12.0.0**: l'upgrade degli head è quindi un prerequisito tecnico, non solo estetico.

---

## 1. Meccanica reale dello stack (base per il debug)

Verificato in `Cpu.cs`. **Lo stack cresce verso il basso**:

- `Init`: `sp = (short)(Ram.MASSIMO_INDIRIZZO + 1)` → SP parte in cima (indirizzo più alto).
- `PushCode`: `sp--` poi scrive → push **decrementa** SP.
- `PopCode`: legge poi `sp++` → pop **incrementa** SP.
- `Call`: `PushCode(ip)` → SP scende.
- `Ret`: `ip = PopCode()` → SP risale.

Inoltre, sull'ordine fetch/execute (rilevante per Step Over):
`Debug()` = `Fetch()` (`curIstruzione = Code[ip]`) + `Execute()` (esegue e fa `ip++` alla fine).
**Dopo uno step, `curIstruzione` è l'istruzione GIÀ eseguita; quella DA eseguire è `Code[ip]`.**

---

## 2. Correzione della logica di debug (sostituisce §5 e §7 dell'originale)

Il piano originale ragionava come se lo stack crescesse verso l'alto: le condizioni
di Step Over/Out erano invertite e lo pseudo-codice `StepOver()` non eseguiva mai la CALL.

### Regole corrette (stack verso il basso)

- **Step Over**: se l'istruzione *da eseguire* (`Code[ip]`) è `call`, salva `S = sp`, esegui la call, poi continua finché `sp < S` (sei ancora dentro la subroutine). Ti fermi quando `sp` torna a `S` (dopo il `ret`). Altrimenti = Step Into.
- **Step Out**: salva `S = sp`, esegui finché `sp <= S`. Ti fermi quando un `ret` porta `sp` a `S+1` (sei risalito di un livello).
- In entrambi i loop, controlla i breakpoint a ogni ciclo e gestisci anche `IPOverRun` e il loop infinito (come fa `Run()`), altrimenti durante step-over/out non scattano né errore IP né `SospendiWindow`.

### Pseudo-codice corretto

```csharp
public class Cpu
{
    // stato d'istanza (prima tutti static)
    private short ax, bx, cx, dx, si, di, bp, sp, ip, flags;
    private bool stop;
    private int loopInfinito, numIstruzioni;
    private Ram memoria;
    private List<Instruction> code;
    private Instruction curIstruzione;

    // NUOVO: breakpoint come indici istruzione (sostituisce Trap/SetTrap/inTrap)
    public HashSet<int> Breakpoints { get; } = new();

    public short IP => ip;
    public short SP => sp;                       // esposto pubblicamente
    public StatoCpu Stato { get; private set; } = StatoCpu.Ferma;
    public bool FlagZero     => TestFlag(ZF);
    public bool FlagSegno    => TestFlag(SF);
    public bool FlagOverflow => TestFlag(OF);
    public bool IPOverRun    => ip > code.Count || ip < 1;

    public void Debug()        // Step Into — invariato nella sostanza
    {
        if (stop) return;
        Stato = StatoCpu.Attiva;
        try { Fetch(); Execute(); }
        catch { Stop(); throw; }
        if (ip == code.Count) Stop();
    }

    public void StepOver()
    {
        if (stop) return;
        // guardare l'istruzione DA eseguire, non curIstruzione (già eseguita)
        if (code[ip].Code == "call")
        {
            short preCallSp = sp;
            Debug();                       // esegue la call: ora sp == preCallSp - 1
            RunWhileInside(preCallSp);     // continua finché sp < preCallSp
        }
        else
        {
            Debug();
        }
    }

    public void StepOut()
    {
        if (stop) return;
        short startSp = sp;
        RunWhileInside((short)(startSp + 1)); // ferma quando sp raggiunge startSp+1
    }

    // esegue finché sp < limite, controllando breakpoint, fine codice, loop infinito
    private void RunWhileInside(short limite)
    {
        Stato = StatoCpu.Attiva;
        while (sp < limite && !stop)
        {
            Fetch();
            if (Breakpoints.Contains(ip))     // breakpoint nella subroutine
                throw new CpuTrapException();
            Execute();
            if (ip == code.Count) { Stop(); break; }
            if (IPOverRun) throw new CpuException(CodiceErrore.IPNonValido);
            if (loopInfinito > 0 && ++numIstruzioni == loopInfinito)
                throw new CpuLoopException();
        }
    }
}
```

> Nota: `RunWhileInside` per `StepOut` al livello top (`sp == MASSIMO+1`) esegue
> fino a fine programma; un eventuale `ret` con stack vuoto solleva
> `StackUnderflow` come già accade. Comportamento accettabile (Step Out al top = Run).

### `Run()` con breakpoint multipli

Il `Run()` attuale controlla `Trap()` tra `Fetch` ed `Execute`. Sostituire quel controllo con:

```csharp
Fetch();
if (Breakpoints.Contains(ip))
    throw new CpuTrapException();   // intercettata dalla UI → entra in modalità debug
Execute();
```

`CpuTrapException`/`CpuLoopException` restano (le cattura la UI). Spariscono
invece `Instruction.Trap`, `Cpu.SetTrap`, `Cpu.inTrap` e il parametro `rigaTrap`
di `Compiler.CompilaCodice`. **Run to Cursor** diventa: aggiungo un breakpoint
temporaneo all'indice della riga del cursore, eseguo `Run()`, lo rimuovo allo stop.

---

## 3. Mappa riga ↔ istruzione (conferma §7)

Verificato in `Compiler.cs`: la lista `debug` (`debug.Add(indRiga)` per ogni
istruzione emessa) finisce in `TabellaDebug`, quindi `TabellaDebug[indiceIstr] = rigaSorgente`.
La rinomina proposta è corretta:

- `InstrToLineMap` = l'attuale `TabellaDebug` (istruzione → riga).
- `LineToInstrMap` = mappa inversa, costruita a fine compilazione, per convertire i breakpoint (numeri di riga) in indici istruzione.

Attenzione: righe vuote/commenti/etichette-su-riga-propria **non** producono
istruzioni (`continue` o `istr == null`), quindi `LineToInstrMap` deve restituire
"nessuna istruzione" (es. `-1`) per quelle righe: un click di breakpoint su una
riga non eseguibile va ignorato o spostato alla successiva istruzione reale.

---

## 4. Fasi di implementazione (sequenza rivista)

### Fase 0 — Pulizia e setup
- [ ] Aggiungere `global.json` (`sdk.version 10.0.101`, `rollForward latestPatch`).
- [ ] **Rimuovere dalla soluzione**: `AvaloniaEdit/` (sorgente), `AvaloniaEdit.TextMate/` (sorgente), `EasyCpu.Core/`, **`EasyCpu.Win`**, **`EasyCpu.Win.Controls`** (decisione concordata). Rimuovere anche le solution folder ormai vuote ("Avalonia Edit", "Frontend Legacy").
- [ ] Aggiornare `Directory.Build.props`: `AvaloniaVersion` `11.3.8 → 12.0.5`.
- [ ] Aggiornare **tutti** gli head a Avalonia 12.0.5 (`EasyCPU`, `.Desktop`, `.Browser`, `.Android`, `.iOS`) e `ReactiveUI.Avalonia` alla versione compatibile con Avalonia 12.
- [ ] In `EasyCPU.csproj`: sostituire i `ProjectReference` ad AvaloniaEdit con i NuGet `Avalonia.AvaloniaEdit 12.0.0` + `AvaloniaEdit.TextMate 12.0.0`; aggiungere i pacchetti `Dock.*` 12.0.0.2.
- [ ] Correggere `EasyCPU.Desktop.csproj`: rimuovere il **`ProjectReference` duplicato** a `EasyCPU` (compare due volte).
- [ ] **Svuotare il demo AvaloniaEdit** da `EasyCPU`: rimuovere `Resources/SampleFiles/*`, `ThemeViewModel`, il `MainWindowViewModel`/`MainEditorView`/`MainView` demo, tenendo solo l'ossatura App/lifetime.
- [ ] *Verifica*: `dotnet build` della sola soluzione frontend + business logic va a buon fine senza i progetti rimossi.

### Fase 1 — Refactoring BusinessLogic (core a istanze)
- [ ] `Parser`: statico → istanziabile (stato `_riga`, `_indcar`).
- [ ] `Compiler`: statico → istanziabile; `TabellaDebug` → `InstrToLineMap`; aggiungere `LineToInstrMap`; **rimuovere** il parametro `rigaTrap` e la logica `messoTrap`/`istr.Trap`.
- [ ] `Cpu`: statico → istanziabile; esporre `SP`; aggiungere `HashSet<int> Breakpoints`, `StepOver()`, `StepOut()`, `RunWhileInside()`; sostituire il controllo `Trap()` con `Breakpoints.Contains(ip)`.
- [ ] `Instruction`: rimuovere il campo `Trap` (non più usato).
- [ ] `Storage`: rimuovere `using System.Drawing` e i metodi `SalvaStatoFinestre`/`LeggiStatoFinestre` (layout delegato a Dock). Il resto è già `System.IO` cross-platform.
- [ ] *Verifica*: unit test sul core — uno per `StepOver` su `call` (rientro corretto a SP iniziale), uno per `StepOut`, uno per breakpoint multipli, uno per `LineToInstrMap` su righe vuote/commenti.

### Fasi 2–7
Invariate rispetto all'originale (Dock + ViewModel, editor con debug, syntax
highlighting `.xshd`, pannelli dockabili, dialog, persistenza/file recenti),
con queste note di allineamento:
- I ViewModel istanziano `Cpu`/`Compiler`/`Parser` (non più chiamate statiche).
- `BreakpointMargin` aggiorna `MainViewModel.Breakpoints` (numeri di riga) → conversione via `LineToInstrMap` → `Cpu.Breakpoints`.
- Persistenza layout: `Dock.Serializer.SystemTextJson` in `layout.json` sotto `Environment.GetFolderPath(SpecialFolder.ApplicationData)/EasyCPU/`.

---

## 5. Riepilogo modifiche rispetto al piano originale

1. **Debug**: condizioni Step Over/Out corrette per stack discendente; `StepOver` ora ispeziona `Code[ip]` ed esegue davvero la CALL; aggiunti controlli IP/loop in `RunWhileInside`.
2. **Win legacy**: rimosso (niente più vincolo di retrocompatibilità, che era comunque irrealizzabile).
3. **Breakpoint**: nuovo `HashSet`, eliminato il vecchio meccanismo `Trap`/`rigaTrap`; "Run to Cursor" = breakpoint temporaneo.
4. **Versioni**: esplicitato l'upgrade 11.3.9 → 12.0.5 su tutti gli head come prerequisito (Dock 12 richiede Avalonia 12).
5. **`EasyCPU`**: aggiunto step esplicito per svuotare il demo AvaloniaEdit prima di costruire la IDE.
6. **Fix minori**: `ProjectReference` duplicato in `.Desktop.csproj`; nota su righe non eseguibili in `LineToInstrMap`; motivazione corretta per la rimozione di `EasyCpu.Core`.
