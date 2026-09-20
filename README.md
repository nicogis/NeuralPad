# NeuralPad

POC di debugger visuale per reti neurali, ispirato alla filosofia di LINQPad.

## Prima milestone

- .NET 10
- DevExpress WPF 26.1
- rete demo 2 -> 3 -> 1
- Dense fully-connected
- ReLU sul layer hidden
- Sigmoid sull'output
- forward pass reale
- trace matematica step-by-step
- `ForwardSession` incrementale: ogni Step esegue realmente una sola operazione
- stato non eseguito distinto dallo zero numerico (`—` nel canvas)
- visualizzazione dinamica di neuroni, pesi e attivazioni
- inspector DevExpress del neurone/collegamento corrente
- selezione diretta di neuroni e connessioni dal grafo
- editing live di Weight e Bias con invalidazione della sessione corrente
- Watch Window live con espressioni come `H1.Activation`, `O1.Z`, `H1.Bias`, `W(H1,O1)`
- modifica live degli input X1/X2
- backward debugger con BCE, delta, gradienti e optimizer step separato
- visualizzazione backward sullo stesso grafo computazionale
- Training Mode XOR: Train Sample, Train Epoch, prediction/loss per campione e storico loss

## Progetti

- `NeuralPad.Core`: motore indipendente dalla UI
- `NeuralPad.App`: shell DevExpress WPF e visualizzazione

## DevExpress

Il progetto usa PackageReference floating `26.1.*` per:

- DevExpress.Wpf.Core
- DevExpress.Wpf.Docking
- DevExpress.Wpf.PropertyGrid
- DevExpress.Wpf.Editors
- DevExpress.Wpf.Grid
- DevExpress.Wpf.Charts

Configurare il feed NuGet DevExpress associato alla propria licenza prima del restore.

## Esecuzione

Aprire `NeuralPad.slnx` con Visual Studio e avviare `NeuralPad.App`.

## Prossime milestone

1. Breakpoint didattici su loss, gradienti e pesi.
2. Snapshot/compare dei parametri tra epoche.
3. Piccolo editor C# / DSL in stile NeuralPad script.
