       mov ax, 10
       push ax
       call proc
       add sp,1
       stop
       
proc:
       mov bp, sp
       mov bx, [bp+1]
       ret       
.DATA
