// sub routine SOMMA A, B
// equivale a: 
       
// int Somma(int[] vet) { ... return somma; }
       
// nota bene: il risultato viene memorizzato in AX                   
       
       push 10	// mette indirizzo vettore
       call sommaVet	// chiama "Somma" (salva IP nello stack)
       add sp,1	// "rimuove" parametri dallo stack
       stop
       
sommaVet:
	push bp		// salva BP
	mov bp, sp		
       push cx
       push si
       
	mov si, [bp+2]	// [BP + 2] -> "indirizzo vettore"
       mov ax, 0
	mov cx, [si]		// CX <- lunghezza
       
inizioCiclo:       
       jcxz fineCiclo
       inc si
       add ax, [si]
       dec cx
       jmp inizioCiclo
       
fineCiclo:       
       pop si
       pop cx
	pop bp
	ret       		
.DATA
10: 4, 2, 3, 4, 1

