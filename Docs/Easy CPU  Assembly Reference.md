EasyCPU

Assembly reference

|  |  |
| --- | --- |
| ![](data:image/x-emf;base64...) | **Paolo Meozzi**  **Stefano Del Furia** |

Premessa 5

Struttura della CPU 6

Artimetica 6

Memoria dati 6

Registri 6

Modelli di indirizzamento 7

Gestione dello stack 8

Flags 9

Set di istruzioni 10

Descrizione generale delle istruzioni 10

Modalità di descrizione delle istruzioni 10

ADD – Addizione 11

AND – Moltiplicazione logica 11

CALL – Chiamata di una procedura 12

CMP – Confronto 12

DEC – Decremento 13

DIV – Divisione intera 13

INC – Incremento 14

JCXZ – Salto se CX è zero 14

JE – Salto se uguale 15

JG – Salto se maggiore 15

JGE – Salto se maggiore o uguale 16

JL – Salto se minore 16

JLE – Salto se minore o uguale 17

JMP – Salto incondizionato 17

JNE – Salto se diverso 18

JNO – Salto se non overflow 18

JNS – Salto se il flag di segno è 0 19

JO – Salto se overflow 19

JS – Salto se il flag di segno è 1 20

MOV – Trasferimento 20

MOVS – Trasferimento di una sequenza 21

MUL – Moltiplicazione intera 21

NEG – Negazione (formazione del complemento a 2) 22

NOP – Nessuna operazione 22

NOT – Negazione logica (formazione del complemento a 1) 23

POP – Prelevamento dallo stack 23

POPF – Prelevamento del registro dei flags dallo stack 24

PUSH – Deposito di un valore nello stack 24

PUSHF – Deposito del registro dei flags nello stack 25

RET – Ritorno da una procedura 25

SHL – Shift logico a sinistra 26

SHR – Shift logico a destra 26

STOP – Arresta la CPU 27

SUB – Sottrazione 27

XOR – Or esclusivo 28

Struttura di un programma assembly 29

## Premessa

EasyCPU è simulatore di un CPU che consente di scrivere programmi in linguaggio assembly e di testarne il funzionamento all’interno di una ambiente di programmazione dotato di interfaccia grafica.

EasyCPU simula l’architettura dei microprocessori X86 di INTEL, anche se implementa un set di istruzioni una gestione della memoria estremamente semplificati rispetto a quelli supportati dai microprocessore reali. Per questo motivo, EasyCPU non può ritenersi un modello completo per l’apprendimento della programmazione assembly e della struttura delle CPU X86; il suo scopo è semplicemente quello di fornire uno strumento facile da apprendere e da utilizzare per l’acquisizione dei principi generali relativi alla programmazione in linguaggio assembly e al funzionamento e alla struttura di un microprocessore.

## Struttura della CPU

### Artimetica

EasyCPU supporta la sola aritmetica a 16 bit. Ogni valore, sia esso immediato (costante) che memorizzato in un registro o in memoria, è rappresentato mediante il tipo intero a 16 bit, con un intervallo di variazione da –32768 e + 32767.

### Memoria dati

EasyCPU supporta una memoria non segmentata di 256 elementi interi. Di questi, gli ultimi 16 sono riservati allo «stack»:

![](data:image/x-emf;base64...)

Diversamente da quanto accade in un sistema reale, nella memoria vengono memorizzati soltanto i dati. Le istruzioni sono collocate in un vettore a se stante, del quale il registro IP funge da indice.

### Registri

EasyCPU supporta un set di registri analogo a quello delle microprocessori X86. Diversamente da questi, i registri adottano unicamente l’aritmetica a 16 bit:

![](data:image/x-emf;base64...)

### Modelli di indirizzamento

EasyCPU supporta 5 modelli di indirizzamento: immediato, a registro, diretto, indiretto a registro, indiretto a registro con scostamento.

Indirizzamento immediato

E’ rappresentato da un valore costante, espresso in forma decimale, esadedimale o carattere. Ad esempio, nelle istruzioni:

mov ax, **1** // memorizza 1 nel registro AX

mov ax, **0Ah** // memorizza 10 nel registro BX

mov ax, 'A' // memorizza 65 nel registro AX

i valori 1, 0Ah, ’A’ rappresentano delle costanti espresse in forma decimale, esadecimale e carattere. La forma esadecimale richiede come suffisso la lettera “h”; il primo carattere dev’essere una cifra. Un valore espresso in formato decimale può variare da –32768 a +32767, mentre un valore espresso in formato esadecimale non può essere preceduto dal segno meno e può variare da 0 a FFFF (che equivale a 65535). Una costante in formato carattere è rappresentata mediante un solo carattere delimitato da apici singoli, ed equivale al corrispondente valore ASCII.

Nel caso in cui un’istruzione richieda due operandi, soltanto il secondo può essere un valore immediato.

Indirizzamento a registro

In questo tipo di indirizzamento, l’operando è rappresentato da un registro. Un registro può comparire sia come primo che come secondo operando, oppure in entrambi:

mov **ax**, 1 // memorizza 1 nel registro AX

add **bx**, **ax** // somma il contenuto di AX a BX e mette il risultato in BX

Il registro IP non può apparire come operando di una istruzione.

Indirizzamento diretto

Nell’indirizzamento diretto l’operando è rappresentato dall’indirizzo di una locazione di memoria, che può variare dal 0 a 255. Un riferimento a un indirizzo della memoria dati è caratterizzato dalla sintassi “[indirizzo]”. Ad esempio:

mov ax, **[1]** // memorizza in AX il contenuto della cella di memoria 1

mov bx, **[0Ah]** // memorizza in BX il contenuto della cella di memoria 10

Come si vede, l’indirizzo di memoria può essere espresso sia in forma decimale che esadecimale.

In realtà un indirizzo di memoria può essere espresso anche mediante una costante di tipo carattere, ma non ciò non rappresenta di norma una buona pratica di programmazione.

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

### Gestione dello stack

In EasyCPU lo stack viene gestito in modo analogo a quanto avviene nei microprocessori della serie X86, ma in forma semplificata. La parte di memoria riservata allo stack inizia a un indirizzo di base immutabile, che è 240; essa occupa esattamente 16 byte.

Nella fase di inizializzazione della CPU, il registro SP (puntatore allo stack) viene impostato al valore 256 (indirizzo massimo dello stack più 1).

![](data:image/x-emf;base64...)

Lo stack è una struttura dati di tipo FIFO (*First In, First Out*: il primo che è entra è il primo ad uscire), che viene gestita mediante le istruzioni PUSH e POP. Mediante l’istruzione PUSH viene immesso un valore dallo stack; tale operazione determina un decremento del registro SP. Mediante l’istruzione POP l’ultimo valore allocato viene estratto; essa determina un incremento del registro SP.

Il tentativo di allocare un numero di valori supereriori alla dimensione dello stack produce un errore di «stack overflow».

### Flags

EasyCPU supporta i flag di segno (SF), zero (ZF) e overflow (OF), i quali vengono prevalentemente impiegati per l’esecuzione dei salti condizionati e sono influenzati dalle operazioni aritmetico logiche (esclusa DIV).

Il flag SF è settato se il risultato di un’operazione aritmetico-logica produce un risultato negativo, e dunque riflette il valore del bit di ordine superiore del risultato.

Il flag ZF è settato se il risultato di un’operazione aritmetico-logica è zero.

Il flag OF è settato se il risultato di un’operazione aritmetico-logica eccede la capacità di memorizzazione dell’operando e dunque l’intervallo di memorizzazione da –3276 a 32768.

Un test classico sui flag prevede un confronto tra due operandi tramite l’istruzione CMP, la quale sottrae il secondo operando dal primo senza però memorizzare il risultato, ma influenzando egualmente lo stato dei flag.

## Set di istruzioni

### Descrizione generale delle istruzioni

EasyCPU supporta istruzioni con zero, uno e due operandi; ogni istruzione ha un numero di operandi predefinito e immutabile. Le istruzioni rispecchiano la seguente sintassi:

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

### ADD – Addizione

Sintassi:

**ADD *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione + sorgente**

Flag definiti:

**SF*,* ZF, OF**

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

**SF*,* ZF; OF = 0**

Descrizione:

AND imposta a 1 i bit del risultato se entrambi i bit corrispondenti dei due operandi sono 1; altrimenti li imposta a 0. Il risultato è memorizzato nell’operando destinazione.

Esempio:

mov ax, 2

and ax, 4 // produce come risultato: 0

### CALL – Chiamata di una procedura

Sintassi:

**CALL *etichetta***

Operazione svolta:

**SP = SP – 1**

**MEMORIA[SP] = IP**

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

CALL modifica il flusso di esecuzione delle istruzioni, assegnando al registro IP un nuovo indirizzo nella memoria delle istruzioni, dopo averlo precedentemente salvato sullo stack. Mediante l’istruzione RET è possibile recuperare il valore di IP per riprendere l’esecuzione dell’istruzione successiva a CALL.

Esempio:

call ciclo // IP viene punta alla istruzione designata da "ciclo"

...

ciclo: *<inizio della procedura>*

### CMP – Confronto

Sintassi:

**CMP *sorgente1, sorgente2***

Operazione svolta:

**sorgente1 – sorgente2**

Flag definiti:

**ZF, SF,OF**

Descrizione:

CMP sottrae il secondo operando al primo senza però memorizzare il risultato. L’effetto è quello di aggiornare i valori dei flags che potranno poi essere testati mediante un’istruzione di salto condizionato.

Esempio:

cmp ax, bx // confronta ax con bx

je salto // salta se ax è uguale a bx (e dunque il flag zero è 1)

### DEC – Decremento

Sintassi:

**DEC *destinazione***

Operazione svolta:

**destinazione = destinazione – 1**

Flag definiti:

**ZF, SF, OF**

Descrizione:

DEC decrementa di uno l’operando

Esempi:

dec ax // decrementa di 1 ax

dec [20] // decrementa di 1 il contenuto della locazione [20]

dec [si] // decrementa di 1 il contenuto della locazione puntata da si

### DIV – Divisione intera

Sintassi:

**DIV *sorgente***

Operazione svolta:

**AX = DX:AX / sorgente**

**DX = DX:AX % sorgente**

Flag definiti:

**Nessuno**

Descrizione:

DIV divide il valore memorizzato nella coppia di registri DX:AX per l’operando, memorizzando in AX il quoziente intero della divisione e in DX il resto intero.

Esempi:

div 2 // divide dx:ax per 2

div [20] // divide dx:ax per il contenuto della locazione [20]

div [si] // divide dx:ax per il contenuto della locazione puntata da si

### INC – Incremento

Sintassi:

**INC *destinazione***

Operazione svolta:

**destinazione = destinazione + 1**

Flag definiti:

**ZF, SF, OF**

Descrizione:

INC incrementa di uno l’operando

Esempi:

inc ax // incrementa di 1 ax

inc [20] // incrementa di 1 il contenuto della locazione [20]

inc [si] // incrementa di 1 il contenuto della locazione puntata da si

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

Esempi:

cmp ax, bx

jg salto // se "ax > bx" IP punta alla istruzione designata da " salto "

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

Esempi:

cmp ax, bx

jl salto // se "ax < bx" IP punta alla istruzione designata da " salto "

### JMP – Salto incondizionato

Sintassi:

**JMP *etichetta***

Operazione svolta:

**IP = indirizzo designato dall’etichetta**

Flag definiti:

**Nessuno**

Descrizione:

JMP modifica il flusso di esecuzione delle istruzioni, assegnando al registro IP un nuovo indirizzo nella memoria delle istruzioni.

Esempio:

jmp salto // IP punta alla istruzione designata da " salto "

salto: ...

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

**JNO *etichetta***

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

**JNO *etichetta***

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

**JNO *etichetta***

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

### MOVS – Trasferimento di una sequenza

Sintassi:

**MOVS**

Operazione svolta:

**MEMORIA[DI] = MEMORIA[SI]**

Flag definiti:

**Nessuno**

Descrizione:

MOVS copia il contenuto della locazione di memoria puntata da SI nella locazione di memoria puntata da DI.

Esempi:

mov di, 0 // imposta a 0 di

mov si, 10 // imposta a 10 si

movs // equivale a: memoria[0] = memoria[10]

### MUL – Moltiplicazione intera

Sintassi:

**MUL *sorgente***

Operazione svolta:

**DX:AX = AX \* sorgente**

Flag definiti:

**OF**

Descrizione:

MUL moltiplica il registro AX per l’operando, memorizzando nella coppia di registri DX:AX il risultato della moltiplicazione. Se il flag OF viene settato significa che DX contiene delle cifre significative.

Esempi:

mul 2 // moltiplica ax per 2

mul [20] // moltiplica ax per il contenuto della locazione [20]

mul [si] // moltiplica ax per il contenuto della locazione puntata da si

### NEG – Negazione (formazione del complemento a 2)

Sintassi:

**NEG *destinazione***

Operazione svolta:

**destinazione = 0 - destinazione**

Flag definiti:

**SF, ZF**

Descrizione:

NEG sottrae l’operando a 0, ottenendo così il suo complemento a 2.

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

NOT inverte ogni bit dell’operando, producendo così il complemento a 1 dello stesso..

Esempi:

mov ax, 2

not ax // produce: -3 (FFFD in esadecim.)

### POP – Prelevamento dallo stack

Sintassi:

**POP *destinazione***

Operazione svolta:

**destinazione = valore che si trova in testa allo stack**

**SP = SP + 1**

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

**SP = SP + 1**

Flag definiti:

**SF, ZF, OF**

Descrizione:

POPF copia nel registro dei flags il contenuto della zona di memoria puntata da SP (testa dello stack). Dopodiché incrementa SP, in modo che punti al nuovo valore in testa allo stack. Si presuppone che in precedenza il registro dei flags fosse stato copiato nello stack mediante l’istruzione PUSHF.

Esempi:

popf // copia nel registro dei flags il valore memorizzato in testa allo stack

### PUSH – Deposito di un valore nello stack

Sintassi:

**PUSH *sorgente***

Operazione svolta:

**SP = SP - 1**

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

**SP = SP - 1**

**il registro dei flags viene memorizzato in testa allo stack**

Flag definiti:

**Nessuno**

Descrizione:

PUSHF, prima decrementa SP quindi copia il registro dei flags nella zona di memoria puntata da SP (testa dello stack). Si presuppone che il registro venga successivamente ripristinato mediante l’istruzione POPF.

Esempi:

pushf // copia il registro dei flags in testa allo stack

### RET – Ritorno da una procedura

Sintassi:

**RET**

Operazione svolta:

**SP = SP + 1**

**IP viene prelevato dallo stack**

Flag definiti:

**Nessuno**

Descrizione:

RET modifica il flusso di esecuzione delle istruzioni, prelevando IP dallo stack e quindi assegnandogli un nuovo indirizzo nella memoria delle istruzioni. Si presuppone che RET termini una procedura che sia stata precedentemente avviata da un’istruzione CALL.

Esempi:

call ciclo // IP punta alla istruzione designata da "ciclo"

...

ciclo: *<inizio della procedura>*

*...*

ret // ritorno all’istruzione successiva a CALL

### SHL – Shift logico a sinistra

Sintassi:

**SHL *destinazione*, *contatore***

Operazione svolta:

**sposta a sinistra i bit dell’operando, impostando a zero il bit di ordine inferiore (bit più a destra)**

Flag definiti:

**SF, ZF**

Descrizione:

SHL, sposta a sinistra i bit dell’operando destinazione per un numero di posizioni equivalente all’operando contatore. Ad ogni spostamento, nel bit più a destra viene memorizzato il valore zero.

Esempi:

mov ax, 2

shl ax, 3 // produce: 10

### SHR – Shift logico a destra

Sintassi:

**SHR *destinazione*, *contatore***

Operazione svolta:

**sposta a destra i bit dell’operando, impostando a zero il bit di ordine superiore (bit più a sinistra)**

Flag definiti:

**SF, ZF**

Descrizione:

SHR, sposta a destra i bit dell’operando destinazione per un numero di posizioni equivalente all’operando contatore. Ad ogni spostamento, nel bit più a sinistra viene memorizzato il valore zero.

Esempi:

mov ax, 4

shr ax, 2 // produce: 1

### STOP – Arresta la CPU

Sintassi:

**STOP**

Operazione svolta:

**Arresta la CPU, terminando l’esecuzione del programma**

Flag definiti:

**Nessuno**

Descrizione:

STOP determina l’immediato arresto dell’esecuzione del programma.

### SUB – Sottrazione

Sintassi:

**SUB *destinazione*, *sorgente***

Operazione svolta:

**destinazione = destinazione - sorgente**

Flag definiti:

**SF*,* ZF, OF**

Descrizione:

L’operando sorgente viene sottratto all’operando destinazione; il risultato viene memorizzato nell’operando destinazione.

Esempi:

sub ax, bx // equivale a: ax = ax - bx

sub [10], 2 // equivale a: [1] = [1] - 2

### XOR – Or esclusivo

Sintassi:

**XOR *destinazione*, *contatore***

**destinazione = destinazione ^ sorgente**

Flag definiti:

**SF*,* ZF, OF = 0**

Descrizione:

XOR imposta a 1 i bit del risultato se entrambi i bit corrispondenti dei due operandi sono diversi tra loro; altrimenti li imposta a 0. Il risultato è memorizzato nell’operando destinazione.

Esempio:

mov ax, 2

xor ax, 4 // produce come risultato: 6

## Struttura di un programma assembly

I programmi assembly compatibili con EasyCPU sono suddivisi in due sezioni, «codice» e «dati». La sezione codice contiene le istruzioni in linguaggio assembly e comincia con l’inizio del file. La parte dati, opzionale, è preceduta dalla direttiva «.DATA» e consente di inizializzare il contenuto di una o più celle di memoria. All’interno dell’IDE di EasyCPU, le due sezioni vengono gestite mediante due editor separati. Un programma può inoltre contenere delle righe di commento, prefissate dal simbolo “//”. Un commento può anche seguire il testo di un’istruzione.

Di seguito viene riportato un programma di esempio che calcola la somma degli elementi dispari all’interno di un sequenza. Questa è definita nella sezione dati a partire dell’indirizzo 1 di memoria. All’indirizzo 0 è memorizzato il numero di elementi.

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

1: 1, 3, 4, 6, 7
