# Persistenza delle impostazioni nel browser (localStorage)

Stato: **implementato** (27 settembre 2026), con l'opzione A per i file recenti (contenuto dei programmi conservato in `localStorage`). Il documento descrive il progetto seguito per far sì che la versione Browser di EasyCPU ricordi opzioni, layout, file recenti e breakpoint tra una sessione e l'altra, come già fa la versione Desktop.

Differenze rispetto al progetto emerse durante l'implementazione:

- La versione Browser (`MainView`) non aveva un menu dei file recenti: è stata aggiunta la sezione **RECENTI** nel menu laterale, visibile quando l'elenco non è vuoto.
- Il layout era serializzato con la reflection, disattivata nella build WebAssembly: ora usa un contesto generato in compilazione (`LayoutJsonContext`), con lo stesso formato JSON (i `layout.json` esistenti restano validi).
- I breakpoint si salvano a ogni modifica; mentre l'elenco viene svuotato o ricaricato (apertura di un file, Nuovo) il salvataggio è sospeso, per non cancellare quelli già salvati.

---

## 1. Situazione attuale

EasyCPU salva le proprie impostazioni come file, con le API `System.IO` (`File.WriteAllText`, `File.ReadAllText`, `File.Exists`…):

| Dato | Dove (Desktop) | Letto | Scritto | Codice |
|---|---|---|---|---|
| Opzioni (formato dati, font, margine, loop infinito…) | `<ApplicationData>/EasyCPU/opzioni.json` | all'avvio | conferma della finestra Opzioni, cambio del formato dalla toolbar | `Storage.LeggiOpzioni` / `SalvaOpzioni` (`EasyCpu.Backend/Local/Storage.cs`) |
| File recenti | `<ApplicationData>/EasyCPU/recenti.json` | all'avvio | all'uscita (Desktop) | `Storage.ApriFileRecenti` / `SalvaFileRecenti` |
| Layout dei pannelli | `<ApplicationData>/EasyCPU/layout.json` | all'avvio | all'uscita (Desktop) | `MainViewModel.LoadLayout` / `SaveLayout` |
| Breakpoint di un programma | `<percorso del programma>.bkpt` | all'apertura del file | al salvataggio del file, all'apertura di un altro file, all'uscita | `MainViewModel.LoadBreakpoints` / `SaveBreakpoints` |

`<ApplicationData>` è `Environment.SpecialFolder.ApplicationData` (su macOS `~/Library/Application Support`).

### Perché nel browser non funziona

Nella versione Browser (.NET WebAssembly) le API `System.IO` lavorano su un file system **in memoria**, che viene ricreato vuoto a ogni caricamento della pagina. Di conseguenza:

- le opzioni tornano sempre ai valori di `Ambiente.Inizializza()` (formato Dec, font predefinito…);
- il layout dei pannelli torna quello predefinito;
- l'elenco dei file recenti è sempre vuoto; inoltre i file aperti dal browser sono identificati solo dal nome (`GetDisplayPath` restituisce `file.Name`), quindi `OpenRecentFile` non potrebbe comunque riaprirli (`File.Exists` è falso e la voce viene rimossa con «File non trovato»);
- i breakpoint non sopravvivono al ricaricamento;
- `SaveAll()` (layout, breakpoint, recenti) è collegato solo a `desktop.Exit` (`App.OnDesktopExit`), che nel browser non esiste.

---

## 2. Obiettivo

1. Nel browser, opzioni, layout, file recenti e breakpoint vengono conservati tra le sessioni usando `localStorage`.
2. Il comportamento e i file della versione Desktop restano **identici** (stessi nomi, stesso formato JSON): nessuna migrazione.
3. Il codice condiviso (`EasyCPU`, `EasyCpu.Backend`) non dipende dal browser: l'accesso a `localStorage` sta solo nel progetto `EasyCPU.Browser`.
4. Se `localStorage` non è disponibile (navigazione privata con storage bloccato, quota esaurita) l'app continua a funzionare come oggi, senza errori visibili.

Fuori obiettivo: salvataggio automatico del programma in corso di modifica (vedi *Decisioni aperte*), sincronizzazione tra dispositivi, versioni Android/iOS.

---

## 3. Architettura proposta

### 3.1 Un archivio chiave-valore astratto

Nel progetto `EasyCpu.Backend` (namespace `EasyCpu.Backend.Local`) si introduce un'interfaccia minima:

```csharp
// Archivio delle impostazioni: testo associato a una chiave.
public interface IArchivioImpostazioni
{
    string? Leggi(string chiave);            // null se la chiave non esiste
    void Scrivi(string chiave, string testo);
    void Elimina(string chiave);
}
```

Le chiavi sono **logiche**, indipendenti dalla piattaforma:

| Chiave | Contenuto |
|---|---|
| `opzioni` | JSON di `OpzioniDto` (come `opzioni.json`) |
| `recenti` | JSON di `RecentiDto` (come `recenti.json`) |
| `layout` | JSON di `DockNode` (come `layout.json`) |
| `breakpoint:<percorso o nome del programma>` | numeri di riga, uno per riga (come il file `.bkpt`) |
| `programma:<nome>` | solo Browser, se si sceglie l'opzione A per i file recenti (vedi 3.5) |

Tutte le operazioni sono **sincrone**: sia il file system sia `localStorage` lo sono, e i punti di lettura attuali (`App.OnFrameworkInitializationCompleted`, `LoadLayout`) sono sincroni.

### 3.2 Implementazione Desktop: file (comportamento attuale)

`ArchivioFile` (in `EasyCpu.Backend/Local`) traduce le chiavi negli stessi percorsi usati oggi:

| Chiave | Percorso |
|---|---|
| `opzioni` | `Ambiente.OpzioniNomeFile` |
| `recenti` | `Ambiente.RecentiNomeFile` |
| `layout` | `Path.Combine(Ambiente.EasyCPUPath, "layout.json")` |
| `breakpoint:<percorso>` | `<percorso>.bkpt` |

`Scrivi` crea la cartella `EasyCPUPath` se manca (come oggi `SalvaOpzioni` e `SaveLayout`). È l'implementazione predefinita: le versioni Desktop, Android e iOS non cambiano comportamento.

### 3.3 Implementazione Browser: localStorage

Nel progetto `EasyCPU.Browser`:

**`wwwroot/persistenza.js`**, piccolo modulo ES che isola `localStorage` e ne intercetta gli errori:

```javascript
// Tutte le chiavi di EasyCPU hanno un prefisso: su GitHub Pages l'origine
// (utente.github.io) è condivisa con gli altri progetti dello stesso utente.
const PREFISSO = "easycpu:";

export function leggi(chiave) {
    try { return globalThis.localStorage.getItem(PREFISSO + chiave); }
    catch { return null; }
}

export function scrivi(chiave, testo) {
    try { globalThis.localStorage.setItem(PREFISSO + chiave, testo); return true; }
    catch { return false; }   // quota esaurita o storage bloccato
}

export function elimina(chiave) {
    try { globalThis.localStorage.removeItem(PREFISSO + chiave); }
    catch { }
}
```

**`ArchivioLocalStorage.cs`**, con l'interop standard di .NET (`System.Runtime.InteropServices.JavaScript`):

```csharp
[SupportedOSPlatform("browser")]
internal sealed partial class ArchivioLocalStorage : IArchivioImpostazioni
{
    public const string Modulo = "persistenza";

    [JSImport("leggi", Modulo)]
    private static partial string? LeggiJs(string chiave);

    [JSImport("scrivi", Modulo)]
    private static partial bool ScriviJs(string chiave, string testo);

    [JSImport("elimina", Modulo)]
    private static partial void EliminaJs(string chiave);

    public string? Leggi(string chiave) => LeggiJs(chiave);
    public void Scrivi(string chiave, string testo) => ScriviJs(chiave, testo);
    public void Elimina(string chiave) => EliminaJs(chiave);
}
```

Si usa un modulo invece di importare direttamente `globalThis.localStorage.getItem`, perché così la chiamata mantiene il `this` corretto, il prefisso sta in un solo punto e gli errori non arrivano a .NET.

**Registrazione in `Program.cs`**: il modulo va caricato **prima** che l'app legga le impostazioni (`App.OnFrameworkInitializationCompleted`):

```csharp
private static async Task Main(string[] args)
{
    await JSHost.ImportAsync(ArchivioLocalStorage.Modulo, "../persistenza.js");
    Storage.Archivio = new ArchivioLocalStorage();
    await BuildAvaloniaApp()
        .WithInterFont()
        .AfterSetup(...)
        .StartBrowserAppAsync("out");
}
```

Il percorso passato a `JSHost.ImportAsync` è relativo a `_framework/`, dove si trova `dotnet.js`; va verificato sul build pubblicato (vedi *Verifica*).

### 3.4 Punto di accesso unico

`Storage` (Backend) espone l'archivio in uso, con l'implementazione a file come predefinita:

```csharp
public static IArchivioImpostazioni Archivio { get; set; } = new ArchivioFile();
```

Tutto il codice che oggi usa `File.*` per le impostazioni passa da `Storage.Archivio`. Non si usa un contenitore di dependency injection: l'app oggi non ne ha uno e basta questo singolo punto di configurazione.

### 3.5 File recenti nel browser

Nel browser un file aperto non ha un percorso riapribile: il file system reale non è accessibile senza passare dal selettore file. Due possibilità:

- **A. Conservare il contenuto (consigliata).** Quando nel browser si apre o si salva un programma, il suo contenuto (`{ "code": [...], "data": [...] }`, lo stesso formato `.asj`) viene salvato nella chiave `programma:<nome>`. Il menu File recenti riapre il programma da `localStorage`. Quando un nome esce dall'elenco dei recenti (massimo `Ambiente.MAXFILERECENTI`), la sua chiave viene eliminata. I programmi di EasyCPU sono piccoli (pochi KB), quindi il limite di `localStorage` (circa 5 MB per origine) non è un problema.
- **B. Nascondere i file recenti nel browser.** Più semplice, ma lo studente perde il modo più rapido di riprendere l'esercizio.

Con l'opzione A la riapertura di un recente non ha un file associato (`_currentFile = null`): "Salva" deve quindi comportarsi come "Salva con nome", come già avviene per i file nuovi.

### 3.6 Quando salvare nel browser

Nel browser non c'è un evento di uscita dell'applicazione. I dati vengono quindi salvati **nel momento in cui cambiano** e, per il layout, anche quando la pagina viene nascosta o chiusa:

| Dato | Momento del salvataggio |
|---|---|
| Opzioni | già oggi a ogni cambiamento (`SalvaOpzioni`) |
| File recenti | in `AddToRecentFiles`, cioè a ogni apertura o salvataggio |
| Breakpoint | a ogni modifica dell'elenco `Breakpoints` del programma corrente |
| Layout | all'evento `pagehide` della pagina (più `visibilitychange` quando diventa `hidden`, necessario su mobile) |

Per il layout, `persistenza.js` registra i due eventi e chiama un metodo .NET esportato:

```javascript
export function registraSalvataggioAllUscita(salva) {
    globalThis.addEventListener("pagehide", () => salva());
    globalThis.document.addEventListener("visibilitychange", () => {
        if (globalThis.document.visibilityState === "hidden") salva();
    });
}
```

```csharp
[JSExport]
internal static void SalvaTutto() => (Application.Current as App)?.SalvaTutto();
```

`App.SalvaTutto()` è un nuovo metodo pubblico che chiama `_mainViewModel?.SaveAll()`; lo usa anche `OnDesktopExit`. `localStorage.setItem` è sincrono, quindi la scrittura si completa prima della chiusura della pagina.

---

## 4. Operazioni di implementazione

Ogni passo lascia il progetto compilabile e i test verdi.

1. **Interfaccia e archivio a file** (`EasyCpu.Backend/Local`)
   - Nuovi file `IArchivioImpostazioni.cs` e `ArchivioFile.cs` (sezioni 3.1 e 3.2).
   - `Storage`: nuova proprietà `Archivio`; `LeggiOpzioni`, `SalvaOpzioni`, `ApriFileRecenti`, `SalvaFileRecenti` usano `Archivio.Leggi`/`Archivio.Scrivi` con le chiavi `opzioni` e `recenti` al posto di `File.ReadAllText`/`File.WriteAllText`.
   - Verifica: sul Desktop `opzioni.json` e `recenti.json` si leggono e si scrivono come prima.

2. **Layout e breakpoint** (`EasyCPU/ViewModels/MainWindowViewModel.cs`)
   - `SaveLayout`/`LoadLayout` usano la chiave `layout`; `LayoutFilePath` e le chiamate a `File`/`Directory` in quei metodi vengono rimosse.
   - `SaveBreakpoints`/`LoadBreakpoints` usano la chiave `breakpoint:<percorso>`; con zero breakpoint si chiama `Elimina` (oggi `File.Delete`).
   - I `try/catch` esistenti restano.
   - Verifica Desktop: layout e file `.bkpt` identici a prima.

3. **Salvataggio senza evento di uscita**
   - `App`: nuovo metodo pubblico `SalvaTutto()`, usato da `OnDesktopExit`.
   - `MainViewModel`: salvataggio dei recenti in `AddToRecentFiles` (`Storage.SalvaFileRecenti()`); salvataggio dei breakpoint su `Breakpoints.CollectionChanged` quando c'è un programma corrente. Sul Desktop sono scritture in più, innocue.

4. **Archivio localStorage** (`EasyCPU.Browser`)
   - Nuovo `wwwroot/persistenza.js` (sezioni 3.3 e 3.6).
   - Nuovo `ArchivioLocalStorage.cs` e metodo `[JSExport] SalvaTutto`.
   - `Program.Main` diventa `async`: `JSHost.ImportAsync`, impostazione di `Storage.Archivio`, registrazione di `pagehide`/`visibilitychange`, poi avvio di Avalonia.
   - Il `.csproj` non richiede modifiche: `System.Runtime.InteropServices.JavaScript` fa parte di `net10.0-browser` e `AllowUnsafeBlocks` è già attivo.

5. **File recenti nel browser** (se si sceglie l'opzione A)
   - `MainViewModel`: dopo l'apertura o il salvataggio nel browser (`OperatingSystem.IsBrowser()`), scrittura di `programma:<nome>` con il JSON `.asj`.
   - `OpenRecentFile`: nel browser legge `programma:<nome>` e chiama `LoadFromStreamAsync` su uno `MemoryStream`; se la chiave manca, la voce viene rimossa (come oggi per un file inesistente).
   - `Ambiente.AggiungiRecenti` o `RefreshRecentFileItems`: eliminazione delle chiavi `programma:` uscite dall'elenco.

6. **Test** (`EasyCpu.Assembler.Tests`, già riferisce `EasyCpu.Backend`)
   - `ArchivioInMemoria` di test (un `Dictionary`) come `Storage.Archivio`.
   - Andata e ritorno di opzioni e recenti: `SalvaOpzioni` → modifica di `Ambiente` → `LeggiOpzioni` → valori originali.
   - Con archivio vuoto `LeggiOpzioni` lascia i valori di `Ambiente.Inizializza()` (formato Dec).
   - `ArchivioFile`: scrittura e lettura in una cartella temporanea, verifica dei nomi di file prodotti.
   - I test devono ripristinare `Storage.Archivio` (è statico, come `Ambiente`; la parallelizzazione dei test è già disattivata).

7. **Documentazione**
   - README (sezione Browser): le impostazioni sono conservate nel browser; per azzerarle si cancellano i dati del sito.
   - Stato di questo documento aggiornato a «implementato».

---

## 5. Verifica manuale nel browser

1. Pubblicare `EasyCPU.Browser` e servirlo in locale.
2. Primo avvio: formato Dec, layout predefinito.
3. Scegliere Hex dalla toolbar, spostare un pannello, aprire un programma e mettere un breakpoint.
4. In DevTools → Application → Local Storage devono comparire le chiavi `easycpu:opzioni`, `easycpu:layout`, `easycpu:recenti`, `easycpu:breakpoint:<nome>` (e `easycpu:programma:<nome>` con l'opzione A).
5. Ricaricare la pagina: formato Hex, stesso layout, programma nei recenti; riaprendolo, il breakpoint è ancora presente.
6. Cancellare i dati del sito: l'app torna allo stato del primo avvio senza errori.
7. Finestra di navigazione privata: l'app funziona; eventuali errori di `localStorage` non compaiono.
8. Verifica che il percorso `../persistenza.js` passato a `JSHost.ImportAsync` sia corretto nel build pubblicato (sia in locale sia su GitHub Pages, dove l'app sta in una sottocartella).

---

## 6. Decisioni aperte

1. **File recenti nel browser**: opzione A (conservare il contenuto) o B (nascondere il menu)? Consigliata A.
2. **Bozza automatica del programma**: salvare periodicamente in `localStorage` il contenuto degli editor, per ritrovarlo dopo un ricaricamento accidentale della pagina? Utile agli studenti, ma è una funzionalità nuova: può seguire in un secondo momento, riusando lo stesso archivio (chiave `bozza`).
3. **Breakpoint nel browser** identificati dal solo nome del file: due programmi con lo stesso nome in cartelle diverse condividono i breakpoint. Accettabile per l'uso didattico, oppure si può aggiungere alla chiave una firma del contenuto (per esempio la lunghezza del codice).
