EasyCPU

IDE Tutorial

|  |  |
| --- | --- |
| ![](data:image/x-emf;base64...) | **Paolo Meozzi**  **Stefano Del Furia** |

Sommario

Introduzione 5

1 Cenni generali sull’IDE 5

1.1 Area «codice e dati» 5

1.2 Area «memoria» ed «errori di compilazione» 5

1.3 Area «stack» 6

1.4 Area «registri» 6

1.5 Toolbar 6

1.6 Barra di stato 6

2 Scrittura, esecuzione e debug di un programma assembly 7

2.1 Struttura di un programma assembly 7

2.2 Gestione programmi: menu «File» 7

2.3 Editor di codice e dati 7

2.4 Compilazione ed esecuzione di un programma 7

2.5 Visualizzazione degli errori compilazione 8

2.6 Debug di un programma (esecuzione “passo passo”) 8

2.7 Continuare o arrestare l’esecuzione di un programma in modalità debug 10

2.8 Interrompere un ciclo infinito 10

2.9 Memoria a byte (modalità x86 fedele)

2.10 Stato della CPU 11

3 Preferenze e opzioni di visualizzazione 11

## Introduzione

EasyCPU è un ambiente di programmazione specificatamente realizzato per l’insegnamento del linguaggio assembly e degli elementi di base dell’architettura di un microprocessore. Consente di scrivere, eseguire e debuggare programmi in linguaggio assembly, ma non è un assemblatore, non produce cioè codice macchina nativo per una determinata piattaforma. EasyCPU simula infatti l’architettura dei microprocessori X86 di INTEL, implementando una CPU virtuale con un set di istruzioni e una gestione dei registri e della memoria estremamente semplificati rispetto a quelli supportati dai microprocessore reali.

Per questo motivo, EasyCPU non può ritenersi un modello completo per l’apprendimento della programmazione assembly e della struttura delle CPU X86; il suo ambito ideale è senz’altro quello didattico ed il suo scopo è quello di fornire uno strumento d’immediato apprendimento e utilizzo per esercitarsi sulla programmazione in assembly.

Per un esame dell’architettura e del set di istruzioni implementati si veda il documento «EasyCPU – Assembly reference»

## Cenni generali sull’IDE

EasyCPU è un tipico un ambiente grafico di programmazione che fornisce le funzionalità necessarie per scrivere, eseguire e testare un programma in linguaggio assembly. La figura a pagina successiva mostra uno screen shot dell’IDE, il quale è suddiviso in quattro aree principali:

* area «codice e dati»
* area «memoria» ed «errori di compilazione»
* area «stack»
* area «registri»

### Area «codice e dati»

Questa rappresenta l’editor del programma, sia per quanto riguarda la scrittura del codice assembly (scheda «codice») che per la definizione di costanti da collocare nella memoria della CPU (scheda «dati»). Durante l’esecuzione di un programma, entrambi gli editor sono disabilitati.

### Area «memoria» ed «errori di compilazione»

Quest’area rappresenta una finestra sul contenuto della memoria della CPU, esclusa la zona dedicata allo stack. Esistono tre modalità di visualizzazione dei dati: decimale, esadecimale e carattere. L’utente può decidere se visualizzare o nascondere quest’area. Se il programma usa la memoria a byte (vedi «Memoria a byte (modalità x86 fedele)») il pannello si intitola «Memoria (byte)» e mostra 16 byte per riga.

Se il programma definisce dei nomi (variabili DB e DW, costanti EQU, etichette del codice), sotto il contenuto della memoria compare la sezione «Simboli»: per ogni nome della sezione dati riporta l’indirizzo, il tipo, il numero di elementi e il valore corrente del primo elemento, così da ritrovare facilmente le variabili del programma; in fondo elenca le etichette con il numero della loro istruzione, utile per leggere le tabelle di salto.

In caso di errori di compilazione del programma, l’area di memoria viene nascosta e viene mostrato l’elenco degli errori di sintassi riscontrati. Cliccando su un errore è il caret dell’editor viene posizionato automaticamente sulla posizione corrispondente nel programma.

### Area «stack»

Rappresenta una finestra sull’area di memoria riservata allo stack. Anche in questo caso esistono tre modalità di visualizzazione dei dati: decimale, esadecimale e carattere. Inoltre, è possibile stabilire se visualizzare le celle di memoria in una o due colonne. Con la memoria a byte ogni riga mostra una parola (due byte), all’indirizzo del suo byte basso.

### Area «registri»

Visualizza il contenuto dei registri nel formato scelto. Per AX, BX, CX e DX sono mostrati anche il byte alto e il byte basso (ad esempio AX = 0141 [AH=01 AL=41]). Sotto i registri sono elencati i flag C (carry), Z (zero), S (segno), O (overflow) e D (direzione), ciascuno con il valore 0 o 1.

### Toolbar

Rende immediatamente disponibili i comandi più comuni di EasyCPU. Questi sono suddivisi in tre categorie: gestione file, esecuzione e opzioni di visualizzazione.

### Barra di stato

La barra di stato mostra lo stato di esecuzione del programma, la riga corrente dell’editor ed il valore del registro selezionato nella area «registri».

![](data:image/x-emf;base64...)

Figura 1 IDE di EasyCPU.

## Scrittura, esecuzione e debug di un programma assembly

Un programma EasyCPU è memorizzato in un file con estensione «asj»; i programmi nel formato originale «as» possono essere aperti e vengono salvati nel formato «asj». Oltre a operare sui registri e sulla memoria della CPU, un programma può interagire con l’utente mediante i servizi dell’istruzione INT 21h, che leggono i tasti premuti e scrivono testo nel pannello «Console».

Nella cartella Docs/samples sono disponibili numerosi programmi di esempio, suddivisi per argomento; ognuno inizia con un commento che spiega cosa illustra, come usarlo e quale risultato attendersi.

### Struttura di un programma assembly

Un programma è suddiviso nelle sezioni «codice» e «dati». La parte codice contiene le istruzioni in linguaggio assembly e comincia con l’inizio del file. La parte dati, opzionale, è separata dalla parte codice dalla direttiva «.DATA». Segue un breve esempio:

// Esempio di programma in linguaggio assembly: somma di due numeri

// i valori da sommare sono memorizzati agli indirizzi di memoria 1 e 2

mov ax, [1] // carica in ax il primo valore da sommare

mov bx, [2] // carica in ax il primo valore da sommare

add ax, bx

// -------------------------------------

.DATA

1: 5

2: 22

Come si vede, un programma può contenere dei commenti, prefissati dal simbolo “//” oppure “;”.

Oltre alla forma «indirizzo: valori» dell’esempio, la sezione dati può definire variabili e costanti con nome (DB, DW, EQU, DUP, ORG), da usare poi nel codice: la sintassi è descritta nel capitolo «La sezione dati» dell’Assembly Reference.

### Gestione programmi: menu «File»

Come in qualsiasi IDE, mediante i comandi del menù «File» ed i pulsanti della tool bar è possibile creare un nuovo programma, aprirne uno esistente e salvare il programma in fase di editing. Il menù «File» definisce inoltre, nel sottomenu «Recenti», l’elenco dei dieci programmi più recenti caricati nell’IDE.

Nella versione Browser i comandi del menù «File» si trovano nel menu laterale, che si apre con il pulsante ☰ in alto a sinistra. La voce «Recenti» apre e chiude l’elenco dei programmi recenti; poiché il browser non consente di riaprire un file dal suo percorso, EasyCPU ne conserva una copia e la riapre da lì. Un programma riaperto dai recenti non è collegato a un file: il comando «Salva» chiede dove salvarlo.

### Editor di codice e dati

Pur essendo memorizzati nello stesso file, le sezioni «codice» e «dati» di un programma sono gestire da editor separati. Per entrambi è possibile stabilire il font utilizzato mediante il comando «Preferenze | Font codice e dati…».

L’editor di codice consente inoltre di stabilire il numero di spazi da inserire automaticamente dal margine sinistro.

### Compilazione ed esecuzione di un programma

Un programma può essere eseguito in tre modi:

* con il comando «Esegui | Esegui»;
* con il tasto F5;
* ciccando sul bottone ![](data:image/png;base64...).

In ogni caso, l’esecuzione procede fino al termine del programma, a meno che non venga identificato un potenziale ciclo infinito (vedi «Interruzione di un ciclo infinito»).

### Visualizzazione degli errori compilazione

L’esecuzione di un programma viene preceduta dalla sua traduzione in un formato interno specifico dell’applicazione, operazione che comprende la verifica sintattica del codice. Gli eventuali errori sintattici vengono mostrati nell’area «errori di compilazione», la quale nasconde automaticamente l’area «memoria».

La figura seguente mostra un frammento di programma contenente due errori; per ogni errore viene riportato se è relativo al codice o ai dati, il numero di riga e un messaggio informativo . Cliccando sul report di un errore, il caret viene automaticamente portato sulla posizione corrispondente nel codice sorgente.

![](data:image/png;base64...)

In alcuni casi sia il messaggio visualizzato che il posizionamento del caret possono essere fuorvianti.

Errori di esecuzione

Alcuni errori si possono rilevare soltanto durante l’esecuzione: ad esempio una divisione per zero, uno stack overflow o un salto a un’istruzione inesistente. In questo caso l’esecuzione si interrompe e il pannello degli errori mostra un errore di tipo «Esecuzione», con la riga dell’istruzione che lo ha causato (se il registro IP non indica più un’istruzione del programma, la riga non è indicata). Lo stesso messaggio compare nella barra di stato. Anche per questi errori, cliccando sull’errore il caret viene portato sulla riga corrispondente.

### Debug di un programma (esecuzione “passo passo”)

Una delle caratteristiche principali di EasyCPU è data dalla possibilità di eseguire il programma un’istruzione alla volta, in modo simile ai moderni IDE. Ciò non solo semplifica lo sviluppo del programma, ma rappresenta uno strumento ideale per l’apprendimento del linguaggio, poiché consente di verificare gli effetti (sulla CPU) di ogni singola istruzione.

In questa modalità, l’editor di codice visualizza l’istruzione da eseguire con uno sfondo giallo, come mostra la figura a pagina successiva:

![](data:image/png;base64...)

Figura 2 Esempio di esecuzione “passo passo” di un programma.

Nell’esempio, è già stata eseguita la prima istruzione e l’esecuzione è sospesa sulla seconda (ancora da eseguire).

Esistono due comandi connessi all’esecuzione “passo passo”, spiegati di seguito.

Esegui istruzione

Mediante:

* il tasto F10,
* il comando «Esegui | Esegui istruzione»,
* il bottone ![](data:image/png;base64...)

è possibile eseguire l’istruzione corrente, evidenziata con lo sfondo giallo. Dopodiché l’esecuzione sarà sospesa sulla prossima istruzione (o terminata se non ci sono più istruzioni).

Un’istruzione stringa con il prefisso REP (ad esempio rep movsw) fa eccezione: ogni comando «Esegui istruzione» esegue una sola ripetizione, e l’istruzione resta evidenziata finché il registro CX non arriva a zero. In questo modo si possono osservare SI, DI e CX cambiare a ogni ripetizione. Un breakpoint su quella riga sospende l’esecuzione soltanto all’arrivo sull’istruzione, non a ogni ripetizione.

Esegui fino all’istruzione

Mediante:

* il tasto F4 e
* il comando «Esegui | Esegui fino a »,

è possibile eseguire tutte le istruzioni che precedono quella selezionata, sulla quale l’esecuzione sarà sospesa. Quest’ultima è rappresentata dalla riga contenente il caret, oppure dalla riga selezionata (testo bianco su sfondo blu) se è il programma è già in esecuzione.

Nello screen shot che segue, l’esecuzione è sospesa sulla seconda istruzione, e l’utente ha selezionato l’ultima istruzione. In questa situazione, eseguendo il comando «Esegui fino» vengono eseguite normalmente la terza e la quarta istruzione, e l’esecuzione viene sospesa sull’ultima.

![](data:image/png;base64...)

Figura 3 Esempio di esecuzione “passo passo” di un programma; comando «Esegui fino a»

Questa funzionalità consente di velocizzare il debug del programma, eseguendo normalmente parti più o meno consistenti di codice e sospendendo l’esecuzione soltanto sulle istruzioni che si desidera indagare.

### Continuare o arrestare l’esecuzione di un programma in modalità debug

Durante il debug del programma è sempre possibile riprendere la normale esecuzione mediante il comando «Esegui | Esegui», oppure arrestarla mediante:

* il tasto MAIUSC-F5,
* il comando «Esegui | Stop »,
* il bottone ![](data:image/png;base64...).

### Interrompere un ciclo infinito

Durante l’esecuzione di un programma, EasyCPU usa un contatore interno per verificare se la CPU virtuale si trova in un ciclo potenzialmente infinito. Quando il contatore raggiunge un numero stabilito dall’utente (vedi «Preferenze e opzioni di visualizzazione») compare la message dialog di figura 4, che consente all’utente di arrestare l’esecuzione, sospenderla ed entrare in modalità debug, oppure continuare l’esecuzione normalmente. In quest’ultimo caso, il contatore sarà azzerato e sarà nuovamente visualizzata la dialog nel caso raggiunga nuovamente il valore limite stabilito.

Il valore limite predefinito del contatore è 65535, ma può essere modificato dall’utente.

![](data:image/png;base64...)

Figura 4 Finestra di dialogo di gestione dei cicli infiniti.

### Memoria a byte (modalità x86 fedele)

Nella modalità predefinita la memoria della CPU è formata da 256 celle da 16 bit. La voce «Memoria a byte (x86)» del menu «Esegui» (nella versione Browser, del menu laterale) attiva per il programma corrente la modalità x86 fedele: 512 byte, parole di due byte in ordine little-endian, stack di 32 parole nei byte da 448 a 511, e le regole del compilatore descritte nel manuale del linguaggio (capitolo «Modalità x86 fedele: memoria a byte»).

La modalità è una proprietà del programma: viene salvata nel file .asj insieme al codice e ai dati, e un programma riaperto (anche dai «Recenti» del browser) torna nella sua modalità. Il comando «Nuovo» riporta la memoria a parole. Cambiare la modalità segna il programma come modificato, azzera registri, memoria e stack e richiede una nuova compilazione; non è possibile cambiarla durante l’esecuzione. La barra di stato indica «(memoria a byte)» all’apertura e alla compilazione. Gli esempi della cartella Docs/samples/12-memoria-byte sono già impostati in questa modalità.

### Stato della CPU

La barra di stato riporta lo stato corrente della CPU, e cioè dell’esecuzione del programma, utile soprattutto quanto si esegue il programma in modalità debug. I possibili stati sono:

|  |  |  |
| --- | --- | --- |
| ![](data:image/png;base64...) |  | Il programma non è in esecuzione. |
| ![](data:image/png;base64...) |  | Il programma è sospeso sulla prima istruzione. |
| ![](data:image/png;base64...) |  | Il programma è sospeso su una istruzione qualsiasi  dopo la prima. |

## Preferenze e opzioni di visualizzazione

EasyCPU consente all’utente di personalizzare alcune caratteristiche dell’IDE, sia attraverso la tool bar che il menù Preferenze.

### Modalità visualizzazione

La tool bar contiene quattro pulsanti che consentono di intervenire sulla modalità di visualizzazione di memoria, registri e stack.

Formato di visualizzazione dei dati

Mediante i tre pulsanti ![](data:image/png;base64...) è possibile stabilire se visualizzare i dati di memoria, stack e registri nel formato esadecimale, decimale e carattere. Nella versione attuale dell’IDE il formato si sceglie dalla casella a discesa della toolbar (Car, Dec, Hex); la scelta viene ricordata e ripristinata all’avvio successivo, e al primo avvio il formato è Dec. Esiste una parziale eccezione per i registri, i cui valori possono essere visualizzati soltanto in formato decimale o esadecimale. (Cliccando sul pulsante![](data:image/png;base64...) si ottiene comunque una visualizzazione in formato decimale.)

Visualizzare/nascondere l’area di memoria

E’ possibile visualizzare o nascondere l’area di memoria mediante il bottone ![](data:image/png;base64...).

Numero di colonne di visualizzazione dello stack

L’ultimo pulsante della toolbar consente di stabilire se visualizzare il contenuto dello stack in una o due colonne. Il pulsante ha due stati. Quando si trova nello stato ![](data:image/png;base64...), lo stack è visualizzato in due colonne è ed possibile passare alla visualizzazione in una colonna. Nello stato ![](data:image/png;base64...) vale naturalmente l’opposto.

### Font dell’editor codice e dati

Attraverso il comando «Preferenze | Font codice e dati…» è possibile scegliere il font da utilizzare negli editor codice e dati. La selezione del nuovo carattere avviene mediante la finestra standard «tipo carattere».

### Finestra di dialogo «Opzioni»

Attraverso la finestra di dialogo «Opzioni», mostrata di seguito, è possibile impostare gli elementi configurabili di EasyCPU. Alcuni di questi possono essere configurati anche mediante la tool bar, e precisamente: il formato di visualizzazione dei dati ed il numero di colonne di visualizzazione dello stack. Segue un breve cenno per le altre impostazioni di configurazione

![](data:image/png;base64...)

Figura 5 Finestra di dialogo Opzioni

Massimo numero di errori visualizzati

Indica il numero massimo di errori di compilazione che vengono considerati, eventuali errori in più non saranno visualizzati.

Visualizza caratteri ‘\0’ come:

All’interno della modalità di visualizzazione carattere consente di stabilire con quale simbolo visualizzare i caratteri con codice ASCII zero.

Margine sinistro

Consente di stabilire il margine sinistro nell’editor di codice, e cioè il numero di spazi inseriti automaticamente quando si digitano le istruzioni nell’editor di codice.

Numero di istruzioni che prefigurano in ciclo infinito

EasyCPU mantiene un contatore interno per ogni istruzione che consente di individuare potenziali cicli infiniti. Quando un contatore raggiunge il valore impostato, EasyCPU visualizza la dialog di interruzione ciclo infinito mostrata in figura 4.

Inizializza registri all’avvio

Attivando questa opzioni, EasyCPU imposta automaticamente a zero il valore di tutti i registri prima di avviare l’esecuzione del programma, altrimenti li lascia nello stato prodotto dalla precedente esecuzione.

### Impostazioni conservate tra le sessioni

EasyCPU ricorda tra una sessione e l’altra le opzioni, la disposizione dei pannelli, l’elenco dei programmi recenti e i breakpoint di ogni programma. Nella versione Desktop sono salvati in file nella cartella dei dati dell’applicazione (i breakpoint in un file con estensione «bkpt» accanto al programma). Nella versione Browser sono salvati nella memoria locale del browser (localStorage) del sito di EasyCPU: per ripristinare le impostazioni iniziali basta cancellare i dati del sito dalle impostazioni del browser.
