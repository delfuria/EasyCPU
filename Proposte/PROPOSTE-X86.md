# Proposte di estensione x86 per EasyCPU

Stato al 26 settembre 2026: **fasi 0–4 completate**. Restano aperte quattro proposte principali (salti indiretti, divieto delle operazioni memoria-memoria, modalità x86 fedele, persistenza nel browser) e alcuni interventi di manutenzione. Il documento riassume lo stato attuale, le decisioni prese, le differenze che restano rispetto a x86 e l'**ordine consigliato** per le prossime fasi.

Documenti di progetto collegati, nella stessa cartella:

| Documento | Contenuto | Stato |
|---|---|---|
| [`SALTI-INDIRETTI.md`](SALTI-INDIRETTI.md) | `jmp`/`call` con registro o memoria, tabelle di salto | progetto |
| [`MODALITA-X86-FEDELE.md`](MODALITA-X86-FEDELE.md) | fase 5: memoria a byte, `byte ptr`/`word ptr` | progetto |
| [`PERSISTENZA-BROWSER.md`](PERSISTENZA-BROWSER.md) | opzioni, layout, recenti e breakpoint conservati nel browser | implementato |
| [`INTERRUPT-CONSOLE.md`](INTERRUPT-CONSOLE.md) | specifica originale di `int 21h` e del pannello Console | implementato (servizi poi estesi in fase 2) |

---

## 1. Cosa supporta oggi EasyCPU

### Architettura

| Aspetto | Stato attuale |
|---|---|
| Registri | AX, BX, CX, DX (con AH/AL, BH/BL, CH/CL, DH/DL), SI, DI, BP, SP, IP |
| Aritmetica | 16 e 8 bit; con e senza segno (MUL/DIV senza segno, IMUL/IDIV con segno) |
| Flag | CF, ZF, SF, DF, OF, nelle posizioni dei bit di x86 (0, 6, 7, 10, 11) |
| Memoria dati | 256 celle **da 16 bit** (modello «a parole»); un accesso a 8 bit usa il byte basso della cella |
| Memoria codice | vettore separato di istruzioni: IP è il numero d'ordine dell'istruzione |
| Stack | celle 240–255 (16 parole), SP parte da 256 |

### Indirizzamento

Immediato (decimale, esadecimale, carattere), a registro, diretto (`[10]`), indiretto (`[si]`), indiretto con scostamento (`[bp+2]`), base + indice (`[bx+si+2]`, solo BX/BP con SI/DI), nomi di variabili e costanti (`conta`, `[vet+si]`, `offset vet`, `[bp+N]`).

### Istruzioni (87 nomi, sinonimi compresi, e 5 prefissi)

| Categoria | Istruzioni |
|---|---|
| Trasferimento | `mov`, `xchg`, `lea` |
| Stack | `push`, `pop`, `pushf`, `popf` |
| Aritmetiche | `add`, `adc`, `sub`, `sbb`, `mul`, `imul`, `div`, `idiv`, `inc`, `dec`, `neg`, `cmp`, `cbw`, `cwd` |
| Logiche | `and`, `or`, `xor`, `not`, `test` |
| Shift e rotazioni | `shl`, `shr`, `sar`, `rol`, `ror`, `rcl`, `rcr` |
| Flag | `clc`, `stc`, `cmc`, `cld`, `std` |
| Salti con segno | `je`/`jz`, `jne`/`jnz`, `jg`/`jnle`, `jge`/`jnl`, `jl`/`jnge`, `jle`/`jng`, `jo`, `jno`, `js`, `jns` |
| Salti senza segno | `ja`/`jnbe`, `jae`/`jnb`/`jnc`, `jb`/`jnae`/`jc`, `jbe`/`jna` |
| Cicli | `loop`, `loope`/`loopz`, `loopne`/`loopnz`, `jcxz` |
| Controllo | `jmp`, `call`, `ret`, `ret n`, `nop`, `stop` |
| Stringhe | `movs`/`movsb`/`movsw`, `lodsb`/`lodsw`, `stosb`/`stosw`, `cmpsb`/`cmpsw`, `scasb`/`scasw`; prefissi `rep`, `repe`/`repz`, `repne`/`repnz` |
| Sistema | `int 21h` con AH = 01h, 02h, 07h, 09h, 0Ah, 4Ch |

### Sezione dati

Stile MASM: `DB`, `DW`, `EQU`, `ORG`, `DUP`, `?`, stringhe tra apici, `offset`; resta valida la forma originale `indirizzo: valori`. Allocazione sequenziale da 0, con errore se i dati invadono lo stack.

### IDE

Pannello Registri con byte alto/basso e flag C, Z, S, O, D; pannello Memoria con la sezione Simboli; errori di compilazione e di esecuzione nel pannello Errori con la riga responsabile; ripetizioni dei prefissi REP eseguite una per Step.

### Verifica

91 test automatici (`EasyCpu.Assembler.Tests`) e 63 programmi di esempio in `Docs/samples`, suddivisi in 10 cartelle per argomento, ognuno con un commento iniziale che descrive il risultato atteso.

---

## 2. Fasi completate

| Fase | Contenuto | Decisioni principali |
|---|---|---|
| 0 | Operando strutturato (`Operando` al posto dell'enum `IdOp`) e test di regressione | Nessun cambiamento di comportamento |
| 1 | Registri a 8 bit; errori di divisione | Memoria a parole (opzione A della P1); errori a runtime mostrati nel pannello Errori |
| 2 | Sezione dati simbolica; `int 21h` con AH (01h, 02h, 07h, 09h, 0Ah, 4Ch) | Nomi in stile MASM (`conta` = contenuto, `offset conta` = indirizzo); nessuna compatibilità con la vecchia selezione del servizio tramite AX; due editor (Codice e Dati) |
| 3 | CF e aritmetica senza segno; istruzioni x86 comuni | `mul`/`div` senza segno, nuove `imul`/`idiv`; `shr` logico e nuovo `sar`; bit dei flag come x86; `imul` solo a un operando |
| 4 | Istruzioni stringa, DF, prefissi REP; indirizzamento base + indice | Forme B/W; una ripetizione per Step; solo le combinazioni x86 (BX/BP + SI/DI) |

Le proposte originali P1–P8 dell'analisi iniziale sono state tutte realizzate, con le eccezioni riportate nella sezione 4 (salti indiretti, divieto memoria-memoria, memoria a byte).

---

## 3. Differenze che restano rispetto a x86

Sono scelte del modello didattico o limiti noti; ognuna è documentata nel manuale o affrontata da una proposta.

| Differenza | Motivo | Proposta |
|---|---|---|
| Memoria a celle da 16 bit: `[10]` e `[11]` non si sovrappongono, un vettore di parole avanza di 1 | Semplicità, compatibilità con i programmi esistenti | C – modalità x86 fedele |
| Operazioni memoria-memoria accettate (`mov a, b`) | Non rompere i programmi esistenti | B |
| `jmp`/`call` solo verso un'etichetta | Le etichette non sono ancora usabili come valori | A |
| Codice in una memoria separata, IP = numero dell'istruzione | Modello Harvard semplificato | nessuna: da spiegare nel manuale |
| Niente segmenti (CS, DS, SS, ES) | Fuori dagli obiettivi didattici | nessuna |
| Flag PF, AF, IF assenti (niente `jp`/`jnp`) | Poco utili didatticamente | nessuna, salvo richiesta |
| `imul` solo nella forma a un operando (8086) | Coerenza con il set 8086 | nessuna, salvo richiesta |
| Solo `int 21h` | Unico servizio utile per l'I/O | nessuna |

---

## 4. Proposte aperte

Valore didattico (**V**) e sforzo (**S**) su una scala da 1 a 3.

### A — Salti e chiamate indirette · V3 · S2

`jmp ax`, `jmp [tab+bx]`, `call [procedure+si]`, con le etichette usabili come valori (`offset etichetta`, `tab DW caso0, caso1`). Insegna tabelle di salto (`switch`/`case`) e puntatori a funzione.

- L'esecuzione è già quasi pronta (`NuovoIp()` legge qualsiasi operando); il lavoro è nel compilatore: una **pre-scansione** delle etichette prima della sezione dati.
- Rischio medio: cambia l'ordine di compilazione; lo coprono i 91 test e i 63 esempi.
- Progetto completo: [`SALTI-INDIRETTI.md`](SALTI-INDIRETTI.md).

### B — Divieto delle operazioni memoria-memoria · V2 · S1

Rendere un errore di compilazione `mov [1], [2]`, `mov a, b`, `add a, b`, come su x86 (restano ammesse le istruzioni stringa). Rimandato dalla fase 4 perché **rompe programmi esistenti**.

- Modifica piccola: un controllo in `Instruction.VerificaIstruzione` (`InMemoria(Op1) && InMemoria(Op2)`, escluse le istruzioni stringa).
- Nota già presente nell'Assembly Reference («Modelli di indirizzamento»).
- Proposta: attivarlo solo nella modalità x86 fedele (C), dove i programmi sono comunque nuovi; valutare in seguito se estenderlo alla modalità a parole.

### C — Modalità x86 fedele (fase 5) · V3 · S3

Memoria a byte, parole little-endian, stack a passi di 2, `byte ptr`/`word ptr`, operandi di dimensione ambigua come errore. Modalità **opzionale**, scelta con la direttiva `.MEMORIA BYTE` nella sezione dati: la modalità a parole resta la predefinita.

- È il cambiamento più invasivo: tocca memoria, CPU, parser, compilatore e pannelli.
- Il progetto prevede prima un refactoring a comportamento invariato, verificato da tutti i test e gli esempi.
- Progetto completo: [`MODALITA-X86-FEDELE.md`](MODALITA-X86-FEDELE.md).

### D — Persistenza delle impostazioni nel browser · V1 · S2 (utilità alta) · ✅ implementata

Nella versione Browser opzioni, layout, file recenti e breakpoint si perdono a ogni ricaricamento della pagina. Si propone un archivio chiave-valore con due implementazioni: file sul Desktop (comportamento attuale, invariato) e `localStorage` nel browser.

- Non riguarda il linguaggio, ma l'uso quotidiano della versione Browser (quella pubblicata per gli studenti).
- Rischio basso: isolato nell'IDE, in `EasyCpu.Backend` e nel progetto Browser.
- Progetto completo: [`PERSISTENZA-BROWSER.md`](PERSISTENZA-BROWSER.md).

### E — Bozza automatica del programma · V1 · S1 (dopo D)

Salvare periodicamente nel browser il contenuto degli editor, per ritrovarlo dopo un ricaricamento accidentale della pagina. Riusa l'archivio della proposta D (chiave `bozza`). Descritta tra le decisioni aperte di `PERSISTENZA-BROWSER.md`.

### F — Manutenzione e documentazione · S1

Piccoli interventi emersi durante le fasi 0–4:

1. **Manuale `.docx`/`.odt`** (`Docs/Easy CPU  Assembly Reference`): è aggiornata solo la versione Markdown; le altre due vanno riallineate.
2. **Sezione OR mancante** nell'Assembly Reference (esistono AND, XOR, NOT e TEST).
3. **Esempio «Struttura di un programma»** nel manuale: usa ancora il vecchio programma SommaDispari, in cui la cella 1 fa sia da indirizzo del vettore sia da suo primo elemento; sostituirlo con la versione corretta di `Docs/samples/07-programmi/programma-somma-dispari.asj`.
4. **Backspace nel servizio 0Ah**: il pannello Console non invia il tasto Backspace, quindi durante la lettura di una riga non si può correggere; va aggiunto nel pannello Console e gestito nel servizio.
5. **Nota sul modello di memoria del codice** (IP = numero dell'istruzione) nel manuale, utile soprattutto dopo la proposta A.

---

## 5. Ordine consigliato

### Criteri

- **Beneficio immediato** per chi usa EasyCPU oggi.
- **Rischio**: non sovrapporre due grandi modifiche allo stesso codice (il compilatore cambia sia in A sia in C).
- **Dipendenze**: gli esempi di C usano le tabelle di salto di A; E usa l'archivio di D; B si inserisce naturalmente in C.
- **Stabilità per gli studenti**: raggruppare i cambiamenti che rompono i programmi esistenti, e renderli opzionali quando possibile.

### Sequenza

| Ordine | Proposta | Perché in questa posizione |
|---|---|---|
| 1 | **D** – Persistenza nel browser (con **F.2–F.4**) | Beneficio immediato per la versione pubblicata, rischio basso, nessun effetto sui programmi. Le piccole correzioni F.2–F.4 sono indipendenti e si possono unire a questa fase. |
| 2 | **A** – Salti indiretti | Completa il set di istruzioni 8086 di base in entrambe le modalità. Cambia l'ordine di compilazione: meglio farlo e stabilizzarlo **prima** del grande refactoring della fase 5. |
| 3 | **C** – Modalità x86 fedele, con **B** attivo solo in questa modalità | La più invasiva, da affrontare con il resto stabile. Il divieto memoria-memoria nasce dentro una modalità nuova e opzionale, senza rompere i programmi esistenti. |
| 4 | **E** – Bozza automatica | Miglioramento dell'IDE, può anche seguire subito D se la perdita del lavoro si rivela un problema frequente. |
| 5 | **B** nella modalità a parole (facoltativo) | Solo se si decide di allineare anche la modalità predefinita a x86, accettando di rompere i programmi che copiano memoria su memoria. |
| 6 | **F.1, F.5** – Manuale `.docx`/`.odt` e nota sul modello | Alla fine, quando il linguaggio è stabile: evita di riallineare i manuali più volte. |

```
D (+F.2–F.4) ──► A ──► C (+B) ──► F.1, F.5
   └──► E (in qualsiasi momento dopo D)
                        B in modalità a parole: facoltativo, dopo C
```

---

## 6. Decisioni aperte

Raccolte dai documenti di progetto; la scelta consigliata è indicata tra parentesi.

**Persistenza nel browser (D, E)**
1. File recenti nel browser: conservare il contenuto dei programmi in `localStorage` (consigliata) o nascondere il menu?
2. Bozza automatica: subito dopo D o più avanti?
3. Breakpoint identificati dal solo nome del file: accettabile (consigliato) o con una firma del contenuto?

**Salti indiretti (A)**
4. Etichetta senza `offset` nel codice (`mov bx, caso0`): errore con suggerimento (consigliato) o costante?
5. `jmp tab` con `tab` variabile DW: salto attraverso la memoria in stile MASM (consigliato) o solo `jmp [tab]`?
6. Etichette nel pannello Simboli: sempre (consigliato)?
7. Esempi in `08-salti-cicli` o in una cartella dedicata?

**Modalità x86 fedele (C, B)**
8. Scelta della modalità: direttiva `.MEMORIA BYTE` (consigliata) o campo nel file `.asj`?
9. Dimensioni: 1 KB di memoria e 128 byte di stack (consigliate), 256 byte o 4 KB?
10. Forma `indirizzo: valori` nella memoria a byte: valori come parole (consigliata), come byte o vietata?
11. Divieto memoria-memoria attivo automaticamente nella modalità fedele (consigliato)?
