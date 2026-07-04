// Programma SommaDispari
// Calcola la somma degli elementi dispari di un vettore e la memorizz in BX

	mov bx, 0		// bx memorizza la somma
	mov cx, [0]		// memorizza il numero degli elementi
	mov si, [1]		// si memorizza l'indirizzo del vettore
	
ciclo:	cmp cx, 0		// verifica se il ciclo è finito
	je fine
	
	mov ax,[si]		// preleva l'elemento
	mov dx, 0		// non obbligatoria
	div 2			// divide per 2 per ottenere il resto in dx
	cmp dx, 0		// se dx è 0 il numero è pari
	je pari		
	add bx, [si]		// somma l'elemento
pari:
	inc si
	dec cx
	jmp ciclo

fine:	
	stop
		
.DATA
0: 5
1: 1, 3, 4, 6, 7
