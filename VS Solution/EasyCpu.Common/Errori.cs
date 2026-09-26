using System;

namespace EasyCpu.Common
{
	public enum CodiceErrore
	{
		AttesaCostante,
		AttesaVirgola,
		AttesoRegistroIndiretto,
		AttesoDuePunti,
		AttesaQuadraChiusura,
		AttesoCodiceIstruzione,
		IndirizzoDatiNonValido,
		NumeroOperandi,
		CostanteFuoriIntervallo,
		DestinazioneCostante,
		Sintassi,
		CarattereNonValido,
		IntervalloIndirizzoDati,
		EtichettaNonValida,
		OperandoNonValido,
		FormatoIndirizzoDati,
		Formato,
		FormatoValoreDati,
		LoopInfinito,
		Sconosciuto,
		StackOverflow,
		StackUnderflow,
		ViolazioneMemoria,
		IPNonValido,
		InterruptNonValido,
		DimensioneOperandi,
		DivisionePerZero,
		QuozienteFuoriIntervallo,
		SimboloDuplicato,
		SimboloNonDefinito,
		NomeSimboloNonValido,
		DatiInAreaStack,
		ServizioNonValido,
		CombinazioneRegistriNonValida,
		PrefissoNonValido,

	}

	public class Errori
	{
		public static string Msg(CodiceErrore err)
		{
			switch (err)
			{
				case CodiceErrore.IndirizzoDatiNonValido: return "Indirizzo di memoria non valido";
				case CodiceErrore.AttesoCodiceIstruzione: return "E' atteso codice mnemonico o codice mnemonico non valido";
				case CodiceErrore.AttesoDuePunti: return "E' atteso il carattere ':'";
				case CodiceErrore.AttesaQuadraChiusura: return "E' atteso il carattere ']'";
				case CodiceErrore.AttesoRegistroIndiretto: return "E' atteso uno tra i registri: BX, BP, SI, DI";
				case CodiceErrore.AttesaVirgola: return "E' atteso il carattere ','";
				case CodiceErrore.AttesaCostante: return "E' atteso un numero";
				case CodiceErrore.CostanteFuoriIntervallo: return "Valore fuori dall'intervallo consentito";
				case CodiceErrore.NumeroOperandi: return "Numero operandi errato";
				case CodiceErrore.DestinazioneCostante: return "L'operando destinazione non può essere una costante";
				case CodiceErrore.Sconosciuto: return "Errore sconosciuto";
				case CodiceErrore.StackOverflow: return "Stack overflow";
				case CodiceErrore.StackUnderflow: return "Stack underflow";
				case CodiceErrore.Formato: return "Formato numerico non valido";
				case CodiceErrore.OperandoNonValido: return "Operando sconosciuto o non valido";
				case CodiceErrore.EtichettaNonValida: return "Etichetta sconosciuta o non valida";
				case CodiceErrore.IntervalloIndirizzoDati: return "Indirizzo dati fuori dall'intervallo";
				case CodiceErrore.Sintassi: return "Errore sintattico";
				case CodiceErrore.CarattereNonValido: return "Carattere sconosciuto o non valido";
				case CodiceErrore.LoopInfinito: return "Il programma si trova in un 'ciclo infinito'";
				case CodiceErrore.ViolazioneMemoria: return "Violazione dei limiti della memoria";
				case CodiceErrore.IPNonValido: return "Registro IP non indirizza un'istruzione";
				case CodiceErrore.InterruptNonValido: return "Numero di interrupt non valido";
				case CodiceErrore.DimensioneOperandi: return "Dimensione degli operandi non valida o non coerente (8/16 bit)";
				case CodiceErrore.DivisionePerZero: return "Divisione per zero";
				case CodiceErrore.QuozienteFuoriIntervallo: return "Il quoziente della divisione non sta nel registro destinazione";
				case CodiceErrore.SimboloDuplicato: return "Nome già definito";
				case CodiceErrore.SimboloNonDefinito: return "Nome di variabile o costante non definito";
				case CodiceErrore.NomeSimboloNonValido: return "Nome non valido: non può essere un registro, un'istruzione o una parola riservata";
				case CodiceErrore.DatiInAreaStack: return "I dati superano l'area riservata allo stack (indirizzi 240..255)";
				case CodiceErrore.ServizioNonValido: return "Servizio int 21h non valido (valore di AH)";
				case CodiceErrore.CombinazioneRegistriNonValida: return "Combinazione di registri non valida: si può sommare BX o BP con SI o DI";
				case CodiceErrore.PrefissoNonValido: return "Prefisso non valido: REP si usa con le istruzioni stringa, REPE e REPNE con CMPS e SCAS";
					//case CodiceErrore.: return "";
					//case CodiceErrore.: return "";
			}
			return "";
		}

	}
}