namespace EasyCpu.Assembler.Memoria
{
    // Memoria dati della CPU. Gli indirizzi sono unità di memoria del modello: celle da 16 bit
    // nella modalità a parole, byte nella modalità x86 fedele.
    public abstract class MemoriaCpu
    {
        public ModelloMemoria Modello { get; }

        protected MemoriaCpu(ModelloMemoria modello) => Modello = modello;

        public abstract short LeggiParola(int indirizzo);
        public abstract void ScriviParola(int indirizzo, short valore);
        public abstract short LeggiByte(int indirizzo);             // con estensione del segno
        public abstract void ScriviByte(int indirizzo, short valore);

        // dati iniziali prodotti dalla compilazione, un valore per unità di memoria
        public abstract void Imposta(IReadOnlyList<int> contenuto);

        public static MemoriaCpu Crea(ModelloMemoria modello) => new MemoriaAParole();
    }
}
