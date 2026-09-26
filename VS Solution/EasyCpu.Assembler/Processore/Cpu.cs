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

        // bit del registro dei flags
        const short ZF = 1;
        const short SF = 2;
        const short OF = 4;

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

        Ram memoria = new Ram();
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

        public short LeggiMemoria(int indirizzo) => memoria[indirizzo];

        public bool FlagSegno => TestFlag(SF);
        public bool FlagZero => TestFlag(ZF);
        public bool FlagOverflow => TestFlag(OF);

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
                    if (Breakpoints.Contains(ip))
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
                if (Breakpoints.Contains(ip))
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

        public void Init(List<Instruction> codice, List<int> memoriaDati, bool initRegs, int AloopInfinito)
        {
            stop = false;
            ultimoCR = false;
            bufferTastiera.Clear();
            memoria.Imposta(memoriaDati);
            Code = codice;
            ip = 0;
            flags = 0;
            sp = (short)(Ram.MASSIMO_INDIRIZZO + 1);
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
            switch (curIstruzione.Code)
            {
                case "shl": Shl(); break;
                case "shr": Shr(); break;
                case "and": And(); break;
                case "or": Or(); break;
                case "xor": Xor(); break;
                case "not": Not(); break;
                case "neg": Neg(); break;
                case "mov": Mov(); break;
                case "movs": Movs(); break;
                case "nop": Nop(); break;
                case "add": Add(); break;
                case "sub": Sub(); break;
                case "mul": IMul(); break;
                case "div": IDiv(); break;
                case "cmp": Cmp(); break;
                case "jcxz": Jcxz(); break;
                case "jg": Jg(); break;
                case "jge": Jge(); break;
                case "jl": Jl(); break;
                case "jle": Jle(); break;
                case "jne": Jne(); break;
                case "je": Je(); break;
                case "jmp": Jmp(); break;
                case "jo": Jo(); break;
                case "jno": Jno(); break;
                case "js": Js(); break;
                case "jns": Jns(); break;
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
            ip++;
        }

        #region istruzioni

        void And()
        {
            int op = LoadOp(1);
            op &= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
            SetFlag(OF, false);
        }

        void Or()
        {
            int op = LoadOp(1);
            op |= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
            SetFlag(OF, false);
        }

        void Xor()
        {
            short op = LoadOp(1);
            op ^= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
            SetFlag(OF, false);
        }

        void Not()
        {
            int op = LoadOp(1);
            op = (short)~op;
            StoreOp(op, 1);
        }

        void Neg()
        {
            short op = LoadOp(1);
            op = (short)(0 - op);
            StoreOp(op, 1);
            SetFlags(op);
        }

        void Mov()
        {
            StoreOp(LoadOp(2), 1);
        }

        void Movs()
        {
            memoria[di] = memoria[si];
        }

        void Add()
        {
            int op = LoadOp(1);
            op += LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
        }

        void Sub()
        {
            int op = LoadOp(1);
            op -= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(op);
        }

        void IMul()
        {
            if (Larghezza == 8)
            {
                int prodotto = Basso(ax) * LoadOp(1);  // AX = AL * op
                ax = (short)prodotto;
                SetFlags(OF + ZF, prodotto);
                return;
            }
            int tmp = ax * LoadOp(1);
            ax = (short)tmp;
            dx = (short)(tmp >> 16);
            SetFlags(OF + ZF, tmp);
        }

        void IDiv()
        {
            int divisore = LoadOp(1);
            if (divisore == 0)
                throw new CpuException(CodiceErrore.DivisionePerZero);

            if (Larghezza == 8)
            {
                int quoziente = ax / divisore;          // AL = AX / op, AH = AX % op
                if (quoziente < sbyte.MinValue || quoziente > sbyte.MaxValue)
                    throw new CpuException(CodiceErrore.QuozienteFuoriIntervallo);
                ax = ConAlto(ConBasso(ax, (short)quoziente), (short)(ax % divisore));
                return;
            }
            long dividendo = ax + ((long)dx << 16);
            long quoziente16 = dividendo / divisore;
            if (quoziente16 < short.MinValue || quoziente16 > short.MaxValue)
                throw new CpuException(CodiceErrore.QuozienteFuoriIntervallo);
            dx = (short)(dividendo % divisore);
            ax = (short)quoziente16;
        }

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

        void Cmp()
        {
            int op = LoadOp(1);
            op -= LoadOp(2);
            SetFlags(op);
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
            sp--;
            if (sp < Ram.INDIRIZZO_STACK)
                throw new CpuException(CodiceErrore.StackOverflow);
            memoria[sp] = valore;
        }

        short PopCode()
        {
            if (sp == Ram.MASSIMO_INDIRIZZO + 1)
                throw new CpuException(CodiceErrore.StackUnderflow);
            return memoria[sp++];
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

        void Ret()
        {
            ip = PopCode();
        }

        void Shl()
        {
            int op = LoadOp(1);
            op <<= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(ZF + SF, op);
        }

        void Shr()
        {
            int op = LoadOp(1);
            op >>= LoadOp(2);
            StoreOp(op, 1);
            SetFlags(ZF + SF, op);
        }

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
        //          [DX+1] = caratteri letti (Invio escluso), da [DX+2] i caratteri seguiti da 13
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
                    for (int i = dx; (memoria[i] & 0xFF) != '$'; i++)
                        ScriviCarattere(memoria[i] & 0xFF);
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
            int massimo = memoria[buffer] & 0xFF;
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
                if (letti < massimo - 1)    // a buffer pieno i caratteri vengono ignorati fino all'Invio
                {
                    memoria[buffer + 2 + letti] = (short)(c & 0xFF);
                    letti++;
                    ScriviCarattere(c);
                }
            }
            memoria[buffer + 2 + letti] = 13;
            memoria[buffer + 1] = (short)letti;
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
                case TipoOperando.Indiretto: return Adatta(memoria[LeggiRegistro(op.Base) + op.Scostamento]);
                case TipoOperando.Costante: return Adatta(op.Scostamento);
                case TipoOperando.Memoria: return Adatta(memoria[op.Scostamento]);
                case TipoOperando.Etichetta: return (short)op.Scostamento;
            }
            return -1;
        }

        void StoreOp(short valore, Operando op)
        {
            switch (op.Tipo)
            {
                case TipoOperando.Registro: ScriviRegistro(op.Base, valore); break;
                case TipoOperando.Indiretto: ScriviMemoria(LeggiRegistro(op.Base) + op.Scostamento, valore); break;
                case TipoOperando.Memoria: ScriviMemoria(op.Scostamento, valore); break;
            }
        }

        // Memoria a celle da 16 bit: un accesso a 8 bit scrive solo il byte basso della cella
        void ScriviMemoria(int indirizzo, short valore)
        {
            memoria[indirizzo] = Larghezza == 8 ? ConBasso(memoria[indirizzo], valore) : valore;
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

        // Una riga per nome della sezione dati: "vet  [000A] DW x5 = 1", "N  EQU 5".
        // Per le variabili mostra il valore corrente della prima cella (byte basso per DB).
        public List<string> DumpSimboli(IEnumerable<Simbolo> simboli)
        {
            string formatoIndirizzo = "{0" + Ambiente.FI + "}";
            string formatoDato = "{0" + Ambiente.FD + "}";
            var dump = new List<string>();
            foreach (var sim in simboli)
            {
                if (sim.Tipo == TipoSimbolo.Equ)
                {
                    dump.Add(string.Format("{0,-10} EQU {1}", sim.Grafia ?? sim.Nome, sim.Valore));
                    continue;
                }
                short valore = memoria[sim.Valore];
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
                    s = s + string.Format(formatoDato, IntToChar(memoria[i]));
                else
                    s = s + string.Format(formatoDato, memoria[i]);

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
