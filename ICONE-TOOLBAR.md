# Icone toolbar e menu — documentazione

Data: 2026-07-03

## Obiettivo

Sostituire i pulsanti testuali della seconda riga della toolbar (in `MainView.axaml`) con pulsanti a icona, e affiancare la stessa icona alle voci corrispondenti nei menu a tendina (File, Modifica, Esegui, Finestre, Strumenti). Il tutto deve funzionare correttamente sia con il tema Chiaro sia con i temi Scuro/Blue già presenti nell'app.

## Scelte progettuali

### 1. Set di icone: Fluent System Icons (Microsoft, licenza MIT)

Valutati inizialmente tre set: Fluent System Icons, Tabler Icons, Codicons (VS Code).

Scelto **Fluent System Icons** perché:
- è lo stesso linguaggio visivo del tema `Avalonia.Themes.Fluent` già usato dal progetto (coerenza stilistica con controlli nativi Avalonia);
- copertura molto ampia (oltre 20.000 varianti tra stili/dimensioni), sufficiente a trovare un'icona sensata per ogni comando, incluse le azioni di debug (step into/over/out);
- licenza MIT, nessuna limitazione di utilizzo o attribuzione obbligatoria.

Distribuito ufficialmente come pacchetto npm `@fluentui/svg-icons`, scaricato dal registry npm (`registry.npmjs.org`, unico host per asset raggiungibile dalla sandbox insieme a `github.com`; `raw.githubusercontent.com`, `unpkg.com`, `cdnjs.cloudflare.com` risultavano bloccati dall'allowlist di rete). Il pacchetto contiene tutte le SVG sorgente in stile "regular" e "filled" a più dimensioni (16/20/24/28 px).

### 2. Formato file: PNG monocromatico invece di SVG

Il progetto Avalonia (v12.0.5) **non ha alcun pacchetto per il rendering SVG** (mancava `Avalonia.Svg.Skia` o simili — l'unico file `.svg` presente, `images/jetbrains.svg`, è usato solo nel badge del README, non nell'app). Aggiungere quella dipendenza avrebbe introdotto un pacchetto NuGet in più solo per mostrare delle icone statiche.

Si è quindi scelto di:
1. Rasterizzare le SVG sorgente in **PNG a 64×64 px con canale alpha**, colore pieno nero (`convert -background none -density 480 <src>.svg -resize 64x64 <out>.png`, ImageMagick con delegate SVG interno).
2. Usare il PNG **solo come maschera di trasparenza** (non come immagine a colori) — il colore effettivo dell'icona viene applicato a runtime in XAML (vedi punto successivo). Questo evita di dover generare e mantenere due set di icone (uno per tema chiaro, uno per scuro): un solo file per icona funziona su tutti e tre i temi.

La risoluzione 64×64 è volutamente più alta della dimensione di visualizzazione (16–20 px) per restare nitida anche su schermi ad alta densità (hi-DPI).

### 3. Adattamento automatico al tema: componente `ThemedIcon`

Creato un piccolo `UserControl` riutilizzabile: `Controls/ThemedIcon.axaml` + `ThemedIcon.axaml.cs`.

Meccanismo (pattern classico Avalonia/WPF per "tingere" un'immagine monocromatica):

```xml
<Border Background="{DynamicResource SystemControlForegroundBaseHighBrush}">
  <Border.OpacityMask>
    <ImageBrush Source="{Binding #Root.Source}" Stretch="Uniform"/>
  </Border.OpacityMask>
</Border>
```

- Il PNG (alpha-only) viene usato come `OpacityMask`: la sua trasparenza "ritaglia" la forma dell'icona.
- Il colore effettivo è il `Background` del `Border`, legato alla risorsa dinamica `SystemControlForegroundBaseHighBrush` — la stessa già usata altrove nel progetto (es. bordo della status bar) e che Avalonia rivaluta automaticamente ad ogni cambio di `RequestedThemeVariant`.
- Risultato: **un solo asset per icona**, colore sempre coerente col tema attivo (scuro su sfondo chiaro, chiaro su sfondo scuro/blue), senza codice C# di gestione tema per le icone.

Espone una proprietà `Source` (`IImage`) impostabile in XAML con la stessa sintassi di `<Image Source="...">`.

### 4. Struttura file e integrazione nel progetto

- Icone salvate in `images/toolbar/` (cartella radice del repo, accanto al `jetbrains.svg` già esistente), come richiesto.
- Il progetto Avalonia (`EasyCPU.csproj`) le include come `AvaloniaResource` puntando fuori dalla cartella di progetto:
  ```xml
  <AvaloniaResource Include="..\..\images\toolbar\**" Link="images\toolbar\%(Filename)%(Extension)" />
  ```
  così sono referenziabili in XAML come `/images/toolbar/<nome>.png`, mantenendo un'unica fonte per tutte le piattaforme (Desktop/Browser/Android/iOS condividono lo stesso progetto core).

### 5. Toolbar vs Menu: due usi diversi dello stesso asset

- **Toolbar** (seconda riga di `MainView.axaml`): i pulsanti sono diventati icona-only (20×20 px dentro bottoni 40×40), con `ToolTip.Tip` che riporta l'etichetta originale (es. "Avvia", "Passo (Step Over)"). Scelta coerente con la richiesta di "sostituire" i pulsanti testuali, in linea con la convenzione standard delle toolbar IDE (icona compatta + tooltip esplicativo).
- **Menu a tendina**: icona (16×16 px, default del componente) assegnata a `MenuItem.Icon`, mantenendo invariato il testo (`Header`) già presente — qui l'icona è un rinforzo visivo accanto all'etichetta, non una sostituzione.

## Mappatura comando → icona

| Comando | File icona | Icona Fluent sorgente |
|---|---|---|
| Nuovo | `new.png` | `document_add_20_regular` |
| Apri | `open.png` | `folder_open_20_regular` |
| Salva | `save.png` | `save_20_regular` |
| Salva come | `save_as.png` | `save_arrow_right_20_regular` |
| Stampa | `print.png` | `print_20_regular` |
| Annulla | `undo.png` | `arrow_undo_20_regular` |
| Ripristina | `redo.png` | `arrow_redo_20_regular` |
| Taglia | `cut.png` | `cut_20_regular` |
| Copia | `copy.png` | `copy_20_regular` |
| Incolla | `paste.png` | `clipboard_paste_20_regular` |
| Seleziona tutto | `select_all.png` | `select_all_on_20_regular` |
| Trova | `find.png` | `search_20_regular` |
| Compila | `compile.png` | `cube_20_regular` |
| Avvia | `run.png` | `play_20_regular` |
| Avvia fino a | `run_until.png` | `play_circle_20_regular` |
| Esegui istruzione (Step Into) | `step_into.png` | `arrow_step_in_20_regular` |
| Passo (Step Over) | `step_over.png` | `arrow_step_over_20_regular` |
| Passo uscita (Step Out) | `step_out.png` | `arrow_step_out_20_regular` |
| Ferma | `stop.png` | `stop_20_regular` |
| Imposta/Rimuovi breakpoint | `breakpoint.png` | `circle_20_filled` |
| Editor codice (pannello) | `panel_code.png` | `code_20_regular` |
| Editor dati (pannello) | `panel_data.png` | `document_table_20_regular` |
| Registri (pannello) | `panel_registers.png` | `list_20_regular` |
| Stack (pannello) | `panel_stack.png` | `stack_20_regular` |
| Memoria (pannello) | `panel_memory.png` | `grid_20_regular` |
| Errori (pannello) | `panel_errors.png` | `error_circle_20_regular` |
| Ripristina layout | `reset_layout.png` | `arrow_reset_20_regular` |
| Opzioni | `options.png` | `settings_20_regular` |
| Tema Chiaro | `theme_light.png` | `weather_sunny_20_regular` |
| Tema Scuro | `theme_dark.png` | `weather_moon_20_regular` |
| Tema Blue (VS Code) | `theme_blue.png` | `paint_brush_20_regular` |

Note sulla mappatura:
- Fluent System Icons non ha icone dedicate al debugging (step into/over/out, breakpoint) come invece i Codicons di VS Code; sono state scelte le icone geometriche più vicine per significato (frecce a gradino per gli step, cerchio pieno per il breakpoint).
- "Compila" usa un cubo (`cube`, metafora di pacchetto/build) — icona aggiornata il 2026-07-04 per sostituire la precedente `wrench_20_regular`, ambigua con un'azione di manutenzione/opzioni. Il cubo è distinto da "Editor codice" (`code`) e da "Opzioni" (`settings`).
- "Blue (VS Code)" usa un pennello (`paint_brush`) come icona generica di "tema personalizzato", non essendoci un'icona Fluent specifica per quel tema.

## File modificati/creati

- `images/toolbar/*.png` — 31 icone (nuove)
- `VS Solution/EasyCPU/Controls/ThemedIcon.axaml` — nuovo componente
- `VS Solution/EasyCPU/Controls/ThemedIcon.axaml.cs` — code-behind
- `VS Solution/EasyCPU/EasyCPU.csproj` — nuova riga `AvaloniaResource` per `images/toolbar`
- `VS Solution/EasyCPU/Views/MainView.axaml` — toolbar riga 2 convertita a icon-only; `MenuItem.Icon` aggiunto a tutte le voci dei 5 menu

## Limiti e punti da verificare manualmente

- **Build non eseguita**: nella sandbox di lavoro non è disponibile l'SDK .NET (il progetto target `net10.0`), quindi non è stato possibile lanciare una `dotnet build` reale. È stata verificata la correttezza XML di tutti i file toccati e la corrispondenza 1:1 tra icone referenziate in XAML e file effettivamente presenti in `images/toolbar/`. Si consiglia una build locale (Visual Studio/Rider) come verifica finale.
- **MenuItem con `ToggleType="CheckBox"`/`"Radio"`**: alle voci di "Finestre" (toggle pannelli) e "Strumenti" (temi) è stata comunque assegnata un'icona: da verificare visivamente se il template Fluent di Avalonia, per questi item, mostra l'icona assieme al segno di spunta/pallino di selezione o se quest'ultimo la sovrascrive nello slot grafico.
- Le icone sono renderizzate a 64×64 px partendo da SVG vettoriali: se in futuro servissero dimensioni molto più grandi (es. icone per una toolbar "grande" o per un launcher), conviene rigenerarle dalla sorgente SVG invece di scalare i PNG attuali.
