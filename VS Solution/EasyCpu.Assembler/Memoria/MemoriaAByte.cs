using EasyCpu.Common;
using EasyCpu.Assembler.Processore;

namespace EasyCpu.Assembler.Memoria
{
    // Modalità x86 fedele: memoria a byte; una parola occupa due byte consecutivi, prima il basso (little-endian)
    public sealed class MemoriaAByte : MemoriaCpu
    {
        readonly byte[] memoria = new byte[ModelloMemoria.Byte.Dimensione];

        public MemoriaAByte() : base(ModelloMemoria.Byte) { }

        void VerificaIntervallo(int indirizzo, int byteAccesso)
        {
            if (indirizzo < 0 || indirizzo + byteAccesso > memoria.Length)
                throw new CpuException(CodiceErrore.ViolazioneMemoria);
        }

        public override short LeggiParola(int indirizzo)
        {
            VerificaIntervallo(indirizzo, 2);
            return (short)(memoria[indirizzo] | memoria[indirizzo + 1] << 8);
        }

        public override void ScriviParola(int indirizzo, short valore)
        {
            VerificaIntervallo(indirizzo, 2);
            memoria[indirizzo] = (byte)valore;
            memoria[indirizzo + 1] = (byte)(valore >> 8);
        }

        public override short LeggiByte(int indirizzo)
        {
            VerificaIntervallo(indirizzo, 1);
            return (sbyte)memoria[indirizzo];
        }

        public override void ScriviByte(int indirizzo, short valore)
        {
            VerificaIntervallo(indirizzo, 1);
            memoria[indirizzo] = (byte)valore;
        }

        public override void Imposta(IReadOnlyList<int> contenuto)
        {
            if (contenuto == null) return;
            int len = Math.Min(contenuto.Count, memoria.Length);
            for (int i = 0; i < len; i++)
                memoria[i] = (byte)contenuto[i];
        }
    }
}
