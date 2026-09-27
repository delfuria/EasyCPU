using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace EasyCpu.DocGen;

/// <summary>
/// Converte l'Assembly Reference da Markdown a un'unica pagina HTML autonoma (CSS incluso),
/// con indice laterale e un'ancora per ogni istruzione e sinonimo (#mov, #jz...):
/// l'IDE apre la pagina su quelle ancore quando si preme F1 su un'istruzione.
/// </summary>
public static partial class Generatore
{
    // "### LOOP, LOOPE, LOOPNE – Cicli con contatore"
    [GeneratedRegex(@"^### ([A-Z]+(?:, [A-Z]+)*) – ")]
    private static partial Regex TitoloIstruzione();

    // "Sinonimi: JNAE, JC." / "LOOPE (sinonimo LOOPZ)"
    [GeneratedRegex(@"Sinonim[oi]: ([A-Z]+(?:, [A-Z]+)*)|\(sinonimo ([A-Z]+)\)")]
    private static partial Regex Sinonimi();

    // Le figure del documento originale sono EMF troncati dalla conversione da .docx: non visualizzabili.
    [GeneratedRegex(@"!\[[^\]]*\]\(data:image/x-emf[^)]*\)")]
    private static partial Regex ImmagineEmf();

    [GeneratedRegex(@"[A-Z]+")]
    private static partial Regex Parola();

    public static string Genera(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var documento = Markdown.Parse(Prepara(markdown), pipeline);
        var corpo = documento.ToHtml(pipeline);
        return Pagina.Replace("{{INDICE}}", Indice(documento)).Replace("{{CORPO}}", corpo);
    }

    /// <summary>Tutte le ancore delle istruzioni (minuscole), sinonimi compresi.</summary>
    public static IReadOnlyCollection<string> Ancore(string markdown)
    {
        var ancore = new HashSet<string>();
        foreach (var (principali, sinonimi) in Istruzioni(markdown.Split('\n')).Values)
        {
            ancore.UnionWith(principali);
            ancore.UnionWith(sinonimi);
        }
        return ancore;
    }

    // Aggiunge le ancore alle istruzioni, toglie le immagini rotte e l'indice scritto a mano
    // (sostituito da quello generato con i link).
    private static string Prepara(string markdown)
    {
        var righe = markdown.Replace("\r\n", "\n").Split('\n');
        var istruzioni = Istruzioni(righe);
        var sb = new StringBuilder();
        bool inIndice = false;

        for (int i = 0; i < righe.Length; i++)
        {
            var riga = righe[i];
            if (riga == "Indice") { inIndice = true; continue; }
            if (inIndice && !riga.StartsWith("## ")) continue;
            inIndice = false;

            if (istruzioni.TryGetValue(i, out var voce))
            {
                // Le ancore degli altri nomi stanno subito prima del titolo, così il salto lo mostra.
                var altre = voce.Principali.Skip(1).Concat(voce.Sinonimi).Distinct().ToList();
                if (altre.Count > 0) sb.Append('\n');
                foreach (var a in altre)
                    sb.Append("<div id=\"").Append(a).Append("\"></div>\n");
                if (altre.Count > 0) sb.Append('\n');
                sb.Append(riga).Append(" {#").Append(voce.Principali[0]).Append("}\n");
                continue;
            }

            sb.Append(ImmagineEmf().Replace(riga, "")).Append('\n');
        }
        return sb.ToString();
    }

    // Per ogni riga di titolo di un'istruzione: mnemonici del titolo e sinonimi citati nella sezione.
    private static Dictionary<int, (List<string> Principali, List<string> Sinonimi)> Istruzioni(string[] righe)
    {
        var risultato = new Dictionary<int, (List<string>, List<string>)>();
        List<string>? sinonimiCorrenti = null;

        for (int i = 0; i < righe.Length; i++)
        {
            var riga = righe[i].TrimEnd('\r');
            if (riga.StartsWith('#')) sinonimiCorrenti = null;

            var titolo = TitoloIstruzione().Match(riga);
            if (titolo.Success)
            {
                var principali = Parola().Matches(titolo.Groups[1].Value).Select(m => m.Value.ToLowerInvariant()).ToList();
                sinonimiCorrenti = [];
                risultato[i] = (principali, sinonimiCorrenti);
                continue;
            }

            if (sinonimiCorrenti is null) continue;
            foreach (Match m in Sinonimi().Matches(riga))
            {
                var elenco = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                sinonimiCorrenti.AddRange(Parola().Matches(elenco).Select(p => p.Value.ToLowerInvariant()));
            }
        }
        return risultato;
    }

    private static string Indice(MarkdownDocument documento)
    {
        var sb = new StringBuilder("<ul>\n");
        bool voceAperta = false, sottoAperto = false;
        foreach (var titolo in documento.Descendants<HeadingBlock>().Where(h => h.Level is 2 or 3))
        {
            var link = $"<a href=\"#{titolo.GetAttributes().Id}\">{WebUtility.HtmlEncode(Testo(titolo))}</a>";
            if (titolo.Level == 2)
            {
                if (sottoAperto) { sb.Append("</ul>"); sottoAperto = false; }
                if (voceAperta) sb.Append("</li>\n");
                sb.Append("<li>").Append(link);
                voceAperta = true;
            }
            else
            {
                if (!sottoAperto) { sb.Append("\n<ul>\n"); sottoAperto = true; }
                sb.Append("<li>").Append(link).Append("</li>\n");
            }
        }
        if (sottoAperto) sb.Append("</ul>");
        if (voceAperta) sb.Append("</li>\n");
        return sb.Append("</ul>").ToString();
    }

    private static string Testo(HeadingBlock titolo)
    {
        var sb = new StringBuilder();
        foreach (var l in titolo.Inline?.Descendants<LiteralInline>() ?? [])
            sb.Append(l.Content.ToString());
        return sb.ToString().Trim();
    }

    private const string Pagina = """
<!doctype html>
<html lang="it">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>EasyCPU – Assembly Reference</title>
<style>
:root { --bg:#ffffff; --fg:#1f2328; --muted:#59636e; --bordo:#d1d9e0; --nav:#f6f8fa; --link:#0969da; --codice:#eff1f3; }
@media (prefers-color-scheme: dark) {
  :root { --bg:#0d1117; --fg:#e6edf3; --muted:#9198a1; --bordo:#3d444d; --nav:#151b23; --link:#4493f8; --codice:#262c36; }
}
* { box-sizing: border-box; }
html { scroll-padding-top: 16px; }
body { margin:0; background:var(--bg); color:var(--fg); font:16px/1.6 -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; }
a { color:var(--link); text-decoration:none; }
a:hover { text-decoration:underline; }
nav { background:var(--nav); border-bottom:1px solid var(--bordo); padding:12px 16px; font-size:14px; }
nav summary { cursor:pointer; font-weight:600; }
nav ul { list-style:none; margin:0; padding-left:0; }
nav ul ul { padding-left:14px; }
nav li { margin:2px 0; }
main { max-width:860px; padding:16px; margin:0 auto; }
h2 { border-bottom:1px solid var(--bordo); padding-bottom:6px; margin-top:40px; }
h3 { margin-top:32px; }
table { border-collapse:collapse; margin:12px 0; display:block; overflow-x:auto; }
th, td { border:1px solid var(--bordo); padding:4px 10px; text-align:left; }
code, pre { background:var(--codice); border-radius:4px; font-family:ui-monospace, Menlo, Consolas, monospace; font-size:0.9em; }
code { padding:1px 4px; }
pre { padding:10px; overflow-x:auto; }
:target { background:color-mix(in srgb, var(--link) 15%, transparent); }
@media (min-width: 1000px) {
  nav { position:fixed; top:0; bottom:0; left:0; width:300px; overflow-y:auto; border-bottom:none; border-right:1px solid var(--bordo); }
  main { margin-left:300px; padding:16px 40px; }
}
</style>
</head>
<body>
<nav><details><summary>Indice</summary>
{{INDICE}}
</details></nav>
<script>if (matchMedia('(min-width: 1000px)').matches) document.querySelector('nav details').open = true;</script>
<main>
{{CORPO}}
</main>
<script>
// Aperta in una scheda in background la pagina viene impaginata con un'altra larghezza:
// quando diventa visibile il salto all'ancora va ripetuto.
function vaiAncora() { const t = location.hash && document.getElementById(decodeURIComponent(location.hash.slice(1))); if (t) t.scrollIntoView(); }
addEventListener('load', vaiAncora);
document.addEventListener('visibilitychange', function f() { if (!document.hidden) { vaiAncora(); document.removeEventListener('visibilitychange', f); } });
</script>
</body>
</html>
""";
}
