// Programmma: SommaOProdotto
// Data una sequenza di tre numeri il programma esegue:
// 	verifica il primo, e se è minore di zero calcola la prodotto degli altri due
//	altrimenti ne calcola la somma
// Il risultato viene memorizzato all'indirizzo di memoria 15
       
       mov ax, [11]
       cmp [10], 0
       jl somma
       mul [12]
       jmp risultato
somma:       
       add ax, [12]
risultato:
       mov [15], ax
       stop        
       
       
       
.DATA
10: -1, 2, 3
