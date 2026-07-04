using System;
using System.Text;
using System.Collections;
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
            get { return ip > Code.Count || ip < 1; }
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

        public bool FlagSegno => TestFlag(SF);
        public bool FlagZero => TestFlag(ZF);
        public bool FlagOverflow => TestFlag(OF);

        public void Run(int IP)
        {
            ip = (short)IP;
            Run();
        }

        static bool LoopInfinito(int numIstruzioni, int limite)
        {
            return limite > 0 && numIstruzioni == limite;
        }

        public void Run()
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
                    Execute();

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
        public void StepInto()
        {
            if (stop) return;
            Stato = StatoCpu.Attiva;
            try
            {
                Fetch();
                Execute();
            }
            catch (Exception)
            {
                Stop();
                throw;
            }
            if (ip == Code.Count)
                Stop();
        }

        // Esegue finché sp < limite, controllando breakpoint e loop infinito
        void RunWhileInside(short limite)
        {
            int numIstruzioni = 0;
            while (!stop && sp < limite)
            {
                if (Breakpoints.Contains(ip))
                    throw new CpuTrapException();

                Fetch();
                Execute();

                if (ip == Code.Count) { Stop(); break; }
                if (IPOverRun) throw new CpuException(CodiceErrore.IPNonValido);

                numIstruzioni++;
                if (LoopInfinito(numIstruzioni, loopInfinito))
                    throw new CpuLoopException();
            }
        }

        public void StepOver()
        {
            if (stop) return;
            // Se l'istruzione corrente non è una call, fa semplicemente StepInto
            if (Code[ip].Code != "call")
            {
                StepInto();
                return;
            }
            short S = sp;
            Stato = StatoCpu.Attiva;
            try
            {
                Fetch();
                Execute(); // esegue la call: sp diminuisce, ip salta alla subroutine
                if (ip == Code.Count) { Stop(); return; }
                RunWhileInside(S); // continua finché sp < S (cioè siamo dentro la subroutine)
            }
            catch (CpuException)
            {
                Stop();
                throw;
            }
        }

        // Esegue finché sp <= S (cioè siamo ancora dentro o più in profondità):
        // RunWhileInside(S+1) → loop while sp < S+1 → while sp <= S
        public void StepOut()
        {
            if (stop) return;
            short S = sp;
            Stato = StatoCpu.Attiva;
            try
            {
                RunWhileInside((short)(S + 1));
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

        void Execute()
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
            int tmp = ax * LoadOp(1);
            ax = (short)tmp;
            dx = (short)(tmp >> 16);
            SetFlags(OF + ZF, tmp);
        }

        void IDiv()
        {
            int dividendo = ax + (dx << 16);
            dx = (short)(dividendo % LoadOp(1));
            ax = (short)(dividendo / LoadOp(1));
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

        short NuovoIp()
        {
            return (short)(LoadOp(1) - 1);
        }

        #endregion

        #region load/store/flags

        short LoadOp(int numOp)
        {
            if (numOp == 1)
                return LoadOp(curIstruzione.Op1, curIstruzione.Offset1);
            else
                return LoadOp(curIstruzione.Op2, curIstruzione.Offset2);
        }

        void StoreOp(int valore, int numOp)
        {
            if (numOp == 1)
                StoreOp((short)valore, curIstruzione.Op1, curIstruzione.Offset1);
            else
                StoreOp((short)valore, curIstruzione.Op2, curIstruzione.Offset2);
        }

        short LoadOp(IdOp op, int offset)
        {
            switch (op)
            {
                case IdOp.ax: return ax;
                case IdOp.bx: return bx;
                case IdOp.cx: return cx;
                case IdOp.dx: return dx;
                case IdOp.si: return si;
                case IdOp.di: return di;
                case IdOp.bp: return bp;
                case IdOp.sp: return sp;
                case IdOp._si: return memoria[si + offset];
                case IdOp._di: return memoria[di + offset];
                case IdOp._bx: return memoria[bx + offset];
                case IdOp._bp: return memoria[bp + offset];
                case IdOp.Costante: return (short)offset;
                case IdOp.Memoria: return memoria[offset];
                case IdOp.Etichetta: return (short)offset;
            }
            return -1;
        }

        void StoreOp(short valore, IdOp op, int offset)
        {
            switch (op)
            {
                case IdOp.ax: ax = valore; break;
                case IdOp.bx: bx = valore; break;
                case IdOp.cx: cx = valore; break;
                case IdOp.dx: dx = valore; break;
                case IdOp.si: si = valore; break;
                case IdOp.di: di = valore; break;
                case IdOp.bp: bp = valore; break;
                case IdOp.sp: sp = valore; break;
                case IdOp._si: memoria[si + offset] = valore; break;
                case IdOp._di: memoria[di + offset] = valore; break;
                case IdOp._bx: memoria[bx + offset] = valore; break;
                case IdOp._bp: memoria[bp + offset] = valore; break;
                case IdOp.Memoria: memoria[offset] = valore; break;
            }
        }

        void SetFlags(short mask, int ris)
        {
            if ((ZF & mask) != 0)
                flags = (short)(((short)ris == 0) ? flags | ZF : flags & ~ZF);

            if ((SF & mask) != 0)
                flags = (short)(((short)ris < 0) ? flags | SF : flags & ~SF);

            if ((OF & mask) != 0)
                flags = (short)((ris > short.MaxValue || ris < short.MinValue) ? flags | OF : flags & ~OF);
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

            reg[0] = string.Format("AX" + formatoReg, ax);
            reg[1] = string.Format("BX" + formatoReg, bx);
            reg[2] = string.Format("CX" + formatoReg, cx);
            reg[3] = string.Format("DX" + formatoReg, dx);
            reg[4] = string.Format("SI" + formatoReg, si);
            reg[5] = string.Format("DI" + formatoReg, di);
            reg[6] = string.Format("BP" + formatoReg, bp);
            reg[7] = string.Format("SP" + formatoReg, sp);
            reg[8] = string.Format("IP" + formatoReg, ip);
            return reg;
        }

        static string IntToChar(int x)
        {
            if (x == 0)
                return Ambiente.FormatoCarZero;
            if (x < 0)
                x = 128;
            return Convert.ToChar(x).ToString();
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
