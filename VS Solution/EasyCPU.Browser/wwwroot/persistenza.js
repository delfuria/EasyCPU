// Archivio delle impostazioni di EasyCPU nel browser (localStorage).
// Tutte le chiavi hanno un prefisso: su GitHub Pages l'origine (utente.github.io)
// è condivisa con gli altri progetti dello stesso utente.
const PREFISSO = "easycpu:";

export function leggi(chiave) {
    try { return globalThis.localStorage.getItem(PREFISSO + chiave); }
    catch { return null; }
}

export function scrivi(chiave, testo) {
    try { globalThis.localStorage.setItem(PREFISSO + chiave, testo); return true; }
    catch { return false; }     // quota esaurita o storage bloccato: l'app continua senza salvare
}

export function elimina(chiave) {
    try { globalThis.localStorage.removeItem(PREFISSO + chiave); }
    catch { }
}

// Nel browser non c'è un evento di uscita dell'applicazione: si salva quando la pagina
// viene nascosta (cambio di scheda, app in background su mobile) o chiusa.
export function registraSalvataggioAllUscita(salva) {
    globalThis.addEventListener("pagehide", () => salva());
    globalThis.document.addEventListener("visibilitychange", () => {
        if (globalThis.document.visibilityState === "hidden") salva();
    });
}
