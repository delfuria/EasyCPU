# Fase 5 – Modalità «x86 fedele»: memoria a byte (P1, opzione B)

Stato: **progetto**, niente ancora implementato. È la fase 5 della roadmap di `PROPOSTE-X86.md`: la più invasiva, da affrontare solo dopo le altre. Il documento descrive il modello, le scelte, l'impatto sul codice e le operazioni da eseguire.

---

## 1. Obiettivo

Oggi EasyCPU ha una memoria di **256 celle da 16 bit**: ogni indirizzo contiene una parola, e un accesso a 8 bit legge o scrive solo il byte basso della cella (opzione A della P1, adottata in fase 1). È semplice, ma non corrisponde a x86:

- `[10]` e `[11]` sono due parole indipendenti, mentre su x86 si sovrappongono;
- un vettore di parole si scorre con `inc si`, su x86 con `add si, 2`;
- non si vede l'ordine dei byte in memoria (little-endian);
- una stringa `DB` occupa una parola per carattere.

La **modalità x86 fedele** introduce una memoria **a byte**, in cui una parola occupa due byte consecutivi in ordine little-endian. È una **modalità opzionale**: la modalità attuale (**a parole**) resta quella predefinita, e tutti i programmi, gli esempi e la documentazione esistenti continuano a funzionare senza modifiche.

### Cosa si insegna in più

- **Endianness**: `mov [10], 1234h` scrive `34h` all'indirizzo 10 e `12h` all'indirizzo 11.
- **Dimensione dei dati**: un vettore di parole avanza di 2 (`add si, 2`), uno di byte di 1; `vet+2` è il secondo elemento di un vettore DW.
- **Tipi degli operandi**: `byte ptr` e `word ptr`, indispensabili quando la dimensione non si deduce dagli operandi (`inc [si]`).
- **Stack reale**: `push`/`pop` spostano SP di 2; in una procedura il primo parametro è a `[bp+4]`.
- **Allineamento e sovrapposizione**: una parola può iniziare a un indirizzo dispari, e due parole a indirizzi consecutivi condividono un byte.

### Cosa resta diverso da x86

La memoria del codice resta separata: IP è il numero d'ordine dell'istruzione, non un indirizzo di byte, e le istruzioni non occupano memoria dati. Non si introducono segmenti. Va dichiarato nel manuale.

---

## 2. Il modello a byte

| Aspetto | Modalità a parole (attuale) | Modalità x86 fedele |
|---|---|---|
| Unità di indirizzamento | cella da 16 bit | byte |
| Dimensione della memoria | 256 celle | **1024 byte** (proposta, vedi *Decisioni aperte*) |
| Parola all'indirizzo *a* | cella *a* | byte *a* (basso) e *a*+1 (alto), little-endian |
| Accesso a 8 bit | byte basso della cella | il byte all'indirizzo |
| Area dello stack | celle 240..255 (16 parole) | byte 896..1023 (**128 byte = 64 parole**, proposta) |
| SP iniziale | 256 | 1024 |
| `push`/`pop`, `call`/`ret` | SP ∓ 1 | SP ∓ 2 |
| `ret n` | SP + n | SP + n (n in byte, come su x86: `ret 4` rimuove due parole) |
| Istruzioni stringa | SI/DI ± 1 | ±1 per le forme B, ±2 per le forme W |
| `DB` | 1 cella per valore | 1 byte per valore |
| `DW` | 1 cella per valore | 2 byte per valore |
| Stringa `'Ciao'` in DB | 4 celle | 4 byte |
| Dimensione di un operando in memoria senza registro né variabile (`inc [si]`) | 16 bit | **errore**: serve `byte ptr` o `word ptr` |

Un accesso a parola che esce dalla memoria (per esempio all'indirizzo 1023) produce l'errore «Violazione dei limiti della memoria», come oggi.

---

## 3. Come si sceglie la modalità

La modalità è una proprietà del **programma**, non dell'IDE: un programma scritto per la memoria a byte (`add si, 2`, `[bp+4]`) dà risultati sbagliati nell'altra modalità, e viceversa. Si propone una **direttiva nella sezione dati**:

```asm
.MEMORIA BYTE        ; modalità x86 fedele
.MEMORIA PAROLE      ; modalità a parole (predefinita, si può omettere)
```

- Va scritta come prima riga significativa della sezione dati (editor Dati), prima di ogni DB/DW/EQU/ORG.
- Se manca, la modalità è **a parole**: i programmi esistenti non cambiano.
- Funziona con entrambi i formati di file (`.as` e `.asj`) senza modificarne la struttura, ed è visibile allo studente.
- Una scelta nelle Opzioni («modalità dei nuovi programmi») può inserire automaticamente la direttiva in File → Nuovo.

L'alternativa, un campo nel file `.asj` (per esempio `"memoria": "byte"`) gestito da un selettore nell'IDE, è discussa nelle *Decisioni aperte*.

La modalità si applica alla compilazione: `Compiler` la riconosce nella sezione dati e la passa alla CPU con il programma compilato. Durante l'esecuzione non cambia.

---

## 4. Sintassi nuova

### 4.1 `byte ptr` e `word ptr`

```asm
inc byte ptr [si]        ; incrementa un byte
mov word ptr [di], 5     ; scrive una parola
cmp byte ptr [bx+2], 'A'
mov al, byte ptr vet+1   ; secondo byte di una variabile DW
```

- Si scrivono prima di un operando in memoria (diretto, indiretto, variabile).
- Impostano la dimensione dell'operando (`Operando.Dimensione` = 8 o 16), con la precedenza sul tipo della variabile.
- Sono ammessi in **entrambe** le modalità: nella modalità a parole `byte ptr [si]` accede al byte basso della cella, come già fanno gli accessi a 8 bit.
- Restano valide le regole attuali: `mov al, word ptr [si]` è un errore `DimensioneOperandi`.

### 4.2 Operandi di dimensione ambigua

Nella modalità a byte, un'istruzione i cui operandi non ne determinano la dimensione è un errore, come in MASM:

| Istruzione | Modalità a parole | Modalità a byte |
|---|---|---|
| `inc [si]` | 16 bit (come oggi) | errore «Dimensione dell'operando non specificata: usare byte ptr o word ptr» |
| `mov [di], 5` | 16 bit | errore |
| `mov [di], ax` | 16 bit | 16 bit (lo determina AX) |
| `inc conta` (`conta DW`) | 16 bit | 16 bit (lo determina la variabile) |
| `push [10]`, `pop [10]` | 16 bit | 16 bit (lo stack lavora solo a parole) |
| `mul [bx]` | 16 bit | errore |

### 4.3 La vecchia forma `indirizzo: valori`

Nella sezione dati la forma originale `10: 4, 2, 3` non ha un tipo. Nella modalità a byte si propone di trattare i valori come **parole** (come `DW`), perché questa è la sua semantica originale, e di scrivere l'indirizzo come indirizzo di byte. Alternativa: vietarla in modalità a byte (vedi *Decisioni aperte*).

---

## 5. Impatto sul codice

### 5.1 Memoria (`EasyCpu.Assembler/Memoria/Ram.cs`)

Oggi `Ram` ha un vettore di 256 `int` con indicizzatore `short this[int]` e costanti statiche `MASSIMO_INDIRIZZO = 255`, `INDIRIZZO_STACK = 240`, usate anche da `Parser`, `Compiler` e dal ViewModel.

Si introduce un'astrazione con due implementazioni:

```csharp
public abstract class Memoria
{
    public abstract int Dimensione { get; }         // 256 celle oppure 1024 byte
    public abstract int InizioStack { get; }        // 240 oppure 896
    public abstract int PassoParola { get; }        // 1 oppure 2: stack, istruzioni stringa W

    public abstract short LeggiParola(int indirizzo);
    public abstract void  ScriviParola(int indirizzo, short valore);
    public abstract short LeggiByte(int indirizzo);   // con estensione del segno
    public abstract void  ScriviByte(int indirizzo, short valore);

    public abstract void Imposta(IReadOnlyList<int> contenuto);   // dati iniziali dalla compilazione
}

public sealed class MemoriaAParole : Memoria { /* comportamento attuale */ }
public sealed class MemoriaAByte   : Memoria { /* byte[], parole little-endian */ }
```

- In `MemoriaAParole`, `LeggiByte`/`ScriviByte` lavorano sul byte basso della cella: è l'attuale `ScriviMemoria` con larghezza 8.
- In `MemoriaAByte`, `LeggiParola(a)` = `b[a] | b[a+1] << 8`; il controllo dei limiti vale per entrambi i byte.
- Le costanti statiche di `Ram` diventano proprietà dell'istanza. Una classe `ModelloMemoria` (enum `Parole`/`Byte`) con i relativi limiti viene usata anche dal compilatore, che non ha un'istanza di `Memoria`.

### 5.2 CPU (`EasyCpu.Assembler/Processore/Cpu.cs`)

| Punto attuale | Modifica |
|---|---|
| `LoadOp`/`StoreOp`/`ScriviMemoria`: `memoria[x]` con `Adatta` | `Larghezza == 8 ? LeggiByte : LeggiParola`; lo stesso per la scrittura |
| `Init`: `sp = MASSIMO_INDIRIZZO + 1`; riceve `List<int>` | riceve il modello o l'istanza di `Memoria`; `sp = Dimensione` |
| `PushCode`/`PopCode`: `sp--`/`sp++`, `INDIRIZZO_STACK` | `sp -= PassoParola` / `+=`, `InizioStack` |
| `Ret` con `n` | limite `Dimensione` invece di `MASSIMO_INDIRIZZO + 1` |
| `Passo` delle istruzioni stringa | `±1` per le forme B; `±PassoParola` per le forme W e `movs` |
| `Movs`, `Lods`, `Stos`, `Cmps`, `Scas` | accessi tramite `LeggiByte`/`LeggiParola` secondo la larghezza |
| `int 21h` 09h (stringa) e 0Ah (buffer) | caratteri letti e scritti con `LeggiByte`/`ScriviByte`: in modalità a byte ogni carattere occupa un byte |
| `LeggiMemoria(int)` (pubblica, usata dai test) | sostituita da `LeggiParola`/`LeggiByte` pubbliche |
| `DumpMemoria`, `DumpSimboli` | vedi 5.5 |

I flag, i registri e le istruzioni aritmetiche e logiche non cambiano: dipendono dalla larghezza dell'operazione, non dal modello di memoria.

### 5.3 Parser e compilatore (`Parsing/`)

| Punto attuale | Modifica |
|---|---|
| Sezione dati: `contatore += valori.Count` | `DB` avanza di 1 per valore, `DW` di `PassoParola`; `Parser.CompilaDati` restituisce i dati come sequenza di unità di memoria (celle o byte) |
| `Simbolo.Celle` | diventa `Dimensione` in unità di memoria; il pannello Simboli la mostra in byte nella modalità a byte |
| `DUP`, stringhe, `?` | invariati nella sintassi; occupazione calcolata come sopra |
| `IndirizzoOk`, `ORG`, controllo dell'area dello stack | limiti del modello invece di `Ram.MASSIMO_INDIRIZZO`/`INDIRIZZO_STACK` |
| `Compiler.CompilaDati` | riconosce `.MEMORIA` in testa ai dati; crea l'immagine della memoria della dimensione giusta; espone la modalità (`Compiler.Modello`) |
| Operandi | nuovi token `byte`, `word`, `ptr` in `LeggiOperando`; `Operando.Dimensione` impostata da `byte ptr`/`word ptr` |
| `Instruction.VerificaIstruzione` | in modalità a byte, errore se la larghezza non è determinata (4.2); la modalità arriva dal parser |
| Nuovi errori (`EasyCpu.Common/Errori.cs`) | `DimensioneNonSpecificata`, `DirettivaMemoriaNonValida` (direttiva dopo altre dichiarazioni o valore sconosciuto) |

### 5.4 IDE (`EasyCPU`)

| Punto attuale | Modifica |
|---|---|
| `DoCompile`: `Cpu.Init(instructions, memory, …)` | passa anche la modalità del programma |
| Reset dei pannelli (riga 329): `new int[Ram.MASSIMO_INDIRIZZO + 1]` | memoria vuota della modalità corrente |
| `RefreshDebugViews`: `DumpMemoria(0, INDIRIZZO_STACK, 8)` e stack | limiti presi dalla memoria della CPU |
| Barra di stato o titolo del pannello Memoria | indicazione della modalità: «Memoria (parole)» / «Memoria (byte)» |
| Evidenziazione (`EasyCPU.xshd`) | nuove parole `byte`, `word`, `ptr`; direttiva `.MEMORIA` |
| File → Nuovo | eventuale direttiva automatica secondo l'opzione dei nuovi programmi |

### 5.5 Pannelli Memoria e Stack

- **Memoria, modalità a byte**: 16 byte per riga, indirizzi a 3 cifre esadecimali (000–3FF); in Hex ogni byte con 2 cifre, in Dec da 0 a 255, in Car il carattere. 1024 byte sono 64 righe, meno di quelle attuali: il pannello resta leggibile.
- **Stack, modalità a byte**: una **parola** per riga (lo stack lavora a parole), con l'indirizzo del byte basso: `03FE: 0005`. La colonna evidenzia la cima (SP).
- **Simboli**: dimensione in byte e valore letto con il tipo della variabile (`vet [0010] DW x5 = 0003`, dove `x5` indica 5 parole cioè 10 byte).
- La modalità a parole mantiene i pannelli attuali.

### 5.6 Test e strumenti

- Gli helper dei test (`BuildCpu`, `Esegui`, `EseguiConConsole`, `Prepara`) creano oggi `new int[256]`: devono usare la memoria prodotta dal compilatore.
- Il runner degli esempi (scratchpad) deve fare lo stesso.

---

## 6. Esempio: lo stesso programma nelle due modalità

Somma di un vettore di parole:

```asm
; modalità a parole                     ; modalità a byte
                                        ; .MEMORIA BYTE   (sezione dati)
vet  DW 3, 8, 1                         vet  DW 3, 8, 1
N    EQU 3                              N    EQU 3

     mov si, 0                               mov si, 0
     mov cx, N                               mov cx, N
     mov ax, 0                               mov ax, 0
ciclo: add ax, [vet+si]                 ciclo: add ax, [vet+si]
     inc si        ; cella successiva        add si, 2     ; parola successiva
     loop ciclo                              loop ciclo
```

Parametri di una procedura:

```asm
; modalità a parole                     ; modalità a byte
somma: push bp                          somma: push bp
       mov bp, sp                              mov bp, sp
       mov ax, [bp+2]  ; ultimo param.         mov ax, [bp+4]  ; ultimo param.
       add ax, [bp+3]                          add ax, [bp+6]
       pop bp                                  pop bp
       ret 2                                   ret 4
```

---

## 7. Operazioni di implementazione

Ogni passo lascia il progetto compilabile e i test verdi. I passi 1–2 sono refactoring **senza cambiamenti di comportamento**, verificati da tutti i test e da tutti gli esempi esistenti.

1. **Astrazione della memoria, solo modalità a parole**
   - `Memoria` astratta e `MemoriaAParole` con il comportamento attuale; `ModelloMemoria` con i limiti.
   - `Cpu` passa da `memoria[x]` a `LeggiParola`/`LeggiByte`/`ScriviParola`/`ScriviByte`; stack e istruzioni stringa usano `PassoParola` (qui 1).
   - `Parser`, `Compiler`, ViewModel usano i limiti del modello invece delle costanti di `Ram`.
   - Verifica: 91 test verdi e 63 esempi con risultati identici.

2. **Allocazione dei dati per unità di memoria**
   - `Simbolo.Celle` → `Dimensione`; `CompilaDati` calcola l'occupazione di DB/DW con `PassoParola`.
   - Verifica come al passo 1.

3. **`byte ptr` / `word ptr`** (entrambe le modalità)
   - Parser, `Operando.Dimensione`, controlli di coerenza.
   - Test nella modalità a parole; esempio `byte-ptr-word-ptr`.

4. **Direttiva `.MEMORIA` e `MemoriaAByte`**
   - Riconoscimento della direttiva ed errore `DirettivaMemoriaNonValida`.
   - `MemoriaAByte` (little-endian, limiti); `PassoParola = 2`.
   - Allocazione dei dati a byte; forma `indirizzo: valori` secondo la decisione presa.
   - Errore `DimensioneNonSpecificata` per gli operandi ambigui.

5. **CPU in modalità a byte**
   - Stack a passi di 2, `ret n`, istruzioni stringa W a passi di 2, `int 21h` 09h/0Ah a byte.
   - Test dedicati (sezione 8).

6. **IDE**
   - Modalità passata a `Cpu.Init`; pannelli Memoria, Stack e Simboli (5.5); indicazione della modalità; evidenziazione; eventuale opzione per File → Nuovo.

7. **Esempi** (nuova cartella, ad esempio `10-memoria-byte`, con gli errori che scalano a `11-errori`)
   - `byte-endianness`: `mov [10], 1234h` e lettura dei singoli byte.
   - `byte-somma-vettore`: `add si, 2` (confronto con l'esempio a parole).
   - `byte-subroutine-parametri`: `[bp+4]`, `ret 4`.
   - `byte-stringhe`: `rep movsb`/`movsw` con passi diversi; Hello world con `int 21h`.
   - `byte-ptr-word-ptr`: le due forme, e gli errori di dimensione ambigua negli esempi di errore.
   - Ogni esempio con l'intestazione standard e verificato eseguendolo.

8. **Documentazione**
   - Assembly Reference: nuovo capitolo «Modalità x86 fedele» (modello, direttiva, `byte ptr`/`word ptr`, differenze nelle istruzioni coinvolte: PUSH, POP, CALL, RET, istruzioni stringa, INT 21h); nota nelle sezioni della memoria e dello stack.
   - README e `PROPOSTE-X86.md` (stato della fase 5).

---

## 8. Test previsti

**Regressione (modalità a parole):** tutti i test e gli esempi attuali invariati; in più test dei passi 1–2 che confrontano `MemoriaAParole` con il comportamento precedente (accessi a 8 bit sul byte basso).

**Modalità a byte:**
- `mov [10], 1234h` → byte 10 = 34h, byte 11 = 12h; `mov ax, [10]` → 1234h; `mov al, [11]` → 12h.
- Allocazione: `a DB 1, 2` + `b DW 3` → `b` all'indirizzo 2, occupa i byte 2 e 3; stringhe e `DUP`; `ORG`; dati nell'area dello stack → errore.
- Stack: `push`/`pop` con SP da 1024 a 1022; `call`/`ret`; `ret 4`; overflow dopo 64 parole; underflow.
- Istruzioni stringa: `movsb` avanza di 1, `movsw` di 2, anche con DF = 1 e REP.
- `int 21h` 09h su una stringa DB; 0Ah con il buffer a byte.
- `byte ptr`/`word ptr`; `inc [si]` → `DimensioneNonSpecificata`; `inc conta` ammesso.
- Accesso a parola all'ultimo byte → `ViolazioneMemoria`.
- Direttiva: assente → modalità a parole; in posizione sbagliata o con valore sconosciuto → errore.

---

## 9. Rischi

- **Ampiezza del refactoring**: `Cpu` accede alla memoria in molti punti (operandi, stack, istruzioni stringa, `int 21h`, dump). Il passo 1, a comportamento invariato e verificato da 91 test e 63 esempi, isola il rischio prima di introdurre la memoria a byte.
- **Costanti statiche di `Ram`**: sono usate da quattro progetti/aree (CPU, parser, compilatore, ViewModel). Vanno sostituite tutte, altrimenti la modalità a byte userebbe limiti sbagliati: una ricerca di `MASSIMO_INDIRIZZO`/`INDIRIZZO_STACK` deve risultare vuota alla fine del passo 1.
- **Confusione dello studente tra le modalità**: mitigata dalla direttiva visibile nel sorgente, dall'indicazione nel pannello Memoria e da esempi separati per modalità.
- **Interazione con altre funzionalità in sospeso**:
  - *salti indiretti* (`SALTI-INDIRETTI.md`): in modalità a byte una tabella `DW` di etichette si indicizza con passo 2 (`shl bx, 1`), cosa che va mostrata negli esempi;
  - *divieto delle operazioni memoria-memoria*: indipendente, ma naturale da attivare almeno nella modalità fedele.

---

## 10. Decisioni aperte

1. **Come si sceglie la modalità**: direttiva `.MEMORIA BYTE` nel sorgente (consigliata: visibile, funziona con `.as` e `.asj`) oppure campo nel file `.asj` con un selettore nell'IDE?
2. **Dimensioni**: memoria di 1024 byte con stack di 128 byte (proposta), oppure 256 byte con stack di 32 byte (come l'attuale per numero di indirizzi, ma con poco spazio per i dati), oppure 4 KB?
3. **Forma `indirizzo: valori` in modalità a byte**: valori come parole (proposta) o come byte, oppure vietata?
4. **Divieto delle operazioni memoria-memoria**: attivarlo automaticamente nella modalità fedele?
5. **Ordine rispetto ai salti indiretti**: implementare prima i salti indiretti (più semplici e utili in entrambe le modalità) e poi la fase 5?
