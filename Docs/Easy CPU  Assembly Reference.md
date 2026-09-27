EasyCPU

Assembly reference

|  |  |
| --- | --- |
| ![](data:image/x-emf;base64...) | **Paolo Meozzi**  **Stefano Del Furia** |

Indice

- Premessa
- Struttura della CPU
  - Aritmetica
  - Memoria dati
  - Registri
  - Modelli di indirizzamento
  - Gestione dello stack
  - Flags
- Set di istruzioni
  - Descrizione generale delle istruzioni
  - Modalità di descrizione delle istruzioni
  - ADC – Addizione con riporto
  - ADD – Addizione
  - AND – Moltiplicazione logica
  - CALL – Chiamata di una procedura
  - CBW – Estensione del segno da byte a parola
  - CLC – Azzeramento del carry
  - CLD – Azzeramento del flag di direzione
  - CMC – Inversione del carry
  - CMP – Confronto
  - CMPSB, CMPSW – Confronto di elementi di due sequenze
  - CWD – Estensione del segno da parola a doppia parola
  - DEC – Decremento
  - DIV – Divisione senza segno
  - IDIV – Divisione con segno
  - IMUL – Moltiplicazione con segno
  - INC – Incremento
  - INT – Chiamata di un servizio di sistema
  - JA – Salto se superiore (senza segno)
  - JAE – Salto se superiore o uguale (senza segno)
  - JB – Salto se inferiore (senza segno)
  - JBE – Salto se inferiore o uguale (senza segno)
  - JCXZ – Salto se CX è zero
  - JE – Salto se uguale
  - JG – Salto se maggiore
  - JGE – Salto se maggiore o uguale
  - JL – Salto se minore
  - JLE – Salto se minore o uguale
  - JMP – Salto incondizionato
  - JNE – Salto se diverso
  - JNO – Salto se non overflow
  - JNS – Salto se il flag di segno è 0
  - JO – Salto se overflow
  - JS – Salto se il flag di segno è 1
  - LEA – Caricamento dell’indirizzo effettivo
  - LODSB, LODSW – Lettura di un elemento di una sequenza
  - LOOP, LOOPE, LOOPNE – Cicli con contatore
  - MOV – Trasferimento
  - MOVS, MOVSB, MOVSW – Copia di un elemento di una sequenza
  - MUL – Moltiplicazione senza segno
  - NEG – Negazione (formazione del complemento a 2)
  - NOP – Nessuna operazione
  - NOT – Negazione logica (formazione del complemento a 1)
  - OR – Somma logica
  - POP – Prelevamento dallo stack
  - POPF – Prelevamento del registro dei flags dallo stack
  - PUSH – Deposito di un valore nello stack
  - PUSHF – Deposito del registro dei flags nello stack
  - RCL, RCR – Rotazione attraverso il carry
  - REP, REPE, REPNE – Prefissi di ripetizione
  - RET – Ritorno da una procedura
  - ROL, ROR – Rotazione
  - SAR – Shift aritmetico a destra
  - SCASB, SCASW – Ricerca in una sequenza
  - SBB – Sottrazione con prestito
  - SHL – Shift logico a sinistra
  - SHR – Shift logico a destra
  - STC – Impostazione del carry
  - STD – Impostazione del flag di direzione
  - STOP – Arresta la CPU
  - STOSB, STOSW – Scrittura di un elemento di una sequenza
  - SUB – Sottrazione
  - TEST – Test logico
  - XCHG – Scambio
  - XOR – Or esclusivo
- Struttura di un programma assembly
- La sezione dati
  - Uso dei nomi nel codice
  - Esempio: Hello world
- Modalità x86 fedele: memoria a byte
  - Come si sceglie
  - Il modello di memoria
  - Regole in più rispetto alla memoria a parole
  - Differenze nei programmi
  - Pannelli

## Premessa

EasyCPU è simulatore di un CPU che consente di scrivere programmi in linguaggio assembly e di testarne il funzionamento all’interno di una ambiente di programmazione dotato di interfaccia grafica.

EasyCPU simula l’architettura dei microprocessori X86 di INTEL, anche se implementa un set di istruzioni una gestione della memoria estremamente semplificati rispetto a quelli supportati dai microprocessore reali. Per questo motivo, EasyCPU non può ritenersi un modello completo per l’apprendimento della programmazione assembly e della struttura delle CPU X86; il suo scopo è semplicemente quello di fornire uno strumento facile da apprendere e da utilizzare per l’acquisizione dei principi generali relativi alla programmazione in linguaggio assembly e al funzionamento e alla struttura di un microprocessore.

## Struttura della CPU

### Aritmetica

EasyCPU supporta l’aritmetica a 16 bit e a 8 bit. Ogni valore, sia esso immediato (costante) che memorizzato in un registro o in memoria, è rappresentato mediante il tipo intero a 16 bit, con un intervallo di variazione da –32768 e + 32767.

Le istruzioni che usano un registro a 8 bit (vedi «Registri») operano a 8 bit, con un intervallo di variazione da –128 a +127.

Gli stessi bit possono rappresentare un numero con segno o senza segno: FFFFh vale –1 con segno e 65535 senza segno (a 8 bit, FFh vale –1 oppure 255). ADD, SUB, INC, DEC e CMP producono lo stesso risultato nei due casi; cambia l’interpretazione dei flag (OF e SF per i numeri con segno, CF per quelli senza segno) e quindi il salto condizionato da usare dopo un confronto. Per la moltiplicazione e la divisione ci sono istruzioni distinte: MUL e DIV senza segno, IMUL e IDIV con segno.

### Memoria dati

EasyCPU supporta una memoria non segmentata di 256 celle da 16 bit (memoria «a parole», la modalità predefinita). Di queste, le ultime 16 sono riservate allo «stack». Nella modalità x86 fedele la memoria è invece di 512 byte, con le parole di due byte come su x86: vedi «Modalità x86 fedele: memoria a byte».

![](data:image/x-emf;base64...)

Diversamente da quanto accade in un sistema reale, nella memoria vengono memorizzati soltanto i dati. Le istruzioni sono collocate in un vettore a se stante, del quale il registro IP funge da indice. Di conseguenza l’indirizzo di un’etichetta è il numero d’ordine della sua istruzione (0, 1, 2…) e non un indirizzo di memoria in byte come su x86: è il valore che contengono le tabelle di salto usate da JMP e CALL indiretti.

### Registri

EasyCPU supporta un set di registri analogo a quello delle microprocessori X86:

![](data:image/x-emf;base64...)

Registri a 8 bit

Come nei microprocessori X86, i registri AX, BX, CX e DX sono divisi in due metà da 8 bit, utilizzabili come registri autonomi: AH e AL (byte alto e byte basso di AX), BH e BL, CH e CL, DH e DL. Non sono registri separati: modificare AL o AH modifica anche AX, e viceversa.

mov ax, 0 // AX = 0000h

mov ah, 1 // AX = 0100h

mov al, 'A' // AX = 0141h

In un’istruzione con due operandi, i registri devono avere la stessa dimensione: mov al, bx produce l’errore «Dimensione degli operandi non valida o non coerente». Una costante usata con un registro a 8 bit deve essere compresa tra –128 e 255 (da 0 a FFh in esadecimale).

Quando un registro a 8 bit viene usato con un operando in memoria (mov al, [10], mov [si], dl), nella memoria a parole l’istruzione legge o scrive soltanto il byte basso della cella: il byte alto della cella non viene modificato. Le celle di memoria restano infatti di 16 bit, e due celle consecutive non si sovrappongono come accade nei microprocessori X86 (e nella memoria a byte, vedi «Modalità x86 fedele: memoria a byte»).

Le istruzioni PUSH e POP operano soltanto a 16 bit: push al produce un errore.

Il pannello Registri mostra, accanto ad AX, BX, CX e DX, il valore dei rispettivi byte alto e basso (ad esempio AX = 0141 [AH=01 AL=41]).

### Modelli di indirizzamento

EasyCPU supporta 6 modelli di indirizzamento: immediato, a registro, diretto, indiretto a registro, indiretto a registro con scostamento, indiretto con base e indice.

Indirizzamento immediato

E’ rappresentato da un valore costante, espresso in forma decimale, esadecimale o carattere. Ad esempio, nelle istruzioni:

mov ax, **1** // memorizza 1 nel registro AX

mov ax, **0Ah** // memorizza 10 nel registro AX

mov ax, 'A' // memorizza 65 nel registro AX

i valori 1, 0Ah, ’A’ rappresentano delle costanti espresse in forma decimale, esadecimale e carattere. La forma esadecimale richiede come suffisso la lettera “h”; il primo carattere dev’essere una cifra. Un valore espresso in formato decimale può variare da –32768 a +32767, mentre un valore espresso in formato esadecimale non può essere preceduto dal segno meno e può variare da 0 a FFFF (che equivale a 65535). Una costante in formato carattere è rappresentata mediante un solo carattere delimitato da apici singoli, ed equivale al corrispondente valore ASCII.

Nel caso in cui un’istruzione richieda due operandi, soltanto il secondo può essere un valore immediato.

Nota: diversamente dai microprocessori X86, nella memoria a parole EasyCPU accetta istruzioni con entrambi gli operandi in memoria, come mov [1], [2] o add a, b. Su un processore X86 sono errori: un dato va prima copiato in un registro, oppure si usano le istruzioni stringa (MOVSB, MOVSW). Nella memoria a byte (modalità x86 fedele) il divieto è attivo e produce l’errore «Un'istruzione non può avere due operandi in memoria: usare un registro».

Indirizzamento a registro

In questo tipo di indirizzamento, l’operando è rappresentato da un registro. Un registro può comparire sia come primo che come secondo operando, oppure in entrambi:

mov **ax**, 1 // memorizza 1 nel registro AX

add **bx**, **ax** // somma il contenuto di AX a BX e mette il risultato in BX

Il registro IP non può apparire come operando di una istruzione.

Indirizzamento diretto

Nell’indirizzamento diretto l’operando è rappresentato dall’indirizzo di una locazione di memoria, che può variare da 0 a 255 (da 0 a 511 nella memoria a byte). Un riferimento a un indirizzo della memoria dati è caratterizzato dalla sintassi “[indirizzo]”. Ad esempio:

mov ax, **[1]** // memorizza in AX il contenuto della cella di memoria 1

mov bx, **[0Ah]** // memorizza in BX il contenuto della cella di memoria 10

Come si vede, l’indirizzo di memoria può essere espresso sia in forma decimale che esadecimale.

In realtà un indirizzo di memoria può essere espresso anche mediante una costante di tipo carattere, ma ciò non rappresenta di norma una buona pratica di programmazione.

Indirizzamento indiretto a registro semplice

Nell’indirizzamento indiretto, l’operando è ancora una volta rappresentato da una locazione della memoria, il cui indirizzo è designato dal contenuto di un registro. A questo scopo possono essere usati i registri SI, DI, BX, BP:

mov ax, **[si]** // memorizza in AX il contenuto della cella di memoria il

// cui indirizzo è memorizzato in SI

mov **[di]**, ax // memorizza il contenuto di AX nella cella di memoria il

// cui indirizzo è memorizzato in DI

add cx, **[bp]** // somma CX al contenuto della cella di memoria il

// cui indirizzo è memorizzato in BP e mette il risultato in CX

Indirizzamento indiretto a registro con scostamento

In questa forma d’indirizzamento, al registro usato come indice viene sommata o sottratta una costante per ottenere uno scostamento dalla cella indirizzata:

mov ax, **[bp+2]** // memorizza in AX il contenuto della cella di memoria

// che si trova all’indirizzo memorizzato in BP più 2

mov **[di-02h]**, ax // memorizza AX nella cella di memoria che si trova

// all’indirizzo memorizzato in DI meno 2

La costante può essere espressa in forma decimale, esadecimale o carattere.

Indirizzamento indiretto con base e indice

L’indirizzo è la somma di un registro base (BX o BP), di un registro indice (SI o DI) e, facoltativamente, di uno scostamento costante. Come nei microprocessori X86 sono ammesse solo queste combinazioni; l’ordine dei termini è libero. Si usa ad esempio per le matrici: la base indica l’inizio della riga, l’indice la colonna.

mov ax, **[bx+si]** // cella di indirizzo BX + SI

mov **[bp+di+2]**, ax // cella di indirizzo BP + DI + 2

mov dx, **[mat+bx+si]** // con il nome di una variabile come scostamento

Combinazioni come [si+di] o [bx+bp] producono l’errore «Combinazione di registri non valida»; un registro non può essere sottratto.

Dimensione dell’operando: byte ptr e word ptr

La dimensione di un accesso alla memoria è determinata dal registro usato (mov al, [si] a 8 bit, mov ax, [si] a 16 bit) o dal tipo della variabile (DB o DW). Quando nessuno dei due la determina, come in inc [si] o mov [di], 5, nella memoria a parole l’accesso è a 16 bit, mentre nella memoria a byte l’istruzione produce l’errore «Dimensione dell'operando non specificata: usare byte ptr o word ptr». Scrivendo **byte ptr** o **word ptr** davanti all’operando in memoria se ne fissa la dimensione, anche in contrasto con il tipo della variabile:

inc **byte ptr** [si] // incrementa un byte (il byte basso della cella, nella memoria a parole)

mov **word ptr** [di], 5 // scrive una parola

mov al, **byte ptr** conta // byte basso della variabile DW conta

Nella memoria a parole byte ptr, come gli altri accessi a 8 bit, legge o scrive il byte basso della cella; nella memoria a byte accede al byte all’indirizzo indicato. Le regole di coerenza restano valide: mov al, word ptr [si] produce l’errore «Dimensione degli operandi non valida o non coerente»; byte ptr e word ptr non si possono usare con un registro o una costante.

### Gestione dello stack

In EasyCPU lo stack viene gestito in modo analogo a quanto avviene nei microprocessori della serie X86, ma in forma semplificata. La parte di memoria riservata allo stack inizia a un indirizzo di base immutabile, che è 240; essa occupa esattamente 16 celle, cioè 16 parole.

Nella fase di inizializzazione della CPU, il registro SP (puntatore allo stack) viene impostato al valore 256 (indirizzo massimo dello stack più 1).

Nella memoria a byte lo stack occupa i byte da 448 a 511 (64 byte, cioè 32 parole), SP parte da 512 e ogni PUSH o POP lo sposta di 2, come su x86.

![](data:image/x-emf;base64...)

Lo stack è una struttura dati di tipo LIFO (*Last In, First Out*: l’ultimo che entra è il primo ad uscire), che viene gestita mediante le istruzioni PUSH e POP. Mediante l’istruzione PUSH viene immesso un valore nello stack; tale operazione determina un decremento del registro SP. Mediante l’istruzione POP l’ultimo valore allocato viene estratto; essa determina un incremento del registro SP.

Il tentativo di allocare un numero di valori superiore alla dimensione dello stack produce un errore di «stack overflow»; un POP a stack vuoto produce l’errore «stack underflow».

### Flags

EasyCPU supporta i flag di carry (CF), segno (SF), zero (ZF) e overflow (OF), i quali vengono prevalentemente impiegati per l’esecuzione dei salti condizionati e sono influenzati dalle operazioni aritmetico logiche (escluse DIV e IDIV).

Il flag CF è settato se un’addizione produce un riporto oltre il bit più significativo, o se una sottrazione (o un confronto) richiede un prestito, cioè se il primo operando, letto come numero senza segno, è minore del secondo. Indica quindi un risultato fuori dall’intervallo dei numeri senza segno (da 0 a 65535, o da 0 a 255 a 8 bit). Negli shift e nelle rotazioni CF riceve l’ultimo bit uscito. INC e DEC non modificano CF. CF può essere impostato direttamente con STC, CLC e CMC.

Il flag SF è settato se il risultato di un’operazione aritmetico-logica produce un risultato negativo, e dunque riflette il valore del bit di ordine superiore del risultato.

Il flag ZF è settato se il risultato di un’operazione aritmetico-logica è zero.

Il flag OF è settato se il risultato di un’operazione aritmetico-logica eccede la capacità di memorizzazione dell’operando e dunque l’intervallo di memorizzazione da –32768 a +32767.

Nelle operazioni a 8 bit i flag si riferiscono al byte: SF riflette il bit 7 del risultato e OF indica un risultato fuori dall’intervallo da –128 a +127.

Un test classico sui flag prevede un confronto tra due operandi tramite l’istruzione CMP, la quale sottrae il secondo operando dal primo senza però memorizzare il risultato, ma influenzando egualmente lo stato dei flag.

I flag sono memorizzati nei bit di un registro a 16 bit, nelle stesse posizioni dei microprocessori X86; è il valore che PUSHF deposita nello stack e POPF preleva:

| Flag | Bit | Valore |
|---|---|---|
| CF | 0 | 0001h |
| ZF | 6 | 0040h |
| SF | 7 | 0080h |
| DF | 10 | 0400h |
| OF | 11 | 0800h |

Il flag di direzione DF non dipende dai risultati delle operazioni: si imposta con STD e si azzera con CLD, e stabilisce se le istruzioni stringa fanno avanzare (DF = 0) o arretrare (DF = 1) i registri SI e DI.

Il pannello Registri mostra i flag come C, Z, S, O, D.

## Set di istruzioni

### Descrizione generale delle istruzioni

EasyCPU supporta istruzioni con zero, uno e due operandi; ogni istruzione ha un numero di operandi predefinito (fa eccezione RET, che ammette la forma RET n). Le istruzioni rispecchiano la seguente sintassi:

<*codice mnemonico*> <*operando1*>opz, <*operando2*>opz

In relazione all’operazione svolta da un’istruzione, l’operando o gli operandi possono assumere il ruolo di «sorgente» e/o di «destinazione». Nel primo caso, l’operando partecipa all’operazione ma non viene modificato da essa. Nel secondo caso, l’operando viene modificato dall’istruzione.

In alcuni casi l’operando «sorgente» o quello «destinazione» sono impliciti, cioè non compaiono nel testo dell’istruzione.

### Modalità di descrizione delle istruzioni

Di seguito, per ogni istruzione saranno presentati:

* il codice mnemonico;
* la sintassi;
* una breve spiegazione sul suo funzionamento, seguita da uno o più esempi d’uso.
* i flag influenzati.

Nota sui flag definiti dalle istruzioni

Nei microprocessori X86, alcune istruzioni pur non settando direttamente i flag li lasciano in uno stato indefinito. In relazione a un determinato flag, ogni istruzione può dunque produrre tre situazioni:

* il flag viene lasciato inalterato;
* in flag viene settato o resettato
* il flag resta in uno stato indefinito (può assumere casualmente 0 o 1);

EasyCPU si comporta in modo diverso. Un flag può essere impostato oppure lasciato inalterato; in sostanza non esiste lo stato indefinito.

### ADC – Addizione con riporto

Sintassi:

**ADC *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione + sorgente + CF**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

ADC somma all’operando destinazione l’operando sorgente e il valore del flag CF (0 o 1). Si usa per sommare numeri più grandi di un registro: si sommano prima le parti basse con ADD, poi le parti alte con ADC, che aggiunge il riporto della prima somma.

Esempi:

add ax, bx // parti basse

adc dx, cx // parti alte più il riporto: DX:AX = DX:AX + CX:BX
### ADD – Addizione

Sintassi:

**ADD *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione + sorgente**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

L’operando destinazione viene sommato all’operando sorgente; il risultato viene memorizzato nell’operando destinazione.

Esempi:

add ax, bx // equivale a: ax = ax + bx

add [10], 2 // equivale a: [10] = [10] + 2

### AND – Moltiplicazione logica

Sintassi:

**AND *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione & sorgente**

Flag definiti:

**SF, ZF; OF = 0, CF = 0**

Descrizione:

AND imposta a 1 i bit del risultato se entrambi i bit corrispondenti dei due operandi sono 1; altrimenti li imposta a 0. Il risultato è memorizzato nell’operando destinazione.

Esempio:

mov ax, 2

and ax, 4 // produce come risultato: 0

### CALL – Chiamata di una procedura

Sintassi:

**CALL *etichetta***

**CALL *sorgente***

Operazione svolta:

**SP = SP – 1 (SP – 2 nella memoria a byte)**

**MEMORIA[SP] = IP**

**IP = indirizzo designato dall’etichetta, oppure valore di sorgente**

Flag definiti:

**Nessuno**

Descrizione:

CALL modifica il flusso di esecuzione delle istruzioni, assegnando al registro IP un nuovo indirizzo nella memoria delle istruzioni, dopo averlo precedentemente salvato sullo stack. Mediante l’istruzione RET è possibile recuperare il valore di IP per riprendere l’esecuzione dell’istruzione successiva a CALL.

Esempio:

call ciclo // IP viene punta alla istruzione designata da "ciclo"

...

ciclo: *<inizio della procedura>*

Nella forma indiretta l’indirizzo della procedura viene letto durante l’esecuzione da un registro a 16 bit o da una cella di memoria (vedi JMP). Una tabella di procedure consente di scegliere la procedura da chiamare in base a un indice:

proc DW somma, prodotto // sezione dati: indirizzi di due procedure

call [proc+si] // chiama la procedura il cui indirizzo è nella cella proc + SI

Nella memoria a byte ogni indirizzo della tabella occupa due byte: l’indice va moltiplicato per 2 (vedi JMP).

### CBW – Estensione del segno da byte a parola

Sintassi:

**CBW**

Operazione svolta:

**AX = AL esteso con segno**

Flag definiti:

**Nessuno**

Descrizione:

CBW copia in tutti i bit di AH il bit di segno di AL, così che AX contenga lo stesso numero con segno di AL. Si usa prima di IDIV con operando a 8 bit, che divide AX.

Esempi:

mov al, -5 // AL = FBh

cbw // AX = FFFBh = -5
### CLC – Azzeramento del carry

Sintassi:

**CLC**

Operazione svolta:

**CF = 0**

Flag definiti:

**CF**

Descrizione:

CLC imposta a 0 il flag CF.

Esempi:

clc
### CLD – Azzeramento del flag di direzione

Sintassi:

**CLD**

Operazione svolta:

**DF = 0**

Flag definiti:

**DF**

Descrizione:

CLD azzera il flag DF: le istruzioni stringa faranno avanzare SI e DI.

Esempi:

cld
### CMC – Inversione del carry

Sintassi:

**CMC**

Operazione svolta:

**CF = not CF**

Flag definiti:

**CF**

Descrizione:

CMC inverte il valore del flag CF.

Esempi:

cmc
### CMP – Confronto

Sintassi:

**CMP *sorgente1, sorgente2***

Operazione svolta:

**sorgente1 – sorgente2**

Flag definiti:

**ZF, SF, OF, CF**

Descrizione:

CMP sottrae il secondo operando al primo senza però memorizzare il risultato. L’effetto è quello di aggiornare i valori dei flags che potranno poi essere testati mediante un’istruzione di salto condizionato.

Esempio:

cmp ax, bx // confronta ax con bx

je salto // salta se ax è uguale a bx (e dunque il flag zero è 1)

### CMPSB, CMPSW – Confronto di elementi di due sequenze

Sintassi:

**CMPSB**

**CMPSW**

Operazione svolta:

**[SI] - [DI], senza memorizzare il risultato**

**SI = SI ± passo, DI = DI ± passo**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

CMPSB e CMPSW confrontano l’elemento puntato da SI con quello puntato da DI come farebbe CMP, poi aggiornano SI e DI. Con REPE confrontano due sequenze finché gli elementi sono uguali.

SI e DI avanzano se DF = 0 (dopo CLD) o arretrano se DF = 1 (dopo STD) di un elemento: è il «passo» indicato nell’operazione svolta. Nella memoria a parole l’elemento è sempre una cella: la forma con suffisso B usa il byte basso delle celle e il registro AL, la forma con suffisso W la cella intera e il registro AX. Nella memoria a byte, come su x86, la forma B avanza di 1 byte e la forma W di 2. Con il prefisso REP l’istruzione viene ripetuta (vedi «REP, REPE, REPNE»).

Esempi:

mov si, offset a

mov di, offset b

mov cx, 4

repe cmpsb // si ferma al primo carattere diverso

jne diverse
### CWD – Estensione del segno da parola a doppia parola

Sintassi:

**CWD**

Operazione svolta:

**DX:AX = AX esteso con segno**

Flag definiti:

**Nessuno**

Descrizione:

CWD copia in tutti i bit di DX il bit di segno di AX: DX diventa FFFFh se AX è negativo, 0 altrimenti. Si usa prima di IDIV con operando a 16 bit, che divide DX:AX.

Esempi:

mov ax, -7

cwd // DX = FFFFh: DX:AX = -7

mov bx, 2

idiv bx // AX = -3, DX = -1
### DEC – Decremento

Sintassi:

**DEC *destinazione***

Operazione svolta:

**destinazione = destinazione – 1**

Flag definiti:

**ZF, SF, OF (CF non cambia)**

Descrizione:

DEC decrementa di uno l’operando

Esempi:

dec ax // decrementa di 1 ax

dec [20] // decrementa di 1 il contenuto della locazione [20]

dec [si] // decrementa di 1 il contenuto della locazione puntata da si

### DIV – Divisione senza segno

Sintassi:

**DIV *sorgente***

Operazione svolta:

**AX = DX:AX / sorgente**

**DX = DX:AX % sorgente**

**Se sorgente è a 8 bit: AL = AX / sorgente, AH = AX % sorgente**

Flag definiti:

**Nessuno**

Descrizione:

DIV divide il numero a 32 bit memorizzato nella coppia di registri DX:AX (DX contiene la parte alta) per l’operando, considerando entrambi numeri senza segno; memorizza in AX il quoziente intero e in DX il resto. Prima di dividere un numero contenuto soltanto in AX occorre quindi azzerare DX.

Se l’operando è a 8 bit, DIV divide AX per l’operando, memorizzando in AL il quoziente e in AH il resto.

Se l’operando vale zero, l’esecuzione si interrompe con l’errore «Divisione per zero». Se il quoziente non sta nel registro destinazione (da 0 a 65535 in AX, da 0 a 255 in AL), l’esecuzione si interrompe con l’errore «Il quoziente della divisione non sta nel registro destinazione».

Per dividere numeri con segno si usa IDIV.

Esempi:

mov dx, 0

div 2 // divide dx:ax per 2

div [20] // divide dx:ax per il contenuto della locazione [20]

div [si] // divide dx:ax per il contenuto della locazione puntata da si

div bl // divide ax per bl: quoziente in al, resto in ah

### IDIV – Divisione con segno

Sintassi:

**IDIV *sorgente***

Operazione svolta:

**AX = DX:AX / sorgente**

**DX = DX:AX % sorgente**

**Se sorgente è a 8 bit: AL = AX / sorgente, AH = AX % sorgente**

Flag definiti:

**Nessuno**

Descrizione:

IDIV esegue la divisione come DIV, ma considera il dividendo e l’operando come numeri con segno. Il quoziente è arrotondato verso zero e il resto ha il segno del dividendo.

Il dividendo deve essere esteso con segno: con operando a 16 bit si usa CWD per estendere AX in DX:AX, con operando a 8 bit CBW per estendere AL in AX.

Gli errori sono quelli di DIV; il quoziente deve essere compreso tra –32768 e 32767 (tra –128 e 127 a 8 bit).

Esempi:

mov ax, -7

cwd

mov bx, 2

idiv bx // AX = -3, DX = -1
### IMUL – Moltiplicazione con segno

Sintassi:

**IMUL *sorgente***

Operazione svolta:

**DX:AX = AX \* sorgente**

**Se sorgente è a 8 bit: AX = AL \* sorgente**

Flag definiti:

**CF, OF**

Descrizione:

IMUL esegue la moltiplicazione come MUL, ma considera gli operandi come numeri con segno. CF e OF valgono 1 se il risultato non sta nella sola metà bassa (AX, o AL a 8 bit).

Esempi:

mov al, -3

mov bl, 20

imul bl // AX = -60
### INC – Incremento

Sintassi:

**INC *destinazione***

Operazione svolta:

**destinazione = destinazione + 1**

Flag definiti:

**ZF, SF, OF (CF non cambia)**

Descrizione:

INC incrementa di uno l’operando

Esempi:

inc ax // incrementa di 1 ax

inc [20] // incrementa di 1 il contenuto della locazione [20]

inc [si] // incrementa di 1 il contenuto della locazione puntata da si

### INT – Chiamata di un servizio di sistema

Sintassi:

**INT 21h**

Operazione svolta:

**esegue il servizio di sistema selezionato dal registro AH**

Flag definiti:

**Nessuno**

Descrizione:

INT 21h richiama un servizio di sistema, come nel sistema operativo DOS. Il servizio viene scelto in base al valore del registro AH; gli altri registri contengono i parametri e ricevono i risultati. L’unico numero di interrupt ammesso è 21h: un numero diverso produce l’errore «Numero di interrupt non valido», mentre un valore di AH non previsto produce l’errore «Servizio int 21h non valido (valore di AH)».

I caratteri letti e scritti compaiono nel pannello Console, che si apre automaticamente. Durante una lettura il programma resta in attesa finché l’utente non preme un tasto nel pannello Console.

| AH | Servizio | Parametri e risultati |
|---|---|---|
| 01h | Legge un carattere con eco | Il carattere letto va in AL e viene mostrato in console |
| 02h | Scrive un carattere | Il carattere da scrivere è in DL |
| 07h | Legge un carattere senza eco | Il carattere letto va in AL e non viene mostrato |
| 09h | Scrive una stringa | DX contiene l’indirizzo della stringa, che termina con il carattere «$» |
| 0Ah | Legge una riga | DX contiene l’indirizzo del buffer (vedi sotto) |
| 4Ch | Termina il programma | Equivale all’istruzione STOP |

Il tasto Invio corrisponde al codice 13 (CR). In scrittura, sia il codice 13 sia il codice 10 (LF) vanno a capo; la coppia 13, 10 usata nei programmi DOS produce un solo ritorno a capo.

Il buffer del servizio 0Ah ha questo formato: la prima cella contiene il numero massimo di caratteri, Invio compreso; nella seconda cella il servizio scrive il numero di caratteri letti, Invio escluso; dalla terza cella in poi vengono memorizzati i caratteri letti, seguiti dal codice 13. Raggiunto il massimo, gli altri caratteri vengono ignorati fino alla pressione di Invio. Il tasto Backspace cancella l’ultimo carattere digitato.

buf DB 20, ?, 20 DUP(?) // buffer per una riga di al massimo 19 caratteri

Come per le variabili DB, ogni carattere occupa il byte basso di una cella di memoria (un byte, nella memoria a byte).

Esempi:

mov ah, 2 // servizio: scrivi carattere

mov dl, 'A'

int 21h // scrive A in console

mov ah, 9 // servizio: scrivi stringa

mov dx, offset msg

int 21h // scrive la stringa msg

mov ah, 4Ch

int 21h // termina il programma

### JA – Salto se superiore (senza segno)

Sintassi:

**JA *etichetta***

Operazione svolta:

**se CF == 0 e ZF == 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JA salta se, dopo un confronto, il primo operando è maggiore del secondo considerando i numeri senza segno.

Sinonimi: JNBE.

Esempi:

mov ax, 0FFFFh

cmp ax, 1

ja salto // salta: 65535 > 1 (JG non salterebbe: -1 < 1)
### JAE – Salto se superiore o uguale (senza segno)

Sintassi:

**JAE *etichetta***

Operazione svolta:

**se CF == 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JAE salta se, dopo un confronto, il primo operando è maggiore o uguale al secondo considerando i numeri senza segno, cioè se non c’è stato prestito.

Sinonimi: JNB, JNC.

Esempi:

cmp ax, bx

jae salto
### JB – Salto se inferiore (senza segno)

Sintassi:

**JB *etichetta***

Operazione svolta:

**se CF == 1 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JB salta se, dopo un confronto, il primo operando è minore del secondo considerando i numeri senza segno. Come JC salta se c’è stato un riporto, ad esempio dopo ADD o uno shift.

Sinonimi: JNAE, JC.

Esempi:

cmp ax, bx

jb salto

shr ax, 1

jc dispari // il bit uscito era 1
### JBE – Salto se inferiore o uguale (senza segno)

Sintassi:

**JBE *etichetta***

Operazione svolta:

**se CF == 1 oppure ZF == 1 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JBE salta se, dopo un confronto, il primo operando è minore o uguale al secondo considerando i numeri senza segno.

Sinonimi: JNA.

Esempi:

cmp ax, bx

jbe salto
### JCXZ – Salto se CX è zero

Sintassi:

**JCXZ *etichetta***

Operazione svolta:

**se CX == 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JCXZ modifica il flusso di esecuzione delle istruzioni, assegnando al registro IP un nuovo indirizzo nella memoria delle istruzioni.

Esempi:

jcxz salto // se "cx = 0" IP punta alla istruzione designata da " salto "

...

salto: ...

### JE – Salto se uguale

Sintassi:

**JE *etichetta***

Operazione svolta:

**se ZF == 1 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JE assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni, ma soltanto se ZF è 1. JE è di norma impiegata dopo un’istruzione CMP, che confronta due operandi alterando lo stato dei flags.

Sinonimo: JZ.

Esempi:

cmp ax, bx

je salto // se "ax = bx" IP punta alla istruzione designata da " salto "

### JG – Salto se maggiore

Sintassi:

**JG *etichetta***

Operazione svolta:

**se SF == OF e ZF = 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JG assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni in base al risultato dell’ultima operazione. Se impiegata dopo l’istruzione di confronto CMP, JG produce il salto se il primo operando è maggior del secondo.

Sinonimo: JNLE. Confronta numeri con segno; per i numeri senza segno si usa JA.

Esempi:

cmp ax, bx

jg salto // se "ax > bx" IP punta alla istruzione designata da " salto "

### JGE – Salto se maggiore o uguale

Sintassi:

**JGE *etichetta***

Operazione svolta:

**se SF == OF esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JGE assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni in base al risultato dell’ultima operazione. Se impiegata dopo l’istruzione di confronto CMP, JGE produce il salto se il primo operando è maggiore o uguale al secondo.

Sinonimo: JNL. Confronta numeri con segno; per i numeri senza segno si usa JAE.

Esempi:

cmp ax, bx

jge salto // se "ax >= bx" IP punta alla istruzione designata da " salto "

### JL – Salto se minore

Sintassi:

**JL *etichetta***

Operazione svolta:

**se SF != OF esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JL assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni in base al risultato dell’ultima operazione. Se impiegata dopo l’istruzione di confronto CMP, JL produce il salto se il primo operando è minore del secondo.

Sinonimo: JNGE. Confronta numeri con segno; per i numeri senza segno si usa JB.

Esempi:

cmp ax, bx

jl salto // se "ax < bx" IP punta alla istruzione designata da " salto "

### JLE – Salto se minore o uguale

Sintassi:

**JLE *etichetta***

Operazione svolta:

**se SF != OF o ZF == 1 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JLE assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni in base al risultato dell’ultima operazione. Se impiegata dopo l’istruzione di confronto CMP, JLE produce il salto se il primo operando è minore o uguale al secondo.

Sinonimo: JNG. Confronta numeri con segno; per i numeri senza segno si usa JBE.

Esempi:

cmp ax, bx

jle salto // se "ax <= bx" IP punta alla istruzione designata da " salto "

### JMP – Salto incondizionato

Sintassi:

**JMP *etichetta***

**JMP *sorgente***

Operazione svolta:

**IP = indirizzo designato dall’etichetta, oppure valore di sorgente**

Flag definiti:

**Nessuno**

Descrizione:

JMP modifica il flusso di esecuzione delle istruzioni, assegnando al registro IP un nuovo indirizzo nella memoria delle istruzioni.

Esempio:

jmp salto // IP punta alla istruzione designata da " salto "

salto: ...

Nella forma indiretta la destinazione viene letta durante l’esecuzione: *sorgente* può essere un registro a 16 bit o una cella di memoria, con qualsiasi modo di indirizzamento. Il valore è il numero dell’istruzione di destinazione, ottenuto con offset *etichetta* nel codice o con il nome dell’etichetta nella sezione dati:

mov bx, offset fine

jmp bx // salta a "fine"

jmp [tab+bx] // salta all’indirizzo contenuto nella cella tab + BX

jmp dest // dest variabile DW: come jmp [dest]

Una tabella di salto (tab DW caso0, caso1, caso2) realizza una scelta multipla con un solo salto, al posto di una catena di CMP e JE. Solo JMP e CALL hanno la forma indiretta: i salti condizionati, LOOP e JCXZ accettano soltanto un’etichetta. Non sono ammessi una costante (per un salto diretto si scrive l’etichetta), un registro a 8 bit o una variabile DB. Una destinazione che non corrisponde a un’istruzione del programma produce l’errore di esecuzione «Registro IP non indirizza un'istruzione».

Nella memoria a byte ogni elemento di una tabella DW occupa due byte: l’indice va moltiplicato per 2 prima del salto (shl bx, 1 seguito da jmp [tab+bx]).

### JNE – Salto se diverso

Sintassi:

**JNE *etichetta***

Operazione svolta:

**se ZF == 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JNE assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni, ma soltanto se ZF è 0. Essa viene di norma impiegata dopo un’istruzione CMP, che confronta due operandi alterando lo stato dei flags.

Sinonimo: JNZ.

Esempi:

cmp ax, bx

jne salto // se "ax != bx" IP punta alla istruzione designata da " salto "

### JNO – Salto se non overflow

Sintassi:

**JNO *etichetta***

Operazione svolta:

**se OF == 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JNO assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni, ma soltanto se l’ultima operazione non ha prodotto un overflow.

Esempi:

add ax, bx

jno salto // se "non overflow " IP punta alla istruzione designata da "salto"

### JNS – Salto se il flag di segno è 0

Sintassi:

**JNS *etichetta***

Operazione svolta:

**se SF == 0 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JNS assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni, ma soltanto se l’ultima operazione ha dato un risultato non negativo.

Esempi:

sub ax, bx

jns salto // se "ax >= 0" IP punta alla istruzione designata da "salto"

### JO – Salto se overflow

Sintassi:

**JO *etichetta***

Operazione svolta:

**se OF == 1 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JO assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni, ma soltanto se l’ultima operazione ha prodotto un overflow.

Esempi:

add ax, bx

jo salto // se "overflow " IP punta alla istruzione designata da "salto"

### JS – Salto se il flag di segno è 1

Sintassi:

**JS *etichetta***

Operazione svolta:

**se SF == 1 esegue:**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JS assegna al registro IP un nuovo indirizzo nella memoria delle istruzioni, ma soltanto se l’ultima operazione ha dato un risultato negativo.

Esempi:

sub ax, bx

js salto // se "ax < 0" IP punta alla istruzione designata da "salto"

### LEA – Caricamento dell’indirizzo effettivo

Sintassi:

**LEA *registro*, *memoria***

Operazione svolta:

**registro = indirizzo dell’operando in memoria**

Flag definiti:

**Nessuno**

Descrizione:

LEA calcola l’indirizzo dell’operando in memoria e lo memorizza nel registro, senza leggere la memoria. La destinazione deve essere un registro a 16 bit e la sorgente un operando in memoria (diretto, indiretto o una variabile): lea si, vet equivale a mov si, offset vet.

Esempi:

lea si, vet // SI = indirizzo di vet

lea di, [si+3] // DI = SI + 3
### LODSB, LODSW – Lettura di un elemento di una sequenza

Sintassi:

**LODSB**

**LODSW**

Operazione svolta:

**AL (o AX) = [SI]**

**SI = SI ± passo**

Flag definiti:

**Nessuno**

Descrizione:

LODSB copia in AL il byte puntato da SI (il byte basso della cella, nella memoria a parole), LODSW copia in AX la parola; poi aggiornano SI. Insieme a STOSB e STOSW servono a scorrere una sequenza elaborandone gli elementi.

SI e DI avanzano se DF = 0 (dopo CLD) o arretrano se DF = 1 (dopo STD) di un elemento: è il «passo» indicato nell’operazione svolta. Nella memoria a parole l’elemento è sempre una cella: la forma con suffisso B usa il byte basso delle celle e il registro AL, la forma con suffisso W la cella intera e il registro AX. Nella memoria a byte, come su x86, la forma B avanza di 1 byte e la forma W di 2. Con il prefisso REP l’istruzione viene ripetuta (vedi «REP, REPE, REPNE»).

Esempi:

mov si, offset msg

lodsb // AL = primo carattere di msg, SI avanza
### LOOP, LOOPE, LOOPNE – Cicli con contatore

Sintassi:

**LOOP *etichetta***

**LOOPE *etichetta***

**LOOPNE *etichetta***

Operazione svolta:

**CX = CX - 1**

**LOOP: se CX != 0 esegue IP = indirizzo designato dall’etichetta**

**LOOPE: se CX != 0 e ZF == 1 salta**

**LOOPNE: se CX != 0 e ZF == 0 salta**

Flag definiti:

**Nessuno**

Descrizione:

LOOP decrementa CX e salta all’etichetta se CX non è diventato zero: ripete quindi un ciclo tante volte quanto il valore iniziale di CX. Il decremento non modifica i flag. Se CX vale 0 prima del LOOP, il decremento lo porta a 65535 e il ciclo viene ripetuto 65536 volte.

LOOPE (sinonimo LOOPZ) salta solo se, oltre a CX diverso da zero, ZF vale 1; LOOPNE (sinonimo LOOPNZ) solo se ZF vale 0. Servono ad esempio per cercare un valore in un vettore, uscendo dal ciclo quando il confronto ha successo.

Esempi:

mov cx, 5

ciclo: add ax, cx

loop ciclo // ripete 5 volte

cerca: inc si

cmp [vet+si], 7

loopne cerca // continua finché diverso da 7
### MOV – Trasferimento

Sintassi:

**MOV *destinazione*, *sorgente***

Operazione svolta:

**destinazione = sorgente**

Flag definiti:

**Nessuno**

Descrizione:

MOV copia il contenuto dell’operando sorgente nell’operando destinazione.

Esempi:

mov ax, bx // copia in ax il contenuto di bx

mov [10], 2 // copia nella locazione 10 il valore 2

### MOVS, MOVSB, MOVSW – Copia di un elemento di una sequenza

Sintassi:

**MOVSB**

**MOVSW**

**MOVS**

Operazione svolta:

**[DI] = [SI]**

**SI = SI ± passo, DI = DI ± passo**

Flag definiti:

**Nessuno**

Descrizione:

MOVSB e MOVSW copiano l’elemento puntato da SI nella cella puntata da DI, poi aggiornano SI e DI. MOVS equivale a MOVSW. Come su x86 è l’istruzione per copiare da memoria a memoria (nella memoria a byte è l’unica ammessa, vedi la nota in «Modelli di indirizzamento»); con REP copia un intero vettore.

SI e DI avanzano se DF = 0 (dopo CLD) o arretrano se DF = 1 (dopo STD) di un elemento: è il «passo» indicato nell’operazione svolta. Nella memoria a parole l’elemento è sempre una cella: la forma con suffisso B usa il byte basso delle celle e il registro AL, la forma con suffisso W la cella intera e il registro AX. Nella memoria a byte, come su x86, la forma B avanza di 1 byte e la forma W di 2. Con il prefisso REP l’istruzione viene ripetuta (vedi «REP, REPE, REPNE»).

Esempi:

mov si, 10

mov di, 0

movsw // memoria a parole: memoria[0] = memoria[10]; SI = 11, DI = 1

mov cx, 5

rep movsw // copia 5 parole

### MUL – Moltiplicazione senza segno

Sintassi:

**MUL *sorgente***

Operazione svolta:

**DX:AX = AX \* sorgente**

**Se sorgente è a 8 bit: AX = AL \* sorgente**

Flag definiti:

**CF, OF**

Descrizione:

MUL moltiplica il registro AX per l’operando, considerando entrambi numeri senza segno (da 0 a 65535), e memorizza il risultato a 32 bit nella coppia di registri DX:AX. CF e OF valgono 1 se DX contiene cifre significative, cioè se il risultato non sta in AX.

Se l’operando è a 8 bit (un registro a 8 bit, una variabile DB o un operando byte ptr), MUL moltiplica AL per l’operando (da 0 a 255) e memorizza il risultato in AX; CF e OF valgono 1 se il risultato non sta in AL.

Per moltiplicare numeri con segno si usa IMUL.

Esempi:

mul 2 // moltiplica ax per 2

mul [20] // moltiplica ax per il contenuto della locazione [20]

mul [si] // moltiplica ax per il contenuto della locazione puntata da si

mul bl // moltiplica al per bl, risultato in ax

mov al, 0FFh

mov bl, 2

mul bl // AX = 255 * 2 = 510 (con IMUL: -1 * 2 = -2)

### NEG – Negazione (formazione del complemento a 2)

Sintassi:

**NEG *destinazione***

Operazione svolta:

**destinazione = 0 - destinazione**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

NEG sottrae l’operando a 0, ottenendo così il suo complemento a 2. CF vale 1, tranne quando l’operando è 0.

Esempi:

mov ax, 2

neg ax // equivale a ax = 0 – ax, che produce: -2 (FFFE in esadecim.)

### NOP – Nessuna operazione

Sintassi:

**NOP**

Operazione svolta:

**Nessuna**

Flag definiti:

**Nessuno**

Descrizione:

NOP non fa compiere alla CPU nessuna azione. Il suo impiego è normalmente quello di garantire la correttezza formale del programma, laddove la modalità di implementazione dell’algoritmo richiede comunque una istruzione senza che però sia necessario che la CPU compia alcuna azione.

### NOT – Negazione logica (formazione del complemento a 1)

Sintassi:

**NOT *destinazione***

Operazione svolta:

**destinazione = complemento a 1 di destinazione**

Flag definiti:

**Nessuno**

Descrizione:

NOT inverte ogni bit dell’operando, producendo così il complemento a 1 dello stesso.

Esempi:

mov ax, 2

not ax // produce: -3 (FFFD in esadecim.)

### OR – Somma logica

Sintassi:

**OR *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione | sorgente**

Flag definiti:

**SF, ZF; OF = 0, CF = 0**

Descrizione:

OR imposta a 1 i bit del risultato se almeno uno dei bit corrispondenti dei due operandi è 1; altrimenti li imposta a 0. Il risultato è memorizzato nell’operando destinazione.

Esempio:

mov ax, 2

or ax, 4 // produce come risultato: 6

### POP – Prelevamento dallo stack

Sintassi:

**POP *destinazione***

Operazione svolta:

**destinazione = valore che si trova in testa allo stack**

**SP = SP + 1 (SP + 2 nella memoria a byte)**

Flag definiti:

**Nessuno**

Descrizione:

POP copia in destinazione il contenuto della zona di memoria puntata da SP (testa dello stack). Dopodiché incrementa SP, in modo che punti al nuovo valore in testa allo stack.

Esempi:

pop ax // copia in ax il valore memorizzato in testa allo stack

pop [10] // copia nella locazione [10] il valore situato in testa allo stack

### POPF – Prelevamento del registro dei flags dallo stack

Sintassi:

**POPF**

Operazione svolta:

**flags = valore che si trova in testa allo stack**

**SP = SP + 1 (SP + 2 nella memoria a byte)**

Flag definiti:

**CF, SF, ZF, OF**

Descrizione:

POPF copia nel registro dei flags il contenuto della zona di memoria puntata da SP (testa dello stack). Dopodiché incrementa SP, in modo che punti al nuovo valore in testa allo stack. Si presuppone che in precedenza il registro dei flags fosse stato copiato nello stack mediante l’istruzione PUSHF.

Esempi:

popf // copia nel registro dei flags il valore memorizzato in testa allo stack

### PUSH – Deposito di un valore nello stack

Sintassi:

**PUSH *sorgente***

Operazione svolta:

**SP = SP - 1 (SP - 2 nella memoria a byte)**

**sorgente viene memorizzato in testa allo stack**

Flag definiti:

**Nessuno**

Descrizione:

PUSH, prima decrementa SP quindi copia l’operando nella zona di memoria puntata da SP (testa dello stack).

Esempi:

push ax // copia ax in testa allo stack

push [10] // copia il contenuto della locazione [10] in testa allo stack

### PUSHF – Deposito del registro dei flags nello stack

Sintassi:

**PUSHF**

Operazione svolta:

**SP = SP - 1 (SP - 2 nella memoria a byte)**

**il registro dei flags viene memorizzato in testa allo stack**

Flag definiti:

**Nessuno**

Descrizione:

PUSHF, prima decrementa SP quindi copia il registro dei flags nella zona di memoria puntata da SP (testa dello stack). Si presuppone che il registro venga successivamente ripristinato mediante l’istruzione POPF.

Esempi:

pushf // copia il registro dei flags in testa allo stack

### RCL, RCR – Rotazione attraverso il carry

Sintassi:

**RCL *destinazione*, *contatore***

**RCR *destinazione*, *contatore***

Operazione svolta:

**ruota i bit dell’operando insieme a CF, a sinistra (RCL) o a destra (RCR)**

Flag definiti:

**CF; OF se contatore = 1**

Descrizione:

RCL e RCR ruotano i bit dell’operando considerando CF come un bit in più: il bit che esce va in CF e il vecchio valore di CF entra dall’altra parte. Servono a spostare bit tra registri, ad esempio negli shift di numeri a 32 bit. Per il contatore valgono le regole di SHL.

Esempi:

clc

mov al, 80h

rcl al, 1 // AL = 00h, CF = 1

rcl al, 1 // AL = 01h, CF = 0
### REP, REPE, REPNE – Prefissi di ripetizione

Sintassi:

**REP *istruzione stringa***

**REPE *CMPSB, CMPSW, SCASB, SCASW***

**REPNE *CMPSB, CMPSW, SCASB, SCASW***

Operazione svolta:

**se CX == 0 l’istruzione non viene eseguita**

**altrimenti: esegue l’istruzione, CX = CX - 1**

**e la ripete finché CX != 0 (REPE: e ZF == 1; REPNE: e ZF == 0)**

Flag definiti:

**quelli dell’istruzione ripetuta**

Descrizione:

Un prefisso si scrive sulla stessa riga dell’istruzione stringa e la fa ripetere al massimo CX volte. REP si usa con tutte le istruzioni stringa; con CMPS e SCAS equivale a REPE. REPE (sinonimo REPZ) continua finché il confronto dà «uguale», REPNE (sinonimo REPNZ) finché dà «diverso». Alla fine CX contiene il numero di ripetizioni non eseguite.

Durante l’esecuzione passo passo, ogni Step esegue una sola ripetizione e IP resta sulla riga del prefisso finché la ripetizione non termina: si possono così osservare SI, DI e CX cambiare. Un breakpoint sulla riga ferma l’esecuzione soltanto all’arrivo, non a ogni ripetizione.

Usare REPE o REPNE con istruzioni diverse da CMPS e SCAS, o REP con un’istruzione non stringa, produce l’errore «Prefisso non valido».

Esempi:

mov cx, 10

rep stosw // scrive AX in 10 parole

mov cx, 100

repne scasb // cerca AL nella stringa puntata da DI
### RET – Ritorno da una procedura

Sintassi:

**RET**

**RET *n***

Operazione svolta:

**IP viene prelevato dallo stack**

**SP = SP + 1 (SP + 2 nella memoria a byte)**

**con RET n: SP = SP + n**

Flag definiti:

**Nessuno**

Descrizione:

RET modifica il flusso di esecuzione delle istruzioni, prelevando IP dallo stack e quindi assegnandogli un nuovo indirizzo nella memoria delle istruzioni. Si presuppone che RET termini una procedura che sia stata precedentemente avviata da un’istruzione CALL.

La forma RET n, dove n è una costante, dopo il ritorno somma n a SP: la procedura elimina così i parametri che il chiamante vi aveva depositato, invece di lasciarlo fare al chiamante con ADD SP, n. Nella memoria a parole n è il numero di parole da rimuovere; nella memoria a byte, come su x86, è in byte (ret 4 rimuove due parametri). Se lo stack contiene meno di n unità si produce l’errore «Stack underflow».

Esempi:

call ciclo // IP punta alla istruzione designata da "ciclo"

...

ciclo: *<inizio della procedura>*

*...*

ret // ritorno all’istruzione successiva a CALL

ret 2 // ritorno e rimozione di due parametri dallo stack (memoria a parole)

### ROL, ROR – Rotazione

Sintassi:

**ROL *destinazione*, *contatore***

**ROR *destinazione*, *contatore***

Operazione svolta:

**ruota i bit dell’operando a sinistra (ROL) o a destra (ROR)**

Flag definiti:

**CF; OF se contatore = 1**

Descrizione:

ROL e ROR ruotano i bit dell’operando: il bit che esce da una parte rientra dall’altra e viene copiato anche in CF. Nessun bit va perso. Per il contatore valgono le regole di SHL.

Esempi:

mov ax, 1234h

rol ax, 4 // AX = 2341h

ror ax, 4 // AX = 1234h
### SAR – Shift aritmetico a destra

Sintassi:

**SAR *destinazione*, *contatore***

Operazione svolta:

**sposta a destra i bit dell’operando, ripetendo il bit di segno**

Flag definiti:

**SF, ZF, CF; OF = 0 se contatore = 1**

Descrizione:

SAR sposta a destra i bit dell’operando come SHR, ma nel bit più a sinistra ripete il bit di segno: il numero conserva il segno. Ogni spostamento equivale a una divisione per 2 di un numero con segno, arrotondata verso il basso. Per il contatore valgono le regole di SHL.

Esempi:

mov ax, -8

sar ax, 1 // AX = -4 (con SHR: 32764)
### SCASB, SCASW – Ricerca in una sequenza

Sintassi:

**SCASB**

**SCASW**

Operazione svolta:

**AL (o AX) - [DI], senza memorizzare il risultato**

**DI = DI ± passo**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

SCASB e SCASW confrontano AL (o AX) con l’elemento puntato da DI come farebbe CMP, poi aggiornano DI. Con REPNE cercano un valore in una sequenza: alla fine ZF vale 1 se il valore è stato trovato e DI punta all’elemento successivo.

SI e DI avanzano se DF = 0 (dopo CLD) o arretrano se DF = 1 (dopo STD) di un elemento: è il «passo» indicato nell’operazione svolta. Nella memoria a parole l’elemento è sempre una cella: la forma con suffisso B usa il byte basso delle celle e il registro AL, la forma con suffisso W la cella intera e il registro AX. Nella memoria a byte, come su x86, la forma B avanza di 1 byte e la forma W di 2. Con il prefisso REP l’istruzione viene ripetuta (vedi «REP, REPE, REPNE»).

Esempi:

mov di, offset msg

mov al, '$'

mov cx, 100

repne scasb // cerca il carattere $
### SBB – Sottrazione con prestito

Sintassi:

**SBB *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione - sorgente - CF**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

SBB sottrae dall’operando destinazione l’operando sorgente e il valore del flag CF. Si usa per sottrarre numeri più grandi di un registro: si sottraggono prima le parti basse con SUB, poi le parti alte con SBB, che tiene conto del prestito.

Esempi:

sub ax, bx // parti basse

sbb dx, cx // parti alte meno il prestito: DX:AX = DX:AX - CX:BX
### SHL – Shift logico a sinistra

Sintassi:

**SHL *destinazione*, *contatore***

Operazione svolta:

**sposta a sinistra i bit dell’operando, impostando a zero il bit di ordine inferiore (bit più a destra)**

Flag definiti:

**SF, ZF, CF; OF se contatore = 1**

Descrizione:

SHL sposta a sinistra i bit dell’operando destinazione per un numero di posizioni pari all’operando contatore. A ogni spostamento, nel bit più a destra viene memorizzato il valore zero, mentre il bit più a sinistra esce e va in CF. Ogni spostamento equivale a una moltiplicazione per 2.

Il contatore può essere una costante o un registro (tipicamente CL); come nei microprocessori X86 se ne usano soltanto i 5 bit bassi (da 0 a 31). Con contatore 0 i flag non cambiano. OF è definito solo per contatore 1: vale 1 se il bit di segno è cambiato.

Esempi:

mov ax, 2

shl ax, 3 // produce: 16

mov ax, 8000h

shl ax, 1 // produce: 0, con CF = 1

### SHR – Shift logico a destra

Sintassi:

**SHR *destinazione*, *contatore***

Operazione svolta:

**sposta a destra i bit dell’operando, impostando a zero il bit di ordine superiore (bit più a sinistra)**

Flag definiti:

**SF, ZF, CF; OF se contatore = 1**

Descrizione:

SHR sposta a destra i bit dell’operando destinazione per un numero di posizioni pari all’operando contatore. A ogni spostamento, nel bit più a sinistra viene memorizzato il valore zero, mentre il bit più a destra esce e va in CF. Ogni spostamento equivale a una divisione per 2 di un numero senza segno; per i numeri con segno si usa SAR.

Per il contatore valgono le regole di SHL. OF, per contatore 1, riceve il bit di segno originale.

Esempi:

mov ax, 4

shr ax, 2 // produce: 1

mov ax, -8

shr ax, 1 // produce: 7FFCh (32764): entra uno zero

### STC – Impostazione del carry

Sintassi:

**STC**

Operazione svolta:

**CF = 1**

Flag definiti:

**CF**

Descrizione:

STC imposta a 1 il flag CF.

Esempi:

stc
### STD – Impostazione del flag di direzione

Sintassi:

**STD**

Operazione svolta:

**DF = 1**

Flag definiti:

**DF**

Descrizione:

STD imposta a 1 il flag DF: le istruzioni stringa faranno arretrare SI e DI. Serve ad esempio per copiare un vettore su una zona sovrapposta partendo dal fondo.

Esempi:

std
### STOP – Arresta la CPU

Sintassi:

**STOP**

Operazione svolta:

**Arresta la CPU, terminando l’esecuzione del programma**

Flag definiti:

**Nessuno**

Descrizione:

STOP determina l’immediato arresto dell’esecuzione del programma.

### STOSB, STOSW – Scrittura di un elemento di una sequenza

Sintassi:

**STOSB**

**STOSW**

Operazione svolta:

**[DI] = AL (o AX)**

**DI = DI ± passo**

Flag definiti:

**Nessuno**

Descrizione:

STOSB scrive AL nel byte puntato da DI (il byte basso della cella, nella memoria a parole), STOSW scrive AX nella parola; poi aggiornano DI. Con REP riempiono una zona di memoria con lo stesso valore.

SI e DI avanzano se DF = 0 (dopo CLD) o arretrano se DF = 1 (dopo STD) di un elemento: è il «passo» indicato nell’operazione svolta. Nella memoria a parole l’elemento è sempre una cella: la forma con suffisso B usa il byte basso delle celle e il registro AL, la forma con suffisso W la cella intera e il registro AX. Nella memoria a byte, come su x86, la forma B avanza di 1 byte e la forma W di 2. Con il prefisso REP l’istruzione viene ripetuta (vedi «REP, REPE, REPNE»).

Esempi:

mov ax, 0

mov di, offset tab

mov cx, 8

rep stosw // azzera 8 parole
### SUB – Sottrazione

Sintassi:

**SUB *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione - sorgente**

Flag definiti:

**SF, ZF, OF, CF**

Descrizione:

L’operando sorgente viene sottratto all’operando destinazione; il risultato viene memorizzato nell’operando destinazione.

Esempi:

sub ax, bx // equivale a: ax = ax - bx

sub [10], 2 // equivale a: [10] = [10] - 2

### TEST – Test logico

Sintassi:

**TEST *destinazione*, *sorgente***

Operazione svolta:

**destinazione & sorgente, senza memorizzare il risultato**

Flag definiti:

**SF, ZF; OF = 0, CF = 0**

Descrizione:

TEST esegue un AND tra i due operandi senza modificarli, aggiornando soltanto i flag: sta ad AND come CMP sta a SUB. Si usa per controllare il valore di uno o più bit.

Esempi:

test ax, 1

jz pari // salta se il bit 0 di AX è 0
### XCHG – Scambio

Sintassi:

**XCHG *operando1*, *operando2***

Operazione svolta:

**scambia il contenuto dei due operandi**

Flag definiti:

**Nessuno**

Descrizione:

XCHG scambia il contenuto di due registri, o di un registro e di una locazione di memoria, senza bisogno di un registro d’appoggio. Gli operandi devono avere la stessa dimensione; non sono ammesse costanti né due operandi in memoria.

Esempi:

xchg ax, bx

xchg cl, ch // scambia i due byte di CX

xchg dx, [10]
### XOR – Or esclusivo

Sintassi:

**XOR *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione ^ sorgente**

Flag definiti:

**SF, ZF; OF = 0, CF = 0**

Descrizione:

XOR imposta a 1 i bit del risultato se entrambi i bit corrispondenti dei due operandi sono diversi tra loro; altrimenti li imposta a 0. Il risultato è memorizzato nell’operando destinazione.

Esempio:

mov ax, 2

xor ax, 4 // produce come risultato: 6

## Struttura di un programma assembly

I programmi assembly compatibili con EasyCPU sono suddivisi in due sezioni, «codice» e «dati». La sezione codice contiene le istruzioni in linguaggio assembly e comincia con l’inizio del file. La parte dati, opzionale, è preceduta dalla direttiva «.DATA» e consente di definire variabili e costanti e di inizializzare il contenuto di una o più celle di memoria (vedi «La sezione dati»). All’interno dell’IDE di EasyCPU, le due sezioni vengono gestite mediante due editor separati. Un programma può inoltre contenere delle righe di commento, prefissate dal simbolo “//” oppure “;”. Un commento può anche seguire il testo di un’istruzione.

Di seguito viene riportato un programma di esempio che calcola la somma degli elementi dispari all’interno di un sequenza. All’indirizzo 0 di memoria è memorizzato il numero di elementi, all’indirizzo 1 l’indirizzo di partenza della sequenza, che nella sezione dati è definita a partire dall’indirizzo 2. Lo stesso programma è disponibile in «Docs/samples/07-programmi/programma-somma-dispari.asj».

// Programma SommaDispari

// Calcola la somma degli elementi dispari di un vettore e la memorizz in BX

mov bx, 0 // bx memorizza la somma

mov cx, [0] // memorizza in cx il numero degli elementi

mov si, [1] // si memorizza l'indirizzo del vettore

ciclo: cmp cx, 0 // verifica se il ciclo è finito

je fine

mov ax,[si] // preleva l'elemento

mov dx, 0 // non obbligatoria

div 2 // divide per 2 per ottenere il resto in dx

cmp dx, 0 // se dx è 0 il numero è pari

je pari

add bx, [si] // somma l'elemento

pari:

inc si

dec cx

jmp ciclo

fine:

stop

.DATA

0: 5

1: 2

2: 1, 3, 4, 6, 7

## La sezione dati

La sezione dati è composta da righe di questi tipi:

| Forma | Significato |
|---|---|
| nome **DW** elemento, elemento, … | Variabile di parole (16 bit) |
| nome **DB** elemento, elemento, … | Variabile di byte (8 bit) |
| nome **EQU** valore | Costante simbolica: non occupa memoria |
| **ORG** indirizzo | Le variabili successive partono dall’indirizzo indicato |
| indirizzo: valore, valore, … | Scrive i valori a partire dall’indirizzo indicato (forma originale; non ammessa nella memoria a byte) |

Le variabili DB e DW vengono collocate in memoria una dopo l’altra, a partire dall’indirizzo 0 o dall’ultimo ORG. Il nome è facoltativo: una riga DB o DW senza nome occupa comunque le celle, ad esempio per proseguire la variabile precedente. Le variabili non possono occupare l’area dello stack (indirizzi da 240 a 255; da 448 a 511 nella memoria a byte).

Un elemento può essere:

- un valore numerico o carattere, oppure il nome di una costante EQU;
- «?», che indica una cella non inizializzata (vale 0);
- una stringa tra apici, come 'Ciao': ogni carattere occupa una cella (solo con DB);
- «n DUP(elemento)», che ripete l’elemento n volte;
- «offset nome», l’indirizzo di un’altra variabile;
- il nome di un’etichetta del codice, che vale il numero della sua istruzione: serve a costruire tabelle di salto (tab DW caso0, caso1) per JMP e CALL indiretti.

I valori di una variabile DB devono essere compresi tra –128 e 255, quelli di una variabile DW tra –32768 e 65535. Nella memoria a parole le celle restano di 16 bit: una variabile DB usa soltanto il byte basso di ogni cella. Nella memoria a byte ogni valore DB occupa un byte e ogni valore DW due byte (vedi «Modalità x86 fedele: memoria a byte»).

N EQU 5 // costante

conta DW 0 // una parola, indirizzo 0

vet DW 1, 3, 4, 6, 7 // cinque parole, indirizzi 1..5

buffer DW N DUP(0) // cinque parole a zero, indirizzi 6..10

msg DB 'Ciao mondo!$' // una cella per carattere

ORG 100

tabella DW N DUP(?) // cinque parole a partire dall'indirizzo 100

Gli esempi indicano gli indirizzi della memoria a parole. I nomi non distinguono maiuscole e minuscole e non possono coincidere con un registro, un’istruzione, una parola riservata (DB, DW, EQU, ORG, DUP, OFFSET) o un’etichetta del codice.

### Uso dei nomi nel codice

EasyCPU segue la convenzione dell’assemblatore MASM:

mov cx, N // costante EQU: CX = 5

mov ax, conta // contenuto della variabile: come mov ax, [conta]

mov ax, [conta] // contenuto della variabile

mov si, offset vet // indirizzo della variabile: SI = 1

mov ax, vet+2 // contenuto della cella vet + 2: come mov ax, [vet+2]

mov ax, [vet+si] // contenuto della cella di indirizzo vet + SI

mov ax, [bp+N] // un nome EQU può essere uno scostamento

mov bx, offset fine // indirizzo di un’etichetta: il numero della sua istruzione

Nel codice l’indirizzo di un’etichetta si scrive sempre con offset: mov bx, fine produce un errore che suggerisce offset fine.

La dimensione di un accesso a una variabile dipende dal suo tipo: una variabile DB si usa con registri a 8 bit (mov al, msg) e una variabile DW con registri a 16 bit (mov ax, conta). Istruzioni come mov ax, msg o push msg (con msg di tipo DB) producono l’errore «Dimensione degli operandi non valida o non coerente». Anche un’istruzione con una sola variabile, come inc conta, opera sulla dimensione della variabile.

Il pannello Memoria mostra, sotto il contenuto della memoria, l’elenco dei nomi definiti con indirizzo, tipo, numero di elementi e valore corrente del primo elemento; in fondo all’elenco compaiono le etichette del codice con il numero della loro istruzione.

### Esempio: Hello world

// sezione dati

msg DB 'Ciao mondo!', 13, 10, '$'

// sezione codice

mov ah, 9 // servizio: scrivi stringa

mov dx, offset msg

int 21h

mov ah, 4Ch // servizio: termina il programma

int 21h

## Modalità x86 fedele: memoria a byte

Nella modalità predefinita la memoria di EasyCPU è formata da 256 celle da 16 bit: ogni indirizzo contiene una parola intera e due indirizzi consecutivi non si sovrappongono. È un modello semplice, ma diverso da quello dei microprocessori X86, dove la memoria è formata da byte e una parola occupa due byte consecutivi. La modalità x86 fedele adotta il modello X86.

### Come si sceglie

La modalità è una proprietà del programma: si attiva con la voce «Memoria a byte (x86)» del menu Esegui e viene salvata nel file .asj. Un programma nuovo, un file .asj senza l’indicazione e un file .as usano la memoria a parole. Quando la modalità è attiva il pannello Memoria si intitola «Memoria (byte)»; cambiarla azzera la CPU e richiede una nuova compilazione.

### Il modello di memoria

| Aspetto | Memoria a parole | Memoria a byte |
|---|---|---|
| Dimensione | 256 celle da 16 bit | 512 byte |
| Parola all’indirizzo a | cella a | byte a (basso) e a+1 (alto) |
| Accesso a 8 bit | byte basso della cella | il byte all’indirizzo |
| Area dello stack | celle 240..255 (16 parole) | byte 448..511 (32 parole) |
| SP iniziale | 256 | 512 |
| PUSH, POP, CALL, RET | SP varia di 1 | SP varia di 2 |
| RET n | n parole | n byte |
| Istruzioni stringa | passo di 1 cella | passo di 1 (forme B) o 2 (forme W) |
| DB, DW, stringhe | una cella per valore | 1 byte per valore DB o carattere, 2 byte per valore DW |

Le parole sono memorizzate con il byte basso all’indirizzo più basso (ordine little-endian, come su X86):

mov word ptr [10], 1234h // byte 10 = 34h, byte 11 = 12h

mov al, [11] // AL = 12h

mov ax, [11] // AX = parola formata dai byte 11 e 12

Un accesso a parola che esce dalla memoria (ad esempio all’indirizzo 511) produce l’errore «Violazione dei limiti della memoria».

### Regole in più rispetto alla memoria a parole

Nella memoria a byte il compilatore applica tre regole dei microprocessori X86:

- la dimensione di un operando in memoria dev’essere determinata da un registro, dal tipo di una variabile o da byte ptr / word ptr: inc [si], mov [di], 5 e mul [bx] producono l’errore «Dimensione dell'operando non specificata: usare byte ptr o word ptr». Fanno eccezione PUSH, POP, JMP e CALL, che lavorano sempre a parole, e LEA, che non accede alla memoria;
- un’istruzione non può avere due operandi in memoria: mov a, b e add [10], [si] producono l’errore «Un'istruzione non può avere due operandi in memoria: usare un registro»; per copiare da memoria a memoria si usano un registro o le istruzioni stringa;
- nella sezione dati la forma «indirizzo: valori», che non indica la dimensione dei valori, non è ammessa: si usano DB e DW.

### Differenze nei programmi

Gli elementi di un vettore di parole distano 2 byte: l’indice avanza di 2.

vet DW 3, 8, 1 // sezione dati: vet occupa i byte 0..5

mov si, 0

mov cx, 3

ciclo: add ax, [vet+si]

add si, 2 // parola successiva

loop ciclo

Anche i parametri di una procedura distano 2 byte: dopo push bp e mov bp, sp l’indirizzo di ritorno è in [bp+2] e i parametri in [bp+4], [bp+6]; ret 4 rimuove due parametri.

somma: push bp

mov bp, sp

mov ax, [bp+4] // ultimo parametro depositato

add ax, [bp+6]

pop bp

ret 4

Una tabella di salto contiene parole: l’indice si moltiplica per 2 prima di jmp [tab+bx] o call [proc+bx].

mov bl, scelta

mov bh, 0

shl bx, 1 // due byte per elemento

jmp [tab+bx]

Gli esempi della cartella Docs/samples/12-memoria-byte mostrano questi casi e gli errori di compilazione della modalità.

### Pannelli

Il pannello Memoria (byte) mostra 16 byte per riga, con valori da 0 a 255 (da 00 a FF in esadecimale); il pannello Stack mostra una parola per riga, all’indirizzo del suo byte basso; nella sezione Simboli il valore di una variabile DB è il suo primo byte, quello di una variabile DW la sua prima parola.
