# Proposte di estensione x86 per EasyCPU

Stato: **proposta**, niente ancora implementato. Il documento parte da un'analisi del codice attuale (`EasyCpu.Assembler`) e propone estensioni in ordine di priorità.

---

## 1. Cosa supporta oggi EasyCPU

### Architettura

| Aspetto | Stato attuale | Riferimento |
|---|---|---|
| Aritmetica | Solo 16 bit con segno (`short`) | `Cpu.cs` |
| Registri | AX, BX, CX, DX, SI, DI, BP, SP, IP. **Niente registri a 8 bit** | `Enums.cs` `IdOp` |
| Memoria dati | 256 celle, **ognuna di 16 bit** (indirizzamento a parola, non a byte) | `Ram.cs` |
| Memoria codice | Vettore separato di `Instruction`: IP è l'indice dell'istruzione, non un indirizzo di byte | `Cpu.cs` `Fetch` |
| Stack | Celle 240–255 (16 parole), SP parte da 256 | `Ram.cs` |
| Flag | Solo **ZF, SF, OF**. Mancano CF, PF, AF, DF, IF | `Cpu.cs` |

### Indirizzamento

- Immediato: decimale, esadecimale con suffisso `h`, carattere `'A'`
- A registro
- Diretto: `[10]`, `[0Ah]`
- Indiretto con un solo registro: `[si]`, `[di]`, `[bx]`, `[bp]`
- Indiretto con scostamento costante: `[bp+2]`, `[di-1]`

Non sono supportati la forma base + indice (`[bx+si]`), le etichette di dati (`[vet]`, `[vet+si]`), i prefissi `byte ptr`/`word ptr` e `offset`.

### Istruzioni (37)

| Categoria | Istruzioni |
|---|---|
| Trasferimento | `mov`, `movs`, `push`, `pop`, `pushf`, `popf` |
| Aritmetiche | `add`, `sub`, `mul`, `div`, `inc`, `dec`, `neg`, `cmp` |
| Logiche/shift | `and`, `or`, `xor`, `not`, `shl`, `shr` |
| Salti | `jmp`, `je`, `jne`, `jg`, `jge`, `jl`, `jle`, `jo`, `jno`, `js`, `jns`, `jcxz` |
| Procedure | `call`, `ret` |
| Sistema | `int 21h` (AX=1 legge con eco, AX=2 scrive DX, AX=7 legge senza eco), `nop`, `stop` |

### Sezione dati (`.DATA`)

La direttiva `.DATA` **esiste già**, ma solo nella forma "indirizzo: valori":

```asm
.DATA
0: 5
1: 1, 3, 4, 6, 7
```

Mancano nomi simbolici, tipi (`DB`/`DW`), stringhe, `DUP` e costanti `EQU`. Nell'IDE codice e dati stanno in due editor separati. Nel file `.as` le due parti sono separate dalla riga `.DATA`.

### Incongruenze con x86 reale emerse dall'analisi

Non sono proposte nuove, ma conviene saperle perché alcune estensioni ci passano sopra.

1. **`mul` e `div` sono con segno** (`IMul`/`IDiv` in `Cpu.cs`). Su x86 `MUL`/`DIV` sono senza segno, le versioni con segno sono `IMUL`/`IDIV`.
2. **Divisione per zero non gestita**: `IDiv` lancia una `DivideByZeroException` .NET, non una `CpuException`. Anche il quoziente che non sta in 16 bit (#DE su x86) non viene controllato.
3. **`movs` non aggiorna SI e DI**. Su x86 li incrementa o decrementa in base a DF.
4. **Operazioni memoria-memoria accettate** (`mov [1], [2]`): x86 non le permette, `Instruction.VerificaIstruzione` non lo controlla.
5. **`jmp`/`call` accettano solo etichette**, non registri o memoria (`jmp ax`, `call [bx]`).
6. **Shift senza CF**: il bit uscito si perde, quindi non si possono fare aritmetica multi-parola o test di bit.

---

## 2. Proposte

Ogni proposta indica valore didattico (**V**) e sforzo (**S**) su una scala da 1 a 3.

### P1 — Registri a 8 bit (AL, AH, BL, BH, CL, CH, DL, DH) · V3 · S2

È la richiesta più naturale ed è la base di molte altre (servizi `int 21h` basati su AH, `mul`/`div` a 8 bit, stringhe).

**Cosa implementare**

- Nuovi valori in `IdOp` per gli 8 registri. Sono **viste** su AX/BX/CX/DX, non registri separati: `mov ah, 1` modifica i bit 8–15 di AX.
- `LoadOp`/`StoreOp` sensibili alla dimensione. Serve una "larghezza operando" (8/16) calcolata dal parser per ogni istruzione.
- **Controllo di coerenza delle dimensioni** a compile time: `mov al, bx` diventa un errore (nuovo `CodiceErrore.DimensioneOperandi`). Le costanti devono stare in -128..255 quando la destinazione è a 8 bit.
- **Flag calcolati sulla larghezza**: SF = bit 7, OF sull'intervallo -128..127 per le operazioni a 8 bit.
- `mul r/m8` dà AX = AL × op; `div r/m8` dà AL = quoziente e AH = resto.
- `push`/`pop` restano solo a 16 bit, come su x86 (`push al` diventa un errore).
- UI: il pannello Registri mostra AH/AL separati per AX..DX (per esempio `AX = 0141h  [AH=01h AL=41h]`). Aggiornare l'evidenziazione sintattica in `EasyCPU.xshd`.

**Decisione chiave: il modello di memoria.** Oggi ogni cella è di 16 bit. Con operandi a 8 bit diretti alla memoria (`mov al, [10]`) ci sono due strade:

| Opzione | Descrizione | Pro | Contro |
|---|---|---|---|
| **A. Memoria a parole (consigliata per iniziare)** | Le celle restano da 16 bit. Un accesso a 8 bit legge o scrive solo il byte basso della cella | Nessun programma esistente si rompe. Documentazione e esempi restano validi | Non è fedele a x86: `[10]` e `[11]` non si sovrappongono |
| **B. Memoria a byte** | 256 byte, parole little-endian su 2 celle consecutive | Fedele a x86, insegna endianness e allineamento | **Rompe tutti i programmi esistenti** (il vettore scorre con `add si, 2` invece di `inc si`). Stack da 16 byte = 8 parole. Va riscritto il pannello Memoria |

Proposta: implementare P1 con l'opzione A. Valutare B in futuro come **modalità opzionale** ("modalità x86 fedele") nelle Opzioni, magari con memoria più grande (1 KB o 4 KB).

Con l'opzione B diventano necessari `byte ptr`/`word ptr` per risolvere ambiguità come `inc [si]` o `mov [di], 5`.

**File toccati**: `Enums.cs`, `Parser.cs` (`LeggiOperando`), `Instruction.cs` (validazione dimensioni), `Cpu.cs` (`LoadOp`/`StoreOp`/`SetFlags`/`IMul`/`IDiv`/`ServizioSistema`), `Errori.cs`, pannello Registri, `EasyCPU.xshd`, Assembly Reference.

---

### P2 — `.DATA` con variabili simboliche · V3 · S2

Oggi il programmatore deve ricordare a mano "il vettore sta all'indirizzo 10". Le variabili nominate sono la differenza più visibile rispetto a un assembler vero (MASM/TASM/emu8086).

**Sintassi proposta** (compatibile MASM):

```asm
.DATA
N       EQU 5                   ; costante simbolica, non occupa memoria
conta   DW  0                   ; una parola
vet     DW  1, 3, 4, 6, 7
buffer  DW  10 DUP(0)           ; 10 parole a zero
msg     DB  'Ciao mondo$'       ; stringa: un carattere per cella
        ORG 100                 ; sposta il contatore di allocazione
tabella DW  N DUP(?)
20: 7, 8, 9                     ; la vecchia sintassi resta valida
```

**Uso nel codice**:

```asm
mov cx, N              ; EQU: costante
mov ax, [conta]        ; indirizzamento diretto simbolico
mov ax, conta          ; stessa cosa (stile MASM), da valutare
mov si, offset vet     ; indirizzo della variabile
mov ax, [vet+2]        ; simbolo + scostamento
mov ax, [vet+si]       ; simbolo come scostamento di un indiretto
```

**Cosa implementare**

- **Tabella dei simboli dati** (nome, indirizzo, tipo DB/DW, dimensione), costruita da `CompilaDati` prima di `CompilaCodice` e passata al `Parser`. Oggi `Compiler` compila le due sezioni in modo indipendente.
- Allocazione sequenziale automatica da 0 (o da `ORG`), con un errore se sconfina nello stack (≥ 240).
- `EQU` utilizzabile sia in `.DATA` sia nel codice.
- Stringhe letterali `'...'`: oggi `LeggiCostanteChar` accetta un solo carattere. Con la memoria a parole (P1 opzione A) ogni carattere occupa una cella, per cui `DB` e `DW` differiscono solo nel controllo dell'intervallo dei valori.
- Nuovi errori: simbolo duplicato, simbolo non definito, dati oltre l'area dello stack.
- UI: il pannello Memoria può mostrare il nome della variabile accanto all'indirizzo (tooltip o colonna). Il pannello Errori deve già distinguere le righe dati da quelle di codice (`CompilerError.DATI`).
- Opzionale: direttiva `.CODE` e **editor unico** con entrambe le sezioni, come negli assembler reali. Oggi il file `.as` è già un unico file, solo l'IDE lo divide in due.

---

### P3 — Carry flag e aritmetica senza segno · V3 · S2

Senza CF non si possono insegnare confronti senza segno, aritmetica multi-precisione e test di bit.

- Flag **CF** calcolato da `add`/`sub`/`cmp`/`neg`/shift (`mul` lo imposta insieme a OF).
- Salti senza segno: `ja`, `jae`, `jb`, `jbe`, `jc`, `jnc`.
- Alias x86 standard: `jz`/`jnz` (= `je`/`jne`), `jnae`, `jnb`, `jng`, `jnl`…
- `adc`, `sbb`, `clc`, `stc`, `cmc`.
- **Correzione**: `mul`/`div` diventano senza segno, e si aggiungono `imul`/`idiv` con segno. È un cambiamento incompatibile: i programmi che usano `mul` con numeri negativi cambiano comportamento. In alternativa si lascia `mul` com'è e si aggiunge solo `imul`, documentando la differenza.
- Pannello flag: aggiungere CF (e PF se si implementa, utile solo per completezza).

---

### P4 — Istruzioni x86 comuni mancanti · V2 · S1–2

Sono in ordine di utilità didattica. Quasi tutte sono piccole aggiunte in `SetCode` e in `Execute`.

| Istruzione | Note |
|---|---|
| `test` | Come `and` ma senza scrivere il risultato |
| `loop`, `loope`, `loopne` | Decrementa CX e salta. Idioma classico dei cicli x86 |
| `xchg` | Scambio tra registri o tra registro e memoria |
| `lea` | Indirizzo effettivo: `lea si, [bx+2]`, `lea si, vet` (serve P2) |
| `cbw`, `cwd` | Estensione del segno, indispensabili prima di `idiv` |
| `sar`, `rol`, `ror`, `rcl`, `rcr` | Shift aritmetico e rotazioni (servono CF da P3) |
| `ret n` | Ritorno che rimuove n parametri dallo stack, convenzione Pascal/stdcall |
| `jmp reg`/`call reg` | Salti indiretti, per insegnare tabelle di salto |

---

### P5 — Istruzioni stringa e flag DF · V2 · S2

- Correggere `movs` in modo che aggiorni SI e DI.
- Flag **DF** con `cld`/`std`.
- `lods`, `stos`, `cmps`, `scas`, e prefissi `rep`, `repe`, `repne`.
- Si abbinano bene a P2 (stringhe in `.DATA`) e P6 (stampa di stringhe).

---

### P6 — Servizi `int 21h` estesi · V2 · S1

Con P1 i servizi si selezionano con **AH**, come in DOS. Oggi si usa AX solo perché AH non esiste (commento in `Cpu.cs`, `ServizioSistema`).

| AH | Servizio |
|---|---|
| 01h | Legge un carattere con eco, risultato in AL (esiste già, oggi con AX=1) |
| 02h | Scrive il carattere in DL (esiste già, oggi con AX=2 e DX) |
| 07h | Legge un carattere senza eco (esiste già) |
| **09h** | **Stampa la stringa terminata da `$` all'indirizzo in DX** (serve P2) |
| **0Ah** | Input di una riga in un buffer (formato DOS: max, letti, caratteri) |
| **4Ch** | Termina il programma (alternativa x86 a `stop`) |

**Compatibilità**: selezionare il servizio con AH rompe i programmi che fanno `mov ax, 2`, perché AH=0. Due opzioni:

- usare il vecchio comportamento se AH=0 e AL≠0;
- cambiare in modo netto e aggiornare esempi e documentazione.

Consigliata la prima opzione, che non rompe niente.

---

### P7 — Indirizzamento completo · V2 · S2

- Base + indice: `[bx+si]`, `[bx+di]`, `[bp+si]`, `[bp+di]`, con scostamento facoltativo (`[bx+si+2]`). Richiede un'estensione di `IdOp` o, meglio, un operando strutturato (base, indice, scostamento) invece dell'enum piatto.
- Simbolo come scostamento: `[vet+si]` (con P2).
- Validazione: vietare memoria-memoria (tranne `movs` e simili) e `mov` verso costante (già presente).

Nota tecnica: l'enum `IdOp` più un `int offset` non basta a rappresentare base+indice+scostamento+dimensione. Conviene introdurre una struct `Operando { Tipo, Base, Indice, Scostamento, Larghezza }` **prima** di implementare P1/P7, per non aggiungere decine di valori all'enum.

---

### P8 — Correzioni di robustezza · V1 · S1

- Divisione per zero e quoziente troppo grande: `CpuException` con un messaggio chiaro invece dell'eccezione .NET.
- Test unitari: oggi esistono 13 test (`CpuTests.cs`), quasi tutti su stepping e `int 21h`. Ogni proposta dovrebbe arrivare con i propri test (flag per ogni istruzione, errori di dimensione, simboli).

---

## 3. Roadmap suggerita

| Fase | Contenuto | Motivo |
|---|---|---|
| 0 | Refactor dell'operando in struct (nota P7) + test di regressione sugli esempi in `Docs/Subroutines` | Base solida, evita di riscrivere due volte `LoadOp`/`StoreOp` |
| 1 | **P1** registri a 8 bit (memoria a parole) + **P8** | Richiesta principale, basso rischio |
| 2 | **P2** `.DATA` simbolica + **P6** `int 21h` con AH e servizio 09h | Insieme permettono il classico "Hello World" x86 |
| 3 | **P3** CF e aritmetica senza segno + **P4** istruzioni mancanti | Completa il set per esercizi tipici |
| 4 | **P5** istruzioni stringa + **P7** base+indice | Avanzato |
| 5 | Modalità "x86 fedele" con memoria a byte (P1 opzione B) | Solo se serve davvero, è il cambiamento più invasivo |

Esempio di obiettivo alla fine della fase 2, oggi impossibile:

```asm
.DATA
msg DB 'Ciao mondo!$'

.CODE
        mov ah, 09h
        mov dx, offset msg
        int 21h
        mov ah, 4Ch
        int 21h
```

---

## 4. Decisioni aperte

1. Modello di memoria: celle da 16 bit (A) o memoria a byte (B)? Oppure A ora e B come modalità opzionale?
2. `mul`/`div`: correggerli come senza segno (incompatibile) o solo aggiungere `imul`/`idiv`?
3. `int 21h`: selezione con AH mantenendo la compatibilità con AX, oppure cambio netto?
4. Editor unico `.DATA` + `.CODE` o restare con due editor separati?
5. Dimensione della memoria: resta 256 celle o si amplia (utile con stringhe e `DUP`)?
