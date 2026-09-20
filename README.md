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
- modifica live degli input X1/X2

## Progetti

- `NeuralPad.Core`: motore indipendente dalla UI
- `NeuralPad.App`: shell DevExpress WPF e visualizzazione

## DevExpress

Il progetto usa PackageReference floating `26.1.*` per:

- DevExpress.Wpf.Core
- DevExpress.Wpf.Docking
- DevExpress.Wpf.PropertyGrid
- DevExpress.Wpf.Editors

Configurare il feed NuGet DevExpress associato alla propria licenza prima del restore.

## Esecuzione

Aprire `NeuralPad.slnx` con Visual Studio e avviare `NeuralPad.App`.

## Prossime milestone

1. Watch window.
2. Backpropagation e gradient debugger.
3. Loss e training timeline con DevExpress ChartControl.
4. Piccolo editor C# / DSL in stile NeuralPad script.
