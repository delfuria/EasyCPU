# Sistema Interrupt (stile x86) + Pannello Console — Spec di implementazione

Stato: **da implementare**. Documento di riferimento per evitare ambiguità in sessione futura.

## Obiettivo

Aggiungere alla CPU EasyCPU un'istruzione `int` (interrupt) in stile x86, limitata a due soli servizi:
- lettura carattere da tastiera (bloccante)
- scrittura carattere su console

Aggiungere un nuovo pannello dock "Console" (input + output) coerente con i pannelli esistenti (Registri/Stack/Memoria/Errori).

## Decisioni confermate (Q&A con utente, 2026-07-06)

1. **Input tastiera: char-per-char.** Ogni tasto premuto va subito nel buffer di lettura della CPU (niente line-buffering, niente Invio per confermare). Fedele a un vero IRQ tastiera.
2. **Visibilità pannello: sempre in lista Strumenti**, stesso pattern toggle manuale di Registri/Stack/Memoria/Errori (nessuna auto-apertura su `int`).
3. **Output console: si pulisce automaticamente a ogni Run**, come già avviene per Registri/Stack/Memoria (vedi `MainWindowViewModel.cs` righe 658-661).

---

## Parte 1 — CPU: istruzione `int`

### 1.1 Nuovo opcode nel parser

File: `VS Solution/EasyCpu.Assembler/Parsing/Parser.cs`, array `SetCode` (righe 12-50).

Aggiungere una riga, stesso shape di `push`/`pop` (1 operando, `TipoOp.Dati` di default = costante immediata):

```csharp
new OpCode("int", 1),
```

Nessun'altra modifica al parser necessaria: la gestione di un operando costante a 1 argomento è già generica (stesso path di `push nn`).

### 1.2 Nuovo codice errore

File: `VS Solution/EasyCpu.Common/Errori.cs`.

Nell'enum `CodiceErrore` (righe 5-33), aggiungere prima della chiusura:

```csharp
InterruptNonValido,
```

Nel metodo `Errori.Msg` (switch righe 39-65), aggiungere un case:

```csharp
case CodiceErrore.InterruptNonValido: return "Numero di interrupt non valido";
```

### 1.3 Cpu.cs — nuovo case nello switch di Execute()

File: `VS Solution/EasyCpu.Assembler/Processore/Cpu.cs`.

Nello switch di `Execute()` (righe 230-268), aggiungere:

```csharp
case "int": Int(); break;
```

### 1.4 Cpu.cs — nuovi campi e metodi

Da aggiungere nella region `#region istruzioni` (dopo `Stop()`, riga ~521), o in una nuova region `#region interrupt / IO`:

```csharp
// Buffer tastiera thread-safe: riempito dall'host (UI) via InviaCarattereTastiera,
// consumato dalla CPU durante l'esecuzione di "int" con AX=1.
readonly System.Collections.Concurrent.ConcurrentQueue<short> bufferTastiera = new();

// Evento verso l'host: la CPU lo invoca per ogni carattere da stampare su console.
public event Action<char> ScriviSuConsole;

// API pubblica chiamata dall'host quando l'utente preme un tasto nel pannello Console.
public void InviaCarattereTastiera(short carattere) => bufferTastiera.Enqueue(carattere);

void Int()
{
    int numero = LoadOp(1); // numero di interrupt (operando costante)
    switch (numero)
    {
        case 0x21:
            ServizioSistema();
            break;
        default:
            throw new CpuException(CodiceErrore.InterruptNonValido);
    }
}

// Convenzione stile DOS int 21h: funzione selezionata da AX
// (qui AX intero, non AH, perché i registri non sono divisi in byte alto/basso).
//   AX = 1  -> leggi carattere da tastiera (bloccante) CON ECO automatico su console,
//              risultato in AX (fedele a DOS int 21h AH=01h, che fa eco automatica)
//   AX = 2  -> scrivi carattere su console, carattere prelevato da DX
void ServizioSistema()
{
    switch (ax)
    {
        case 1:
            ax = LeggiCarattereBloccante();
            // Eco automatico: il carattere letto va anche in output, non solo in AX.
            // CR (13) viene tradotto in '\n' SOLO ai fini della visualizzazione
            // (il valore in AX resta 13 puro, per non rompere "cmp ax, 13" nei programmi asm).
            ScriviSuConsole?.Invoke(ax == 13 ? '\n' : (char)ax);
            break;
        case 2:
            ScriviSuConsole?.Invoke((char)dx);
            break;
    }
}

// Blocca il thread di Run() finché non arriva un carattere o la CPU viene fermata.
// Il polling su "stop" riusa lo stesso meccanismo di terminazione già presente in Run().
short LeggiCarattereBloccante()
{
    while (!bufferTastiera.TryDequeue(out short c))
    {
        if (stop) return 0;
        System.Threading.Thread.Sleep(1);
    }
    return c;
}
```

**Nota bene:** `Int()` NON incrementa manualmente `ip`: il metodo `Execute()` lo fa già dopo lo switch (riga 269 `ip++;`), stesso comportamento di tutte le altre istruzioni.

**Nota sul blocco:** `Run()` (e `StepInto`/`StepOver`/`StepOut`) girano già sul thread separato usato per breakpoint e step, quindi bloccare quel thread in attesa di un tasto NON blocca la UI. Se l'utente preme "Ferma" mentre la CPU è in attesa di un carattere, il flag `stop` (impostato da `Stop()`) sblocca `LeggiCarattereBloccante` entro ~1ms.

### 1.5 Reset stato tra esecuzioni

`Init()` (riga 203) NON svuota `bufferTastiera` di default: se l'utente ha digitato caratteri "in anticipo" prima di una nuova Run, restano in coda. Decisione da confermare in implementazione: se si vuole comportamento pulito ad ogni Run, aggiungere in `Init()`:

```csharp
bufferTastiera.Clear();
```

(coerente con la scelta già presa di pulire l'output console ad ogni Run — vedi Parte 2.4).

---

## Parte 2 — Pannello Console (Dock.Avalonia)

Pattern di riferimento: `RegistersViewModel.cs` / `RegistersView.axaml` (i pannelli più semplici esistenti).

### 2.1 Nuovo file `ViewModels/ConsoleViewModel.cs`

```csharp
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace EasyCPU.ViewModels;

public partial class ConsoleViewModel : Tool
{
    [ObservableProperty] private string _output = "";

    // Sollevato dalla View quando l'utente preme un tasto col focus sul pannello Console.
    public event Action<char>? CarattereDigitato;

    public void NotificaCarattere(char c) => CarattereDigitato?.Invoke(c);
}
```

### 2.2 Nuovo file `Views/ConsoleView.axaml` (+ `.axaml.cs`)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:EasyCPU.ViewModels"
             x:Class="EasyCPU.Views.ConsoleView"
             x:DataType="vm:ConsoleViewModel"
             Focusable="True"
             KeyDown="OnKeyDown">
    <ScrollViewer>
        <TextBlock Text="{Binding Output}" FontFamily="Courier New,Monospace"
                   Margin="4" TextWrapping="Wrap" />
    </ScrollViewer>
</UserControl>
```

Code-behind `ConsoleView.axaml.cs` — cattura `KeyDown`, converte `e.Key`/testo in carattere, notifica il ViewModel (analogo a come `CodeEditorView`/altri già gestiscono input; verificare pattern esatto di conversione `Key -> char` più adatto, es. via `TextInput` event invece di `KeyDown` per gestire correttamente lettere maiuscole/minuscole e caratteri speciali — **da verificare in implementazione**, probabilmente meglio agganciarsi a `TextInputEvent` di Avalonia invece che `KeyDownEvent` per ottenere direttamente il carattere già risolto da tastiera/layout):

```csharp
using Avalonia.Controls;
using Avalonia.Input;

namespace EasyCPU.Views;

public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();
        AddHandler(TextInputEvent, OnTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (DataContext is not ViewModels.ConsoleViewModel vm || e.Text is null) return;
        foreach (var c in e.Text)
            vm.NotificaCarattere(c);
    }
}
```

(Rimuovere l'handler `KeyDown="OnKeyDown"` nello XAML sopra se si adotta `TextInputEvent` in code-behind: sono alternativi, scegliere in implementazione quale gestisce meglio i caratteri su mobile/browser dove la tastiera software potrebbe comportarsi diversamente.)

**Punto aperto per l'implementazione:** su Android/iOS/Browser serve verificare che il pannello, quando ha il focus, apra la tastiera virtuale. Se `Focusable="True"` su `UserControl` non basta, potrebbe servire un elemento nativamente focusable (es. `TextBox` trasparente/nascosto usato solo per catturare input, con `Output` mostrato in overlay sopra). Questo è un rischio noto da validare su dispositivo/emulatore reale prima di considerare la feature completa sulle piattaforme mobile.

### 2.3 `DockFactory.cs` — registrazione pannello

```csharp
public ConsoleViewModel? Console { get; private set; }
```

In `CreateLayout()` (dopo riga 190, insieme agli altri Tool):

```csharp
Console = new ConsoleViewModel { Id = "Console", Title = "Console" };
```

Nel `toolDock` (righe 223-230), aggiungerlo alla lista visibile insieme a Registri/Stack/Memoria:

```csharp
VisibleDockables = CreateList<IDockable>(Registers, Stack, Memory, Console),
```

Nessuna modifica necessaria a `ContainerFor`/`RebuildNode`: essendo un `Tool` come gli altri, ricade nel default `ToolContainer` (vedi `ContainerFor`, riga 44 `return ToolContainer;`, e `RebuildNode` case `ToolNode`, righe 154-164 — usa `ToolContainer` di default perché l'id `"Console"` non è `"Errors"`).

### 2.4 `MainWindowViewModel.cs` — proprietà, toggle, wiring

**Proprietà visibilità** (dopo `IsErrorsVisible`, riga 210):

```csharp
public bool IsConsoleVisible
{
    get => _factory.IsPanelVisible(_factory.Console);
    set => SetPanelVisible(_factory.Console, value);
}
```

**Notifica cambio pannelli**, in `OnPanelVisibilityChanged()` (riga 149-157), aggiungere:

```csharp
OnPropertyChanged(nameof(IsConsoleVisible));
```

**Comando toggle**, accanto alle righe 918-923:

```csharp
[RelayCommand] private void ToggleConsole() => IsConsoleVisible = !IsConsoleVisible;
```

**Dizionario layout persistente** (per save/restore layout.json), riga ~545-553:

```csharp
["Console"] = _factory.Console,
```

**Pulizia output ad ogni Run** — stesso blocco che già pulisce Registri/Stack/Memoria (riga ~658-661):

```csharp
if (_factory.Console is { } cv) cv.Output = "";
```

**Wiring Cpu <-> pannello Console.** Va individuato il punto in cui `MainWindowViewModel` (o `MainViewModel`, verificare nome classe esatto — nota: file si chiama `MainWindowViewModel.cs` ma la classe al suo interno potrebbe chiamarsi `MainViewModel`, da confermare in apertura file) crea/possiede l'istanza di `Cpu` usata per Run/Step (verosimilmente in un `Compiler`/campo `_cpu`). In quel punto, una sola volta (es. dopo la creazione della CPU o nel costruttore), collegare:

```csharp
_cpu.ScriviSuConsole += c =>
{
    if (_factory.Console is { } cv)
        cv.Output += c;
};

if (_factory.Console is { } console)
    console.CarattereDigitato += c => _cpu.InviaCarattereTastiera((short)c);
```

**Attenzione thread:** `ScriviSuConsole` viene invocato dal thread di Run() (non UI thread). Se Avalonia richiede che le modifiche a proprietà bindate avvengano sul UI thread, avvolgere l'assegnazione con `Dispatcher.UIThread.Post(...)` o equivalente. **Da verificare in implementazione** controllando come gli altri pannelli (es. aggiornamento `Registers.Dump` durante Run) gestiscono già questo problema — se già lo fanno correttamente, replicare lo stesso meccanismo per coerenza.

### 2.5 Voci menu/drawer — 3 punti da aggiornare

**a) `Views/MainView.axaml`** (drawer mobile/browser/desktop), dopo il blocco Errori (righe 205-209):

```xml
<CheckBox Classes="drawer-item" Command="{Binding ToggleConsoleCommand}" IsChecked="{ReflectionBinding IsConsoleVisible, Mode=OneWay}">
    <StackPanel Orientation="Horizontal" Spacing="12">
        <icons:ThemedIcon Source="/images/toolbar/panel_console.png" Width="20" Height="20"/>
        <TextBlock Text="Console" VerticalAlignment="Center"/>
    </StackPanel>
</CheckBox>
```

**b) `Views/MainWindow.xaml`** — menu nativo macOS, dentro `NativeMenuItem Header="Finestre"` (righe 63-76), dopo riga 71:

```xml
<NativeMenuItem Header="Console" Command="{Binding ToggleConsoleCommand}"/>
```

**c) `Views/MainWindow.xaml`** — menu Windows/Linux, dentro `MenuItem Header="Fi_nestre"` (righe 177-199), dopo riga 195:

```xml
<MenuItem Header="_Console" ToggleType="CheckBox" Command="{Binding ToggleConsoleCommand}" IsChecked="{ReflectionBinding IsConsoleVisible, Mode=OneWay}">
    <MenuItem.Icon><icons:ThemedIcon Source="/images/toolbar/panel_console.png"/></MenuItem.Icon>
</MenuItem>
```

**Icona mancante:** `panel_console.png` non esiste nelle risorse toolbar attuali (viste finora solo `panel_code/data/registers/stack/memory/errors.png`). Serve crearla (stesso stile monocromatico delle altre, usate con `ThemedIcon`/`OpacityMask`) oppure, come soluzione temporanea, riusare un'icona esistente o omettere `MenuItem.Icon`/`icons:ThemedIcon` per la voce Console finché l'asset non è pronto.

---

## Parte 3 — Checklist file da toccare

| File | Tipo modifica |
|---|---|
| `EasyCpu.Assembler/Parsing/Parser.cs` | +1 riga in `SetCode` |
| `EasyCpu.Common/Errori.cs` | +1 valore enum, +1 case switch |
| `EasyCpu.Assembler/Processore/Cpu.cs` | +1 case Execute, +2 campi, +4 metodi, (opz.) clear buffer in `Init()` |
| `EasyCPU/ViewModels/ConsoleViewModel.cs` | **nuovo file** |
| `EasyCPU/Views/ConsoleView.axaml` + `.axaml.cs` | **nuovo file** (x2) |
| `EasyCPU/DockFactory.cs` | +1 proprietà, +1 riga CreateLayout, +1 modifica VisibleDockables |
| `EasyCPU/ViewModels/MainWindowViewModel.cs` | +1 proprietà, +1 notify, +1 RelayCommand, +1 riga dizionario layout, +1 riga clear-on-run, wiring evento Cpu (posizione esatta da individuare) |
| `EasyCPU/Views/MainView.axaml` | +1 blocco CheckBox drawer |
| `EasyCPU/Views/MainWindow.xaml` | +1 riga NativeMenuItem, +1 blocco MenuItem |
| asset icona `panel_console.png` | da creare o omettere temporaneamente |

## Parte 4 — Verifica dopo implementazione

1. Build pulita su tutte le piattaforme toccate (almeno Desktop; idealmente Android/iOS/Browser per via del pannello UI condiviso).
2. Programma asm di prova (base):
   ```
   mov ax, 2
   mov dx, 65      ; 'A'
   int 21h         ; deve stampare 'A' in console

   mov ax, 1
   int 21h         ; deve aspettare un tasto, risultato in ax
   ```
   **Nota:** il numero di interrupt va scritto in esadecimale (`21h`), non `21` decimale — il case C# in `Int()` è `0x21` (=33 decimale). Il parser EasyCPU riconosce l'esadecimale solo col suffisso `h` (vedi `Parser.StringToInt`). Usare `int 21` (decimale) genera `CodiceErrore.InterruptNonValido` a runtime.

3. Verificare che "Ferma" durante attesa tastiera sblocchi la CPU entro tempi brevi (no deadlock).
4. Verificare toggle pannello Console da drawer (mobile/browser/desktop) e da entrambi i menu desktop (macOS nativo + Windows/Linux).
5. Verificare che l'output si azzeri ad ogni nuova Run.
6. Verificare persistenza layout (`layout.json`): chiudere/riaprire pannello Console, riavviare app, controllare che lo stato sia ripristinato correttamente (non deve rompere il caricamento layout esistente se il file salvato precedentemente non conteneva l'id `"Console"`).

7. **Programma asm di prova (input riga fino a Invio + memorizzazione + rivisualizzazione).** Test più completo: chiede il nome, lo scrive in memoria a partire dalla locazione 50 carattere per carattere finché non arriva Invio (codice 13, CR), poi lo rilegge e lo ristampa:

   ```
   mov bx, 50          ; bx = puntatore di scrittura, inizio buffer nome

   leggi:
       mov ax, 1       ; funzione: leggi carattere (bloccante)
       int 21h
       cmp ax, 13      ; 13 = Invio (CR)
       je fine_lettura
       mov [bx], ax
       inc bx
       jmp leggi

   fine_lettura:
       mov [bx], 0     ; terminatore stringa (byte 0)
       mov bx, 50      ; bx = puntatore di lettura, torna all'inizio

   stampa:
       mov dx, [bx]
       cmp dx, 0
       je fine
       mov ax, 2       ; funzione: scrivi carattere
       int 21h
       inc bx
       jmp stampa

   fine:
       stop
   ```

   Punti da verificare con questo test:
   - i caratteri digitati finiscono correttamente in memoria da 50 in poi (controllabile aprendo il pannello Memoria mentre il programma è in pausa/dopo `fine_lettura`, es. con un breakpoint su `mov bx, 50` della fase stampa);
   - il tasto Invio termina la lettura senza essere scritto in memoria né stampato;
   - la stringa ristampata coincide esattamente con quella digitata;
   - **echo automatico**: col servizio AX=1 aggiornato in Parte 1.4, ogni carattere letto viene anche inviato a `ScriviSuConsole` — l'utente vede quello che digita apparire nel pannello Console in tempo reale (fedele a DOS `int 21h AH=01h`). Il carattere Invio (13) viene tradotto in `'\n'` solo per la visualizzazione (l'eco va a capo), mentre in `AX` resta il valore puro 13 usato dal confronto `cmp ax, 13` per terminare il ciclo.
   - **Attenzione doppio output:** in questo programma di test, dopo la lettura (con eco) il nome viene anche ristampato esplicitamente nel loop `stampa` (AX=2). Risultato atteso in console: il nome appare mentre lo si digita (eco), poi Invio va a capo, poi il nome appare una seconda volta (ristampa) subito sotto. Comportamento corretto e atteso, non un bug — utile da annotare per chi verifica il test per non pensare a un carattere duplicato per errore.
   - **Prerequisito da correggere in `ConsoleView` (Parte 2.2):** il tasto Invio (Enter) non genera testo tramite `TextInputEvent` in Avalonia (l'evento scatta solo per caratteri stampabili). Serve quindi un handler aggiuntivo su `KeyDown` che intercetti specificamente `Key.Enter`/`Key.Return` e invii esplicitamente il carattere `13` a `vm.NotificaCarattere('\r')` (o direttamente a `Cpu.InviaCarattereTastiera`), altrimenti il programma di test sopra non riceverà mai il terminatore e resterà bloccato in lettura per sempre.
