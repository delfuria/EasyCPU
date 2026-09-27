namespace EasyCpu.Assembler.Memoria
{
    // Limiti di un modello di memoria, usati da CPU, compilatore e IDE
    public sealed class ModelloMemoria
    {
        // modalità a parole: 256 celle da 16 bit, stack nelle celle 240..255
        public static readonly ModelloMemoria Parole = new(256, 240, 1);

        public int Dimensione { get; }      // numero di unità di memoria (celle o byte)
        public int InizioStack { get; }     // primo indirizzo riservato allo stack
        public int PassoParola { get; }     // unità occupate da una parola: stack, DW, istruzioni stringa W

        ModelloMemoria(int dimensione, int inizioStack, int passoParola)
        {
            Dimensione = dimensione;
            InizioStack = inizioStack;
            PassoParola = passoParola;
        }
    }
}
