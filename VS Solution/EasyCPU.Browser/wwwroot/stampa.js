// Stampa di EasyCPU nel browser (Stampa.cs): la pagina generata viene caricata in un iframe
// nascosto e stampata da lì, senza aprire altre schede né lasciare l'app.
let cornice = null;

export function stampa(html) {
    cornice?.remove();
    cornice = globalThis.document.createElement("iframe");
    // non display:none né visibility:hidden: alcuni browser stamperebbero una pagina vuota
    cornice.style.cssText = "position:fixed; right:0; bottom:0; width:0; height:0; border:0;";
    cornice.onload = () => {
        cornice.contentWindow.focus();
        cornice.contentWindow.print();
    };
    cornice.srcdoc = html;      // prima dell'inserimento: un solo evento load
    globalThis.document.body.appendChild(cornice);
}
