using System.Collections.Generic;
using System.Text;

namespace EasyCpu.Assembler.Processore;

// Parte di una riga di un pannello (Registri, Memoria, Stack): Cambiato se il valore
// è diverso da quello mostrato al refresh precedente
public readonly record struct Segmento(string Testo, bool Cambiato);

// Confronta le righe di un pannello con quelle mostrate in precedenza (stessa posizione:
// i dump sono a colonne fisse) ed evidenzia per intero ogni valore modificato
public static class DiffPannello
{
    const string Separatori = " []=:";

    static bool Separatore(char c) => Separatori.IndexOf(c) >= 0;

    public static List<List<Segmento>> Confronta(IReadOnlyList<string>? vecchie, IReadOnlyList<string> nuove)
    {
        var risultato = new List<List<Segmento>>(nuove.Count);
        for (int r = 0; r < nuove.Count; r++)
        {
            string? vecchia = vecchie != null && r < vecchie.Count ? vecchie[r] : null;
            risultato.Add(Segmenta(nuove[r], vecchia == null ? null : Cambiati(vecchia, nuove[r])));
        }
        return risultato;
    }

    // Caratteri diversi estesi all'intero valore (sequenza senza separatori). Un separatore
    // cambiato appartiene al valore alla sua destra (numeri allineati a destra che si
    // accorciano), altrimenti a quello a sinistra; se isolato (un carattere ' ' o '='
    // in modalità caratteri) è evidenziato da solo.
    static bool[] Cambiati(string vecchia, string nuova)
    {
        var cambiato = new bool[nuova.Length];
        for (int i = 0; i < nuova.Length; i++)
        {
            if (i < vecchia.Length && vecchia[i] == nuova[i]) continue;
            if (!Separatore(nuova[i]))
                SegnaValore(nuova, cambiato, i);
            else if (i + 1 < nuova.Length && !Separatore(nuova[i + 1]))
                SegnaValore(nuova, cambiato, i + 1);
            else if (i > 0 && !Separatore(nuova[i - 1]))
                SegnaValore(nuova, cambiato, i - 1);
            else
                cambiato[i] = true;
        }
        return cambiato;
    }

    static void SegnaValore(string riga, bool[] cambiato, int i)
    {
        int inizio = i, fine = i;
        while (inizio > 0 && !Separatore(riga[inizio - 1])) inizio--;
        while (fine + 1 < riga.Length && !Separatore(riga[fine + 1])) fine++;
        for (int k = inizio; k <= fine; k++) cambiato[k] = true;
    }

    static List<Segmento> Segmenta(string riga, bool[]? cambiato)
    {
        var segmenti = new List<Segmento>();
        if (cambiato == null)
        {
            segmenti.Add(new Segmento(riga, false));
            return segmenti;
        }
        var testo = new StringBuilder();
        bool stato = false;
        for (int i = 0; i < riga.Length; i++)
        {
            if (cambiato[i] != stato && testo.Length > 0)
            {
                segmenti.Add(new Segmento(testo.ToString(), stato));
                testo.Clear();
            }
            stato = cambiato[i];
            testo.Append(riga[i]);
        }
        if (testo.Length > 0 || segmenti.Count == 0)
            segmenti.Add(new Segmento(testo.ToString(), stato));
        return segmenti;
    }
}
