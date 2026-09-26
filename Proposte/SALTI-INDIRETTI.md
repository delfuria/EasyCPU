# Salti e chiamate indirette (jmp/call con registro o memoria)

Stato: **progetto**, niente ancora implementato. È la proposta A di `PROPOSTE-X86.md` (P4 dell'analisi iniziale), rimandata dalla fase 3. Il documento descrive cosa introdurre, perché oggi non è possibile e le operazioni da eseguire.

---

## 1. Cosa si vuole ottenere

Oggi `jmp` e `call` hanno solo la forma **diretta**: la destinazione è un'etichetta, fissata in compilazione.

```asm
jmp fine
call somma
```

Con i salti **indiretti** la destinazione viene letta durante l'esecuzione da un registro o da una cella di memoria:

```asm
jmp ax              ; salta all'istruzione il cui indirizzo è in AX
jmp [tab+bx]        ; salta all'indirizzo contenuto nella cella tab+BX
call [procedure+si] ; chiama la procedura il cui indirizzo è in procedure+SI
```

Per sfruttarli servono anche le **etichette usate come valori**, per costruire le tabelle:

```asm
; sezione dati
tab     DW caso0, caso1, caso2      ; tabella di salto: indirizzi di tre punti del codice

; codice
        mov bx, offset caso2        ; indirizzo di un'etichetta in un registro
        jmp bx
```

### Valore didattico

- **`switch`/`case` in assembly**: invece di una catena di `cmp`/`je`, un solo salto attraverso una tabella indicizzata dalla scelta.
- **Puntatori a funzione e tabelle di dispatch**: `call [procedure+si]` sceglie la procedura durante l'esecuzione (menu, calcolatrice, macchina a stati).
- Mostra che anche il codice ha indirizzi, trattabili come dati.

### Esempio completo di riferimento

```asm
; sezione dati
tab     DW caso0, caso1, caso2
msg0    DB 'zero$'
msg1    DB 'uno$'
msg2    DB 'due$'

; codice
        mov ah, 1
        int 21h             ; AL = tasto premuto ('0', '1' o '2')
        sub al, '0'         ; AL = 0, 1 o 2
        mov bh, 0
        mov bl, al
        jmp [tab+bx]        ; una cella per elemento: l'indice è la scelta
caso0:  mov dx, offset msg0
        jmp stampa
caso1:  mov dx, offset msg1
        jmp stampa
caso2:  mov dx, offset msg2
stampa: mov ah, 9
        int 21h
        mov ah, 4Ch
        int 21h
```

---

## 2. Il modello di EasyCPU: indirizzi di codice

In EasyCPU codice e dati stanno in **memorie separate**: le istruzioni sono in un vettore a parte (`List<Instruction>`) e IP è il **numero d'ordine dell'istruzione** (0, 1, 2…), non un indirizzo di memoria in byte come su x86.

Il valore di un'etichetta è quindi il numero dell'istruzione a cui si riferisce. Una tabella di salto contiene numeri di istruzione, e il pannello Memoria mostra quei numeri (ad esempio 7, 9, 11). Il concetto è identico a x86, ma i valori sono diversi: va spiegato nel manuale, come le altre differenze del modello (celle da 16 bit, stack di 16 celle).

---

## 3. Situazione attuale nel codice

### Esecuzione: già quasi pronta

- `Cpu.NuovoIp()` calcola la destinazione come `LoadOp(1) - 1` (l'istruzione successiva viene raggiunta dall'`ip++` di `Execute`). `LoadOp` sa già leggere registri, memoria diretta, indiretta e base+indice: `jmp ax` o `jmp [tab+bx]` funzionerebbero senza modifiche a `Jmp()`.
- `Call()` salva `ip` sullo stack e poi usa `NuovoIp()`; `Ret()` ripristina `ip`. Il ritorno da una `call` indiretta funziona quindi come da una diretta.
- Un salto verso un numero di istruzione inesistente è già segnalato: `IPOverRun` produce l'errore di esecuzione «Registro IP non indirizza un'istruzione» sia con Avvia sia con Step. Saltare esattamente dopo l'ultima istruzione termina il programma, come quando l'esecuzione arriva in fondo.
- `StepOver` riconosce una chiamata da `Code == "call"`: vale anche per `call [..]`.

### Compilazione: qui stanno gli ostacoli

1. **`jmp`/`call` accettano solo un nome.** In `Parser.Compila`, le istruzioni con `TipoOp.Codice` (salti, `call`, `loop`, `jcxz`) leggono un solo token e creano `new Instruction(code, salto)`; il nome viene risolto alla fine di `Compiler.CompilaCodice` (`CercaEtichetta`).
2. **Le etichette non sono utilizzabili come valori.** Un nome in un operando viene cercato solo fra i simboli della sezione dati (`Parser.CercaSimbolo`).
3. **Ordine di compilazione.** Dalla fase 2 la sezione dati è compilata **prima** del codice, perché il codice usa i nomi delle variabili. Mentre si compila `tab DW caso0` le etichette del codice non sono ancora note.
4. **Riferimenti in avanti.** `mov bx, offset fine` con `fine:` più in basso richiede di conoscere un'etichetta non ancora incontrata; oggi solo i salti hanno la risoluzione posticipata.

---

## 4. Progetto

### 4.1 Pre-scansione delle etichette

Prima di compilare i dati, `Compiler` esegue una **pre-scansione** del codice che raccoglie tutte le etichette con il numero dell'istruzione a cui puntano.

Il conteggio è semplice e affidabile: dopo `PreparaRiga`, ogni riga non vuota produce **al massimo un'istruzione**. Se la riga è formata dalla sola etichetta (`fine:`), l'etichetta punta all'istruzione della riga successiva. È la stessa regola di `CompilaCodice` (`indiceEtichetta = istruzioni.Count` per una riga con la sola etichetta).

Se il codice contiene errori, i numeri della pre-scansione possono differire da quelli definitivi. Non è un problema: in quel caso la compilazione fallisce e il programma non viene eseguito.

Il riconoscimento dell'etichetta a inizio riga va fatto con la stessa logica di `Parser.Compila` (identificatore seguito da `:`), da estrarre in un metodo del parser riusato in entrambi i punti.

Nuovo ordine di `DoCompile` (`MainWindowViewModel`) e dei test:

1. pre-scansione delle etichette del codice;
2. compilazione dei dati (può usare le etichette);
3. compilazione del codice (può usare dati ed etichette, anche in avanti).

La pre-scansione può stare dentro `Compiler.CompilaDati` (ricevendo anche le righe del codice) oppure in un metodo pubblico separato chiamato prima; la seconda soluzione lascia invariata la firma di `CompilaDati`.

### 4.2 Etichette come simboli

Le etichette entrano nella tabella dei simboli del parser con un nuovo tipo:

```csharp
public enum TipoSimbolo { Equ, Db, Dw, Etichetta }   // Etichetta: Valore = numero dell'istruzione
```

- Il controllo «etichetta con lo stesso nome di una variabile» oggi è in `CompilaCodice`; con le etichette in tabella diventa un normale `SimboloDuplicato`, segnalato sulla riga che lo causa.
- `CercaEtichetta` può usare la stessa tabella.

### 4.3 Etichette nelle espressioni

| Contesto | Scrittura | Significato |
|---|---|---|
| Sezione dati | `tab DW caso0, caso1` | numero dell'istruzione (come oggi `DW vet` è l'indirizzo della variabile) |
| Codice | `mov bx, offset caso0` | numero dell'istruzione, come costante |
| Codice | `mov bx, caso0` (senza `offset`) | **errore** con un messaggio che suggerisce `offset` (vedi *Decisioni aperte*) |

In `Parser.LeggiTermine` un nome di tipo `Etichetta` è ammesso dopo `offset` o nella sezione dati; negli altri casi produce l'errore.

### 4.4 Operando di `jmp` e `call`

Solo `jmp` e `call` ottengono la forma indiretta. I salti condizionati, `loop` e `jcxz` restano con la sola etichetta, come su x86.

In `Parser.Compila`, per `jmp`/`call`:

- se il token successivo è un'**etichetta** → forma diretta (come oggi);
- se è un **registro**, `[` o una **variabile DW** → si legge un operando normale con `LeggiOperando()`:
  - `jmp bx`, `call ax`
  - `jmp [bx]`, `jmp [tab+bx]`, `jmp [tab+bx+si]`
  - `jmp tab` (stile MASM: il nome di una variabile indica il suo contenuto, come `mov ax, conta`)
- se il nome non è noto → errore `EtichettaNonValida` (come oggi).

### 4.5 Controlli in compilazione (`Instruction.VerificaIstruzione`)

| Caso | Esito |
|---|---|
| `jmp al`, `jmp b` con `b` di tipo DB | `DimensioneOperandi`: l'indirizzo è a 16 bit |
| `jmp 5`, `jmp offset caso0` | `OperandoNonValido`: per un salto diretto si scrive l'etichetta |
| `call [tab+bx]`, `jmp si` | ammessi |

### 4.6 Esecuzione (`Cpu`)

Nessuna modifica necessaria a `Jmp`, `Call`, `Ret`: `NuovoIp()` legge già qualsiasi operando.

Resta da verificare con i test che `LoadOp` su un operando di memoria senza variabile (`jmp [bx]`) usi la larghezza 16. Oggi è così, perché la larghezza di un'istruzione senza registri a 8 bit vale 16.

### 4.7 Interfaccia

- **Pannello Simboli** (sotto il pannello Memoria): mostrare anche le etichette, ad esempio `caso0  ETICHETTA 7`, per leggere le tabelle di salto nella memoria.
- **Evidenziazione**: nessuna modifica (`jmp`, `call`, `offset` sono già evidenziati).

---

## 5. Operazioni di implementazione

Ogni passo lascia il progetto compilabile e i test verdi.

1. **Pre-scansione** (`Parser`, `Compiler`)
   - Estrazione del riconoscimento dell'etichetta a inizio riga in un metodo del parser, usato da `Compila` e dalla pre-scansione.
   - Nuovo `TipoSimbolo.Etichetta`; la pre-scansione definisce le etichette nella tabella dei simboli.
   - `CompilaCodice` e `CercaEtichetta` usano la tabella; il controllo sui nomi duplicati passa da `DefinisciSimbolo`.
   - Test: indici corretti con etichette su riga propria, etichette seguite da istruzione, commenti, righe vuote, prefissi `rep`. I 91 test e i 63 esempi esistenti devono restare invariati.

2. **Ordine di compilazione**
   - `DoCompile` (`MainWindowViewModel`), gli helper di test e il runner degli esempi: pre-scansione, poi dati, poi codice.
   - Test: `tab DW caso0` risolto con un'etichetta definita più in basso nel codice.

3. **Etichette nelle espressioni** (`Parser.LeggiTermine`, `CompilaDati`)
   - `offset etichetta` nel codice ed `etichetta` negli elementi DW; errore per un'etichetta senza `offset` nel codice.
   - Nuovo codice d'errore (ad esempio `EtichettaSenzaOffset`) con messaggio esplicativo, in `EasyCpu.Common/Errori.cs`.

4. **Operando di `jmp`/`call`** (`Parser.Compila`, `Instruction`)
   - Riconoscimento della forma indiretta e controlli della sezione 4.5.
   - `Instruction.ToString` / `OpToString` per la forma indiretta.

5. **Test di esecuzione** (nuovo `SaltiIndirettiTests`)
   - `jmp reg`, `jmp [tab+bx]`, `jmp tab`, `call [procedure+si]` con ritorno corretto e SP ripristinato.
   - Tabella con destinazione fuori dal programma → `IPNonValido`.
   - StepOver su `call [..]` esegue l'intera procedura.
   - Errori di compilazione della sezione 4.5 e dell'etichetta senza `offset`.

6. **Pannello Simboli**
   - `Cpu.DumpSimboli` mostra le etichette.

7. **Esempi** (`Docs/samples`, nuova cartella o `08-salti-cicli`)
   - `salti-tabella-menu`: menu con `int 21h` e `jmp [tab+bx]` (l'esempio della sezione 1).
   - `call-tabella-procedure`: calcolatrice con `call [procedure+si]` (somma, differenza, prodotto).
   - `salti-indiretti-errori-compilazione`: forme non ammesse.
   - Ogni esempio con l'intestazione standard e verificato eseguendolo.

8. **Documentazione**
   - Assembly Reference: sezioni JMP e CALL con la forma indiretta, «La sezione dati» (etichette come valori), nota sul modello (IP = numero dell'istruzione).
   - `PROPOSTE-X86.md`: proposta A segnata come completata e aggiornamento dello stato; README.

---

## 6. Rischi

- **Pre-scansione**: è l'unico punto che cambia l'ordine di compilazione. Un errore di conteggio sposterebbe tutte le destinazioni. La mitigano i test sugli indici e l'esecuzione di tutti gli esempi esistenti, che usano molte etichette.
- **Nomi**: etichette e variabili condividono ora un'unica tabella. Programmi che oggi hanno un'etichetta e una variabile con lo stesso nome sono già rifiutati (`SimboloDuplicato`), quindi non cambia nulla per l'utente.

---

## 7. Decisioni aperte

1. **Etichetta senza `offset` nel codice** (`mov bx, caso0`): errore con suggerimento (consigliato, evita di confondere indirizzo e contenuto), oppure accettarla come costante?
2. **`jmp tab`** con `tab` variabile DW: accettarlo come salto attraverso la memoria, in stile MASM (consigliato, coerente con `mov ax, conta`), oppure richiedere `jmp [tab]`?
3. **Etichette nel pannello Simboli**: mostrarle sempre (consigliato) o solo se usate come valori?
4. **Cartella degli esempi**: aggiungerli a `08-salti-cicli` o creare una cartella dedicata?
