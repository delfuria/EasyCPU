
using EasyCpu.Common;
using EasyCpu.Assembler.Processore;    

namespace EasyCpu.Assembler.Memoria
{
    public class Ram
    {
        public static readonly int MASSIMO_INDIRIZZO = 255;
        public static readonly int INDIRIZZO_STACK = 240;

        int[] memoria;
        public Ram()
        {

            this.memoria = new int[MASSIMO_INDIRIZZO + 1];
        }

        void VerificaIntervalloIndice(int indice)
        {
            if (indice < 0 || indice > MASSIMO_INDIRIZZO)
                throw new CpuException(CodiceErrore.ViolazioneMemoria);
        }

        public short this[int indice]
        {
            get
            {
                VerificaIntervalloIndice(indice);
                return (short)memoria[indice];
            }
            set
            {
                VerificaIntervalloIndice(indice);
                memoria[indice] = value;
            }
        }
        public void Imposta(List<int> memoria)
        {
            if (memoria == null) return;
            int len = (memoria.Count < MASSIMO_INDIRIZZO + 1) ? memoria.Count : MASSIMO_INDIRIZZO + 1;
            for (int i = 0; i < len; i++)
            {
                this.memoria[i] = memoria[i];
            }
            // Array.Copy(memoria, 0, this.memoria, 0, len);
        }
    }
}
