using System;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using EasyCpu.Assembler.Parsing;
using EasyCpu.Assembler.Memoria;
using EasyCpu.Common;


namespace EasyCpu.Assembler.Processore
{
    public class Cpu
    {

        static readonly string[] nomiRegs =
        {
            "AX", "BX", "CX", "DX", "SI", "DI", "BP", "SP", "IP",
        };

        public enum StatoCpu
        {
            Pronta,
            Attiva,
            Ferma
        }

        // bit del registro dei flags, nelle stesse posizioni di x86
        const short CF = 1 << 0;
        const short ZF = 1 << 6;
        const short SF = 1 << 7;
        const short DF = 1 << 10;
        const short OF = 1 << 11;

        const short TUTTI = (ZF | SF | OF);

        // registri
        short ax;
        short bx;
        short cx;
        short dx;

        short sp;
        short bp;
        short ip;

        short si;
        short di;

        short flags;

        public bool stop;
        int loopInfinito;
        public StatoCpu Stato = StatoCpu.Ferma;

        MemoriaCpu memoria = MemoriaCpu.Crea(ModelloMemoria.Parole);
        List<Instruction> Code;
        Instruction curIstruzione;

        public HashSet<int> Breakpoints { get; } = new();

        public bool IPOverRun
        {
            get { return ip > Code.Count || ip < 0; }
        }

        public short IP => ip;
        public short SP => sp;
        public short AX => ax;
        public short BX => bx;
        public short CX => cx;
        public short DX => dx;
        public short SI => si;
        public short DI => di;
        public short BP => bp;

        public short LeggiMemoria(int indirizzo) => memoria.LeggiParola(indirizzo);
        public short LeggiByte(int indirizzo) => memoria.LeggiByte(indirizzo);

        public ModelloMemoria Modello => memoria.Modello;

        public bool FlagSegno => TestFlag(SF);
        public bool FlagZero => TestFlag(ZF);
        public bool FlagOverflow => TestFlag(OF);
        public bool FlagCarry => TestFlag(CF);
        public bool FlagDirezione => TestFlag(DF);

        // true mentre un'istruzione con prefisso REP deve ancora ripetersi: IP resta fermo su di essa
        bool ripetizioneInCorso;

        public async Task Run(int IP)
        {
            ip = (short)IP;
            await Run();
        }

        static bool LoopInfinito(int numIstruzioni, int limite)
        {
            return limite > 0 && numIstruzioni == limite;
        }

        public async Task Run()
        {
            Stato = StatoCpu.Attiva;
            int numIstruzioni = 0;
            try
            {
                while (!stop)
                {
                    if (!ripetizioneInCorso && Breakpoints.Contains(ip))
                        throw new CpuTrapException();

                    Fetch();
                    await Execute();

                    if (ip == Code.Count)
                        Stop();

                    if (IPOverRun)
                        throw new CpuException(CodiceErrore.IPNonValido);

                    numIstruzioni++;
                    if (LoopInfinito(numIstruzioni, loopInfinito))
                        throw new CpuLoopException();
                }
            }
            catch (CpuException)
            {
                Stop();
                throw;
            }
        }

        // Singolo step senza verifica breakpoint (usa per avanzare dopo un trap)
        public async Task StepInto()
        {
            if (stop) return;
            Stato = StatoCpu.Attiva;
            try
            {
                Fetch();
                await Execute();
            }
            catch (Exception)
            {
                Stop();
                throw;
            }
            if (ip == Code.Count)
                Stop();
            else if (IPOverRun)
            {
                Stop();
                throw new CpuException(CodiceErrore.IPNonValido);
            }
        }

        // Esegue finché sp < limite, controllando breakpoint e loop infinito
        async Task RunWhileInside(short limite)
        {
            int numIstruzioni = 0;
            while (!stop && sp < limite)
            {
                if (!ripetizioneInCorso && Breakpoints.Contains(ip))
                    throw new CpuTrapException();

                Fetch();
                await Execute();

                if (ip == Code.Count) { Stop(); break; }
                if (IPOverRun) throw new CpuException(CodiceErrore.IPNonValido);

                numIstruzioni++;
                if (LoopInfinito(numIstruzioni, loopInfinito))
                    throw new CpuLoopException();
            }
        }

        public async Task StepOver()
        {
            if (stop) return;
            // Se l'istruzione corrente non è una call, fa semplicemente StepInto
            if (Code[ip].Code != "call")
            {
                await StepInto();
                return;
            }
            short S = sp;
            Stato = StatoCpu.Attiva;
            try
            {
                Fetch();
                await Execute(); // esegue la call: sp diminuisce, ip salta alla subroutine
                if (ip == Code.Count) { Stop(); return; }
                await RunWhileInside(S); // continua finché sp < S (cioè siamo dentro la subroutine)
            }
            catch (CpuException)
            {
                Stop();
                throw;
            }
        }

        // Esegue finché sp <= S (cioè siamo ancora dentro o più in profondità):
        // RunWhileInside(S+1) → loop while sp < S+1 → while sp <= S
        public async Task StepOut()
        {
            if (stop) return;
            short S = sp;
            Stato = StatoCpu.Attiva;
            try
            {
                await RunWhileInside((short)(S + 1));
            }
            catch (CpuException)
            {
                Stop();
                throw;
            }
        }

        // modello: memoria del programma (Compiler.Modello); se omesso, modalità a parole
        public void Init(List<Instruction> codice, List<int> memoriaDati, bool initRegs, int AloopInfinito,
            ModelloMemoria modello = null)
        {
            memoria = MemoriaCpu.Crea(modello ?? ModelloMemoria.Parole);
            stop = false;
            ultimoCR = false;
            ripetizioneInCorso = false;
            bufferTastiera.Clear();
            memoria.Imposta(memoriaDati);
            Code = codice;
            ip = 0;
            flags = 0;
            sp = (short)memoria.Modello.Dimensione;
            loopInfinito = AloopInfinito;
            Stato = StatoCpu.Pronta;
            if (initRegs)
                InizializzaRegs();
        }

        void InizializzaRegs()
        {
            ax = bx = cx = dx = si = di = bp = 0;
            flags = 0;
        }

        void Fetch()
        {
            curIstruzione = Code[ip];
        }

        async Task Execute()
        {
            string prefisso = curIstruzione.Prefisso;
            if (prefisso != null && cx == 0)    // REP con CX = 0: l'istruzione non viene eseguita
            {
                ripetizioneInCorso = false;
                ip++;
                return;
            }

            switch (curIstruzione.Code)
            {
                case "shl": Shl(); break;
                case "shr": Shr(); break;
                case "sar": Sar(); break;
                case "rol": Rol(); break;
                case "ror": Ror(); break;
                case "rcl": Rcl(); break;
                case "rcr": Rcr(); break;
                case "and": And(); break;
                case "or": Or(); break;
                case "xor": Xor(); break;
                case "test": Test(); break;
                case "not": Not(); break;
                case "neg": Neg(); break;
                case "mov": Mov(); break;
                case "movs": case "movsb": case "movsw": Movs(); break;
                case "lodsb": case "lodsw": Lods(); break;
                case "stosb": case "stosw": Stos(); break;
                case "cmpsb": case "cmpsw": Cmps(); break;
                case "scasb": case "scasw": Scas(); break;
                case "cld": SetFlag(DF, false); break;
                case "std": SetFlag(DF, true); break;
                case "xchg": Xchg(); break;
                case "lea": Lea(); break;
                case "nop": Nop(); break;
                case "add": Add(); break;
                case "adc": Adc(); break;
                case "sub": Sub(); break;
                case "sbb": Sbb(); break;
                case "mul": Mul(); break;
                case "imul": IMul(); break;
                case "div": Div(); break;
                case "idiv": IDiv(); break;
                case "cbw": ax = Basso(ax); break;
                case "cwd": dx = (short)(ax < 0 ? -1 : 0); break;
                case "cmp": Cmp(); break;
                case "clc": SetFlag(CF, false); break;
                case "stc": SetFlag(CF, true); break;
                case "cmc": SetFlag(CF, !TestFlag(CF)); break;
                case "jcxz": Jcxz(); break;
                case "jg": case "jnle": Jg(); break;
                case "jge": case "jnl": Jge(); break;
                case "jl": case "jnge": Jl(); break;
                case "jle": case "jng": Jle(); break;
                case "jne": case "jnz": Jne(); break;
                case "je": case "jz": Je(); break;
                case "ja": case "jnbe": SaltaSe(!TestFlag(CF) && !TestFlag(ZF)); break;
                case "jae": case "jnb": case "jnc": SaltaSe(!TestFlag(CF)); break;
                case "jb": case "jnae": case "jc": SaltaSe(TestFlag(CF)); break;
                case "jbe": case "jna": SaltaSe(TestFlag(CF) || TestFlag(ZF)); break;
                case "jmp": Jmp(); break;
                case "jo": Jo(); break;
                case "jno": Jno(); break;
                case "js": Js(); break;
                case "jns": Jns(); break;
                case "loop": Loop(true); break;
                case "loope": case "loopz": Loop(TestFlag(ZF)); break;
                case "loopne": case "loopnz": Loop(!TestFlag(ZF)); break;
                case "dec": Dec(); break;
                case "inc": Inc(); break;
                case "pop": Pop(); break;
                case "push": Push(); break;
                case "pushf": PushF(); break;
                case "popf": PopF(); break;
                case "call": Call(); break;
                case "ret": Ret(); break;
                case "stop": Stop(); break;
                case "int": await Int(); break;
            }

            if (prefisso != null)
            {
                // una ripetizione per esecuzione: finché deve continuare, IP resta sull'istruzione
                cx--;
                bool confronto = curIstruzione.Code.StartsWith("cmps") || curIstruzione.Code.StartsWith("scas");
                bool continua = cx != 0 && (!confronto ||
                    (prefisso is "repne" or "repnz" ? !TestFlag(ZF) : TestFlag(ZF)));
                ripetizioneInCorso = continua;
                if (continua)
                    return;
            }
            ip++;
        }

        #region istruzioni

        // AND, OR, XOR, TEST: CF = OF = 0
        void Logica(Func<int, int, int> operazione, bool memorizza)
        {
            int ris = operazione(LoadOp(1), LoadOp(2));
            if (memorizza)
                StoreOp(ris, 1);
            SetFlags(ZF + SF, ris);
            SetFlag(OF, false);
            SetFlag(CF, false);
        }

        void And() => Logica((a, b) => a & b, true);
        void Or() => Logica((a, b) => a | b, true);
        void Xor() => Logica((a, b) => a ^ b, true);
        void Test() => Logica((a, b) => a & b, false);

        void Not()
        {
            int op = LoadOp(1);
            op = (short)~op;
            StoreOp(op, 1);
        }

        void Neg()
        {
            int op = LoadOp(1);
            int ris = -op;
            StoreOp(ris, 1);
            SetFlags(ris);
            SetFlag(CF, op != 0);
        }

        void Mov()
        {
            StoreOp(LoadOp(2), 1);
        }

        // Istruzioni stringa: SI e DI avanzano (DF = 0) o arretrano (DF = 1) di un elemento:
        // un byte per le forme ...b (AL), una parola per le forme ...w (AX).
        // Nella modalità a parole un elemento è sempre una cella; le forme ...b usano il suo byte basso.
        short Passo => (short)((TestFlag(DF) ? -1 : 1) * (Larghezza == 8 ? 1 : memoria.Modello.PassoParola));

        void Movs()
        {
            ScriviMemoria(di, LeggiMemoriaOp(si));
            si += Passo;
            di += Passo;
        }

        void Lods()
        {
            ax = Larghezza == 8 ? ConBasso(ax, LeggiMemoriaOp(si)) : LeggiMemoriaOp(si);
            si += Passo;
        }

        void Stos()
        {
            ScriviMemoria(di, ax);
            di += Passo;
        }

        // CMPS: confronta [SI] con [DI] come CMP [SI], [DI]
        void Cmps()
        {
            ImpostaFlagDifferenza(LeggiMemoriaOp(si), LeggiMemoriaOp(di), 0);
            si += Passo;
            di += Passo;
        }

        // SCAS: confronta AL/AX con [DI] come CMP AL, [DI]
        void Scas()
        {
            ImpostaFlagDifferenza(Adatta(ax), LeggiMemoriaOp(di), 0);
            di += Passo;
        }

        void Xchg()
        {
            short op1 = LoadOp(1);
            short op2 = LoadOp(2);
            StoreOp(op2, 1);
            StoreOp(op1, 2);
        }

        // LEA: carica nel registro l'indirizzo dell'operando in memoria, non il suo contenuto
        void Lea()
        {
            StoreOp(IndirizzoEffettivo(curIstruzione.Op2), 1);
        }

        // valore senza segno sulla dimensione dell'istruzione (0..255 oppure 0..65535)
        int Maschera => Larghezza == 8 ? 0xFF : 0xFFFF;
        int SenzaSegno(int valore) => valore & Maschera;

        // ADD/ADC: CF = riporto oltre il bit più significativo
        void Somma(int riporto)
        {
            int a = LoadOp(1), b = LoadOp(2);
            int ris = a + b + riporto;
            StoreOp(ris, 1);
            SetFlags(ris);
            SetFlag(CF, SenzaSegno(a) + SenzaSegno(b) + riporto > Maschera);
        }

        // SUB/SBB/CMP: CF = prestito (primo operando minore del secondo, senza segno)
        void Differenza(int prestito, bool memorizza)
        {
            int ris = ImpostaFlagDifferenza(LoadOp(1), LoadOp(2), prestito);
            if (memorizza)
                StoreOp(ris, 1);
        }

        int ImpostaFlagDifferenza(int a, int b, int prestito)
        {
            int ris = a - b - prestito;
            SetFlags(ris);
            SetFlag(CF, SenzaSegno(a) < SenzaSegno(b) + prestito);
            return ris;
        }

        int Carry => TestFlag(CF) ? 1 : 0;

        void Add() => Somma(0);
        void Adc() => Somma(Carry);
        void Sub() => Differenza(0, true);
        void Sbb() => Differenza(Carry, true);
        void Cmp() => Differenza(0, false);

        // MUL: senza segno. A 8 bit AX = AL * op, a 16 bit DX:AX = AX * op.
        // CF = OF = 1 se la metà alta del risultato non è zero.
        void Mul()
        {
            if (Larghezza == 8)
            {
                int prodotto = (ax & 0xFF) * SenzaSegno(LoadOp(1));
                ax = (short)prodotto;
                ImpostaFlagMoltiplicazione(prodotto, prodotto > 0xFF);
                return;
            }
            uint tmp = (uint)(ushort)ax * (uint)SenzaSegno(LoadOp(1));
            ax = (short)tmp;
            dx = (short)(tmp >> 16);
            ImpostaFlagMoltiplicazione((int)tmp, dx != 0);
        }

        // IMUL: con segno. CF = OF = 1 se il risultato non sta nella metà bassa.
        void IMul()
        {
            if (Larghezza == 8)
            {
                int prodotto = Basso(ax) * LoadOp(1);
                ax = (short)prodotto;
                ImpostaFlagMoltiplicazione(prodotto, prodotto != (sbyte)prodotto);
                return;
            }
            int tmp = ax * LoadOp(1);
            ax = (short)tmp;
            dx = (short)(tmp >> 16);
            ImpostaFlagMoltiplicazione(tmp, tmp != (short)tmp);
        }

        void ImpostaFlagMoltiplicazione(int prodotto, bool metaAlta)
        {
            SetFlags(ZF, prodotto);
            SetFlag(OF, metaAlta);
            SetFlag(CF, metaAlta);
        }

        // DIV: senza segno. A 8 bit AL = AX / op, AH = AX % op; a 16 bit AX = DX:AX / op, DX = DX:AX % op.
        void Div()
        {
            int divisore = SenzaSegno(LoadOp(1));
            if (divisore == 0)
                throw new CpuException(CodiceErrore.DivisionePerZero);

            if (Larghezza == 8)
            {
                int dividendo = (ushort)ax;
                int quoziente = dividendo / divisore;
                if (quoziente > 0xFF)
                    throw new CpuException(CodiceErrore.QuozienteFuoriIntervallo);
                ax = ConAlto(ConBasso(ax, (short)quoziente), (short)(dividendo % divisore));
                return;
            }
            uint dividendo32 = ((uint)(ushort)dx << 16) | (ushort)ax;
            uint quoziente32 = dividendo32 / (uint)divisore;
            if (quoziente32 > 0xFFFF)
                throw new CpuException(CodiceErrore.QuozienteFuoriIntervallo);
            dx = (short)(dividendo32 % (uint)divisore);
            ax = (short)quoziente32;
        }

        // IDIV: con segno, stessi registri di DIV. Il resto ha il segno del dividendo.
        void IDiv()
        {
            int divisore = LoadOp(1);
            if (divisore == 0)
                throw new CpuException(CodiceErrore.DivisionePerZero);

            if (Larghezza == 8)
            {
                int quoziente = ax / divisore;
                if (quoziente < sbyte.MinValue || quoziente > sbyte.MaxValue)
                    throw new CpuException(CodiceErrore.QuozienteFuoriIntervallo);
                ax = ConAlto(ConBasso(ax, (short)quoziente), (short)(ax % divisore));
                return;
            }
            long dividendo = (int)(((uint)(ushort)dx << 16) | (ushort)ax);
            long quoziente16 = dividendo / divisore;
            if (quoziente16 < short.MinValue || quoziente16 > short.MaxValue)
                throw new CpuException(CodiceErrore.QuozienteFuoriIntervallo);
            dx = (short)(dividendo % divisore);
            ax = (short)quoziente16;
        }

        // INC e DEC non modificano CF
        void Inc()
        {
            int op = LoadOp(1);
            op++;
            StoreOp(op, 1);
            SetFlags(op);
        }

        void Dec()
        {
            int op = LoadOp(1);
            op--;
            StoreOp(op, 1);
            SetFlags(op);
        }

        void SaltaSe(bool condizione)
        {
            if (condizione)
                ip = NuovoIp();
        }

        // LOOP: decrementa CX (senza modificare i flag) e salta se CX != 0 e la condizione è vera
        void Loop(bool condizione)
        {
            cx--;
            SaltaSe(cx != 0 && condizione);
        }

        void Jmp()
        {
            ip = NuovoIp();
        }

        void Je()
        {
            if (TestFlag(ZF))
                ip = NuovoIp();
        }

        void Jne()
        {
            if (!TestFlag(ZF))
                ip = NuovoIp();
        }

        void Jg()
        {
            if (TestFlag(SF) == TestFlag(OF) && !TestFlag(ZF))
                ip = NuovoIp();
        }

        void Jge()
        {
            if (TestFlag(SF) == TestFlag(OF))
                ip = NuovoIp();
        }

        void Jl()
        {
            if (TestFlag(SF) != TestFlag(OF))
                ip = NuovoIp();
        }

        void Jle()
        {
            if (TestFlag(SF) != TestFlag(OF) || TestFlag(ZF))
                ip = NuovoIp();
        }

        void Jcxz()
        {
            if (cx == 0)
                ip = NuovoIp();
        }

        void Jo()
        {
            if (TestFlag(OF))
                ip = NuovoIp();
        }

        void Jno()
        {
            if (!TestFlag(OF))
                ip = NuovoIp();
        }

        void Js()
        {
            if (TestFlag(SF))
                ip = NuovoIp();
        }

        void Jns()
        {
            if (!TestFlag(SF))
                ip = NuovoIp();
        }

        void PushCode(short valore)
        {
            sp -= (short)memoria.Modello.PassoParola;
            if (sp < memoria.Modello.InizioStack)
                throw new CpuException(CodiceErrore.StackOverflow);
            memoria.ScriviParola(sp, valore);
        }

        short PopCode()
        {
            if (sp == memoria.Modello.Dimensione)
                throw new CpuException(CodiceErrore.StackUnderflow);
            short valore = memoria.LeggiParola(sp);
            sp += (short)memoria.Modello.PassoParola;
            return valore;
        }

        void Push()
        {
            PushCode(LoadOp(1));
        }

        void Pop()
        {
            StoreOp(PopCode(), 1);
        }

        void PushF()
        {
            PushCode(flags);
        }

        void PopF()
        {
            flags = PopCode();
        }

        void Call()
        {
            PushCode(ip);
            ip = NuovoIp();
        }

        // RET n: dopo il ritorno rimuove n parametri dallo stack
        void Ret()
        {
            ip = PopCode();
            if (curIstruzione.Op1.Tipo != TipoOperando.Costante)
                return;
            int nuovoSp = sp + curIstruzione.Op1.Scostamento;
            if (nuovoSp > memoria.Modello.Dimensione)
                throw new CpuException(CodiceErrore.StackUnderflow);
            sp = (short)nuovoSp;
        }

        // Shift e rotazioni: il conteggio usa solo i 5 bit bassi, come su x86; con conteggio 0 i flag non cambiano.
        // CF riceve l'ultimo bit uscito; OF è definito solo per conteggio 1.
        int Conteggio => LoadOp(2) & 0x1F;
        int BitAlto(int valore) => (valore >> (Larghezza - 1)) & 1;

        void Shl()
        {
            int n = Conteggio;
            if (n == 0) return;
            int op = SenzaSegno(LoadOp(1));
            int ris = op << n;
            StoreOp(ris, 1);
            SetFlags(ZF + SF, ris);
            SetFlag(CF, ((ris >> Larghezza) & 1) != 0);
            if (n == 1) SetFlag(OF, (BitAlto(ris) ^ Carry) != 0);
        }

        // SHR: shift logico, entrano zeri a sinistra
        void Shr()
        {
            int n = Conteggio;
            if (n == 0) return;
            int op = SenzaSegno(LoadOp(1));
            int ris = op >> n;
            StoreOp(ris, 1);
            SetFlags(ZF + SF, ris);
            SetFlag(CF, ((op >> (n - 1)) & 1) != 0);
            if (n == 1) SetFlag(OF, BitAlto(op) != 0);
        }

        // SAR: shift aritmetico, conserva il segno
        void Sar()
        {
            int n = Conteggio;
            if (n == 0) return;
            int op = LoadOp(1);
            int ris = op >> n;
            StoreOp(ris, 1);
            SetFlags(ZF + SF, ris);
            SetFlag(CF, ((op >> (n - 1)) & 1) != 0);
            if (n == 1) SetFlag(OF, false);
        }

        // Rotazioni: modificano solo CF (e OF per conteggio 1)
        void Ruota(bool sinistra, bool attraversoCarry)
        {
            int n = Conteggio;
            if (n == 0) return;
            int op = SenzaSegno(LoadOp(1));
            int carry = Carry;
            for (int i = 0; i < n; i++)
            {
                int uscito = sinistra ? BitAlto(op) : op & 1;
                int entrante = attraversoCarry ? carry : uscito;
                op = sinistra
                    ? SenzaSegno((op << 1) | entrante)
                    : (op >> 1) | (entrante << (Larghezza - 1));
                carry = uscito;
            }
            StoreOp(op, 1);
            SetFlag(CF, carry != 0);
            if (n == 1)
                SetFlag(OF, sinistra
                    ? (BitAlto(op) ^ carry) != 0
                    : (BitAlto(op) ^ ((op >> (Larghezza - 2)) & 1)) != 0);
        }

        void Rol() => Ruota(sinistra: true, attraversoCarry: false);
        void Ror() => Ruota(sinistra: false, attraversoCarry: false);
        void Rcl() => Ruota(sinistra: true, attraversoCarry: true);
        void Rcr() => Ruota(sinistra: false, attraversoCarry: true);

        void Nop()
        {
        }

        public void Stop()
        {
            stop = true;
            Stato = StatoCpu.Ferma;
        }

        #endregion

        #region interrupt / IO

        // Buffer tastiera thread-safe: riempito dall'host (UI) via InviaCarattereTastiera,
        // consumato dalla CPU durante l'esecuzione di "int 21h" con i servizi di lettura.
        readonly System.Collections.Concurrent.ConcurrentQueue<short> bufferTastiera = new();

        // Evento verso l'host: la CPU lo invoca per ogni carattere da stampare su console.
        public event Action<char> ScriviSuConsole;

        // Evento verso l'host: invocato a ogni "int" valido, per aprire automaticamente il pannello Console.
        public event Action InterruptRichiesto;

        // Eventi verso l'host: delimitano l'attesa di un tasto (int 21h, servizi di lettura), per un cursore lampeggiante.
        public event Action AttesaTastieraIniziata;
        public event Action AttesaTastieraTerminata;

        // API pubblica chiamata dall'host quando l'utente preme un tasto nel pannello Console.
        public void InviaCarattereTastiera(short carattere) => bufferTastiera.Enqueue(carattere);

        async Task Int()
        {
            int numero = LoadOp(1); // numero di interrupt (operando costante)
            switch (numero)
            {
                case 0x21:
                    InterruptRichiesto?.Invoke();
                    await ServizioSistema();
                    break;
                default:
                    throw new CpuException(CodiceErrore.InterruptNonValido);
            }
        }

        // Servizi DOS int 21h, selezionati da AH:
        //   01h -> legge un carattere da tastiera (bloccante) con eco su console, risultato in AL
        //   02h -> scrive su console il carattere in DL
        //   07h -> legge un carattere da tastiera (bloccante) senza eco, risultato in AL
        //   09h -> scrive su console la stringa terminata da '$' che inizia all'indirizzo in DX
        //   0Ah -> legge una riga nel buffer all'indirizzo DX: [DX] = caratteri massimi (Invio compreso),
        //          [DX+1] = caratteri letti (Invio escluso), da [DX+2] i caratteri seguiti da 13;
        //          Backspace (8) cancella l'ultimo carattere
        //   4Ch -> termina il programma
        // Memoria a celle da 16 bit: ogni carattere occupa il byte basso di una cella.
        async Task ServizioSistema()
        {
            switch ((ax >> 8) & 0xFF)
            {
                case 0x01:
                    short letto = await LeggiCarattereBloccante();
                    ax = ConBasso(ax, letto);
                    ScriviCarattere(letto);
                    break;
                case 0x02:
                    ScriviCarattere(dx & 0xFF);
                    break;
                case 0x07:
                    ax = ConBasso(ax, await LeggiCarattereBloccante());
                    break;
                case 0x09:
                    for (int i = dx; (memoria.LeggiByte(i) & 0xFF) != '$'; i++)
                        ScriviCarattere(memoria.LeggiByte(i) & 0xFF);
                    break;
                case 0x0A:
                    await LeggiRiga(dx);
                    break;
                case 0x4C:
                    Stop();
                    break;
                default:
                    throw new CpuException(CodiceErrore.ServizioNonValido);
            }
        }

        async Task LeggiRiga(int buffer)
        {
            int massimo = memoria.LeggiByte(buffer) & 0xFF;
            if (massimo == 0)
                return;
            int letti = 0;
            while (true)
            {
                short c = await LeggiCarattereBloccante();
                if (stop)
                    return;
                if (c == 13)
                    break;
                if (c == 8)                 // Backspace: cancella l'ultimo carattere letto, se c'è
                {
                    if (letti > 0)
                    {
                        letti--;
                        ScriviCarattere(8);
                    }
                }
                else if (letti < massimo - 1)    // a buffer pieno i caratteri vengono ignorati fino all'Invio
                {
                    memoria.ScriviByte(buffer + 2 + letti, c);
                    letti++;
                    ScriviCarattere(c);
                }
            }
            memoria.ScriviByte(buffer + 2 + letti, 13);
            memoria.ScriviByte(buffer + 1, (short)letti);
            ScriviCarattere(13);
        }

        // Invia un carattere alla console. CR (13) va a capo; LF (10) va a capo solo se non
        // segue un CR, così la coppia 13, 10 dei programmi DOS produce un solo ritorno a capo.
        bool ultimoCR;
        void ScriviCarattere(int c)
        {
            bool dopoCR = ultimoCR;
            ultimoCR = c == 13;
            if (c == 13 || (c == 10 && !dopoCR))
                ScriviSuConsole?.Invoke('\n');
            else if (c != 10)
                ScriviSuConsole?.Invoke((char)c);
        }

        // Attende in modo cooperativo (senza bloccare il thread) finché non arriva un carattere
        // o la CPU viene fermata. Task.Delay invece di Thread.Sleep: su runtime single-thread
        // (WASM/Browser) un'attesa sincrona bloccherebbe l'unico thread disponibile, congelando
        // l'intera UI/tab; l'await cede il controllo al chiamante tra un tentativo e l'altro.
        async Task<short> LeggiCarattereBloccante()
        {
            AttesaTastieraIniziata?.Invoke();
            try
            {
                short c;
                while (!bufferTastiera.TryDequeue(out c))
                {
                    if (stop) return 0;
                    await Task.Delay(1);
                }
                return c;
            }
            finally
            {
                AttesaTastieraTerminata?.Invoke();
            }
        }

        short NuovoIp()
        {
            return (short)(LoadOp(1) - 1);
        }

        #endregion

        #region load/store/flags

        short LoadOp(int numOp)
        {
            if (numOp == 1)
                return LoadOp(curIstruzione.Op1);
            else
                return LoadOp(curIstruzione.Op2);
        }

        void StoreOp(int valore, int numOp)
        {
            if (numOp == 1)
                StoreOp((short)valore, curIstruzione.Op1);
            else
                StoreOp((short)valore, curIstruzione.Op2);
        }

        // dimensione dell'istruzione corrente (8 o 16 bit)
        int Larghezza => curIstruzione.Larghezza;

        // Riporta un valore alla dimensione dell'istruzione, con estensione del segno
        short Adatta(int valore) => Larghezza == 8 ? (sbyte)valore : (short)valore;

        short LoadOp(Operando op)
        {
            switch (op.Tipo)
            {
                case TipoOperando.Registro: return LeggiRegistro(op.Base);
                case TipoOperando.Indiretto: return LeggiMemoriaOp(IndirizzoEffettivo(op));
                case TipoOperando.Costante: return Adatta(op.Scostamento);
                case TipoOperando.Memoria: return LeggiMemoriaOp(op.Scostamento);
                case TipoOperando.Etichetta: return (short)op.Scostamento;
            }
            return -1;
        }

        void StoreOp(short valore, Operando op)
        {
            switch (op.Tipo)
            {
                case TipoOperando.Registro: ScriviRegistro(op.Base, valore); break;
                case TipoOperando.Indiretto: ScriviMemoria(IndirizzoEffettivo(op), valore); break;
                case TipoOperando.Memoria: ScriviMemoria(op.Scostamento, valore); break;
            }
        }

        // [base + indice + scostamento] oppure [indirizzo]
        int IndirizzoEffettivo(Operando op)
        {
            if (op.Tipo != TipoOperando.Indiretto)
                return op.Scostamento;
            return LeggiRegistro(op.Base) + (op.HaIndice ? LeggiRegistro(op.Indice) : 0) + op.Scostamento;
        }

        // Accesso alla memoria con la dimensione dell'istruzione corrente (byte con estensione del segno, o parola)
        short LeggiMemoriaOp(int indirizzo) =>
            Larghezza == 8 ? memoria.LeggiByte(indirizzo) : memoria.LeggiParola(indirizzo);

        void ScriviMemoria(int indirizzo, short valore)
        {
            if (Larghezza == 8)
                memoria.ScriviByte(indirizzo, valore);
            else
                memoria.ScriviParola(indirizzo, valore);
        }

        // byte basso/alto di una parola, con estensione del segno
        static short Basso(short parola) => (sbyte)parola;
        static short Alto(short parola) => (sbyte)(parola >> 8);

        // parola con il byte basso/alto sostituito da quello di valore
        static short ConBasso(short parola, short valore) => (short)((parola & 0xFF00) | (valore & 0xFF));
        static short ConAlto(short parola, short valore) => (short)((parola & 0x00FF) | ((valore & 0xFF) << 8));

        short LeggiRegistro(Registro reg)
        {
            switch (reg)
            {
                case Registro.ax: return ax;
                case Registro.bx: return bx;
                case Registro.cx: return cx;
                case Registro.dx: return dx;
                case Registro.si: return si;
                case Registro.di: return di;
                case Registro.bp: return bp;
                case Registro.sp: return sp;
                case Registro.al: return Basso(ax);
                case Registro.ah: return Alto(ax);
                case Registro.bl: return Basso(bx);
                case Registro.bh: return Alto(bx);
                case Registro.cl: return Basso(cx);
                case Registro.ch: return Alto(cx);
                case Registro.dl: return Basso(dx);
                case Registro.dh: return Alto(dx);
            }
            return -1;
        }

        void ScriviRegistro(Registro reg, short valore)
        {
            switch (reg)
            {
                case Registro.ax: ax = valore; break;
                case Registro.bx: bx = valore; break;
                case Registro.cx: cx = valore; break;
                case Registro.dx: dx = valore; break;
                case Registro.si: si = valore; break;
                case Registro.di: di = valore; break;
                case Registro.bp: bp = valore; break;
                case Registro.sp: sp = valore; break;
                case Registro.al: ax = ConBasso(ax, valore); break;
                case Registro.ah: ax = ConAlto(ax, valore); break;
                case Registro.bl: bx = ConBasso(bx, valore); break;
                case Registro.bh: bx = ConAlto(bx, valore); break;
                case Registro.cl: cx = ConBasso(cx, valore); break;
                case Registro.ch: cx = ConAlto(cx, valore); break;
                case Registro.dl: dx = ConBasso(dx, valore); break;
                case Registro.dh: dx = ConAlto(dx, valore); break;
            }
        }

        // flag calcolati sulla dimensione dell'istruzione: a 8 bit SF è il bit 7 e OF indica l'uscita da -128..127
        void SetFlags(short mask, int ris)
        {
            short troncato = Adatta(ris);

            if ((ZF & mask) != 0)
                flags = (short)((troncato == 0) ? flags | ZF : flags & ~ZF);

            if ((SF & mask) != 0)
                flags = (short)((troncato < 0) ? flags | SF : flags & ~SF);

            if ((OF & mask) != 0)
                flags = (short)((ris != troncato) ? flags | OF : flags & ~OF);
        }

        void SetFlags(int ris)
        {
            SetFlags(TUTTI, ris);
        }

        void SetFlag(int flag, bool stato)
        {
            if (stato)
                flags |= (short)flag;
            else
                flags &= (short)~flag;
        }

        public bool TestFlag(int flag)
        {
            return (flags & flag) != 0;
        }

        #endregion

        #region dump / utilità

        public string DumpReg(int indReg)
        {
            string formatoReg;
            if (Ambiente.FormatoDati != FormatoValore.Hex)
                formatoReg = " =  [HEX: {0" + Ambiente.FRHEX + "}] [BIN: {1}] [CAR: {2}]";
            else
                formatoReg = " =  [DEC: {0" + Ambiente.FRDEC + "}] [BIN: {1}] [CAR: {2}]";
            short tmp;
            switch (indReg)
            {
                case 0: tmp = ax; break;
                case 1: tmp = bx; break;
                case 2: tmp = cx; break;
                case 3: tmp = dx; break;
                case 4: tmp = si; break;
                case 5: tmp = di; break;
                case 6: tmp = bp; break;
                case 7: tmp = sp; break;
                case 8: tmp = ip; break;
                default: tmp = 0; break;
            }
            return string.Format(nomiRegs[indReg] + formatoReg, tmp, ShortToStrBin(tmp), IntToChar(tmp));
        }

        public static string ShortToStrBin(short valore)
        {
            StringBuilder s = new StringBuilder();
            int v = (ushort)valore;
            int r = v % 2;
            v = v / 2;
            while (v != 0)
            {
                s.Insert(0, r);
                r = v % 2;
                v = v / 2;
            }
            s.Insert(0, r);
            while (s.Length < 16)
                s.Insert(0, 0);
            return s.ToString();
        }

        public string[] DumpRegs()
        {
            string formatoReg = " = {0" + Ambiente.FR + "} ";
            string[] reg = new string[9];

            reg[0] = string.Format("AX" + formatoReg, ax) + DumpByte("AH", "AL", ax);
            reg[1] = string.Format("BX" + formatoReg, bx) + DumpByte("BH", "BL", bx);
            reg[2] = string.Format("CX" + formatoReg, cx) + DumpByte("CH", "CL", cx);
            reg[3] = string.Format("DX" + formatoReg, dx) + DumpByte("DH", "DL", dx);
            reg[4] = string.Format("SI" + formatoReg, si);
            reg[5] = string.Format("DI" + formatoReg, di);
            reg[6] = string.Format("BP" + formatoReg, bp);
            reg[7] = string.Format("SP" + formatoReg, sp);
            reg[8] = string.Format("IP" + formatoReg, ip);
            return reg;
        }

        // "[AH=01 AL=41]": byte alto e basso di un registro a 16 bit
        static string DumpByte(string nomeAlto, string nomeBasso, short parola)
        {
            string formato = (Ambiente.FormatoDati == FormatoValore.Hex) ? "{0:X2}" : "{0,4:0}";
            string Byte(short b) => (Ambiente.FormatoDati == FormatoValore.Hex) ? string.Format(formato, (byte)b) : string.Format(formato, b);
            return string.Format("[{0}={1} {2}={3}]", nomeAlto, Byte(Alto(parola)), nomeBasso, Byte(Basso(parola)));
        }

        static string IntToChar(int x)
        {
            if (x == 0)
                return Ambiente.FormatoCarZero;
            if (x < 0)
                x = 128;
            return Convert.ToChar(x).ToString();
        }

        // Una riga per nome della sezione dati: "vet  [000A] DW x5 = 1", "N  EQU 5";
        // in fondo le etichette del codice: "fine  ETICHETTA 7" (numero dell'istruzione).
        // Per le variabili mostra il valore corrente della prima cella (byte basso per DB).
        public List<string> DumpSimboli(IEnumerable<Simbolo> simboli)
        {
            string formatoIndirizzo = "{0" + Ambiente.FI + "}";
            string formatoDato = "{0" + Ambiente.FD + "}";
            var dump = new List<string>();
            foreach (var sim in simboli)
            {
                if (sim.Tipo is TipoSimbolo.Equ or TipoSimbolo.Etichetta)
                {
                    dump.Add(string.Format("{0,-10} {1} {2}", sim.Grafia ?? sim.Nome,
                        sim.Tipo == TipoSimbolo.Equ ? "EQU" : "ETICHETTA", sim.Valore));
                    continue;
                }
                short valore = memoria.LeggiParola(sim.Valore);
                if (sim.Tipo == TipoSimbolo.Db)
                    valore = (short)(valore & 0xFF);
                string testo = Ambiente.FormatoDati == FormatoValore.Car
                    ? IntToChar(valore)
                    : string.Format(formatoDato, valore).Trim();
                string celle = sim.Celle > 1 ? " x" + sim.Celle : "";
                dump.Add(string.Format("{0,-10} [{1}] {2}{3} = {4}", sim.Grafia ?? sim.Nome,
                    string.Format(formatoIndirizzo, sim.Valore).Trim(),
                    sim.Tipo == TipoSimbolo.Db ? "DB" : "DW", celle, testo));
            }
            return dump;
        }

        public List<string> DumpMemoria(int da, int a, int colonne)
        {
            if (memoria == null) return null;
            string formatoIndirizzo = "{0" + Ambiente.FI + "}: ";
            string formatoDato = "{0" + Ambiente.FD + "} ";

            List<string> dump = new List<string>();
            string s = string.Format(formatoIndirizzo, da);
            for (int i = da; i < a;)
            {
                if (Ambiente.FormatoDati == FormatoValore.Car)
                    s = s + string.Format(formatoDato, IntToChar(memoria.LeggiParola(i)));
                else
                    s = s + string.Format(formatoDato, memoria.LeggiParola(i));

                if ((++i - da) % colonne == 0)
                {
                    dump.Add(s);
                    s = string.Format(formatoIndirizzo, i);
                }
            }
            if (s.Length > 6)
                dump.Add(s);
            return dump;
        }

        #endregion
    }
}
