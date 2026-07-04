// sub routine SOMMA A, B
// equivale a: 
       
// int Somma(int a, int b) { return a + b }
       
// nota bene: il risultato viene memorizzato in AX                   
       
       push [1]	// mette "a" nello stack
       push [2]	// mette "b" nello stack
       call somma	// chiama "Somma" (salva IP nello stack)
       add sp,2	// "rimuove" parametri dallo stack

       push ax
       push [10]
       call somma
       
       
       push ax
       push [15]
       call somma
       stop
       
somma:
	push bp		// salva BP
	mov bp, sp		
	mov ax, [bp+2]	// [BP + 2] -> "b"
	add ax, [bp+3]	// [BP + 3] -> "a"
	pop bp
	ret       		
.DATA
1:4, 5
10: 11
15: 12
