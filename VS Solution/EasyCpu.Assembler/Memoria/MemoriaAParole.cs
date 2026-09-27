using EasyCpu.Common;
using EasyCpu.Assembler.Processore;

namespace EasyCpu.Assembler.Memoria
{
    // Modalità a parole (predefinita): 256 celle da 16 bit; un accesso a 8 bit usa il byte basso della cella
    public sealed class MemoriaAParole : MemoriaCpu
    {
        readonly int[] memoria = new int[ModelloMemoria.Parole.Dimensione];

        public MemoriaAParole() : base(ModelloMemoria.Parole) { }

        void VerificaIntervalloIndice(int indice)
        {
            if (indice < 0 || indice >= Modello.Dimensione)
                throw new CpuException(CodiceErrore.ViolazioneMemoria);
        }

        public override short LeggiParola(int indirizzo)
        {
            VerificaIntervalloIndice(indirizzo);
            return (short)memoria[indirizzo];
        }

        public override void ScriviParola(int indirizzo, short valore)
        {
            VerificaIntervalloIndice(indirizzo);
            memoria[indirizzo] = valore;
        }

        public override short LeggiByte(int indirizzo) => (sbyte)LeggiParola(indirizzo);

        // sostituisce il byte basso della cella, il byte alto resta invariato
        public override void ScriviByte(int indirizzo, short valore) =>
            ScriviParola(indirizzo, (short)((LeggiParola(indirizzo) & 0xFF00) | (valore & 0xFF)));

        public override void Imposta(IReadOnlyList<int> contenuto)
        {
            if (contenuto == null) return;
            int len = Math.Min(contenuto.Count, memoria.Length);
            for (int i = 0; i < len; i++)
                memoria[i] = contenuto[i];
        }
    }
}
