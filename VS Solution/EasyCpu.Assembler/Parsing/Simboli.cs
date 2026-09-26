namespace EasyCpu.Assembler.Parsing
{
    public enum TipoSimbolo
    {
        Equ,    // costante simbolica: non occupa memoria
        Db,     // variabile a byte
        Dw      // variabile a parola
    }

    // Nome definito nella sezione dati
    public class Simbolo
    {
        public string Nome;     // in minuscolo: i nomi non distinguono maiuscole e minuscole
        public string Grafia;   // come scritto nella dichiarazione, per la visualizzazione
        public TipoSimbolo Tipo;
        public int Valore;      // EQU: valore della costante; DB/DW: indirizzo della prima cella
        public int Celle;       // DB/DW: numero di celle allocate

        public Simbolo(string nome, TipoSimbolo tipo, int valore, int celle)
        {
            Nome = nome;
            Tipo = tipo;
            Valore = valore;
            Celle = celle;
        }

        // dimensione degli accessi alla variabile: 8 (DB), 16 (DW), 0 per le costanti
        public int Larghezza => Tipo == TipoSimbolo.Db ? 8 : (Tipo == TipoSimbolo.Dw ? 16 : 0);
    }
}
