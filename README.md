# NeuralPad

NeuralPad is an experimental visual debugger and learning environment for neural networks, inspired by the interactive workflow of LINQPad.

The goal is to make the mathematics and runtime state of a small neural network directly inspectable:

**C# → mathematics → graph**

Instead of treating a network as a black box, NeuralPad exposes forward propagation, backpropagation, gradients, optimizer updates, weights, biases and intermediate activations step by step.

> NeuralPad is currently an experimental/educational project, not a production machine-learning framework.

## Current features

- .NET 10 / WPF
- UI built with DevExpress WPF
- UI-independent neural-network core
- dynamically configurable dense networks
- ReLU, Sigmoid, Tanh and Linear activations
- real incremental forward execution
- real incremental backward propagation
- Binary Cross Entropy for Sigmoid outputs
- multiple inputs and multiple outputs
- visual graph of neurons and weighted connections
- connection contribution inspection
- neuron inspection for bias, weighted sum, activation and gradients
- editable weights and biases
- Watch window, for example:
  - `H1.Activation`
  - `O1.Z`
  - `H1.Bias`
  - `W(H1,O1)`
- training datasets with arbitrary input/output vectors
- Train Sample / Train Epoch
- loss history
- training breakpoints
- parameter snapshots and comparisons
- separate Forward / Backward / Optimizer phases
- C# scripting through Roslyn
- AvalonEdit-based C# editor
- syntax highlighting
- first IntelliSense/completion support
- live Roslyn diagnostics

## Architecture

The solution is intentionally split between the computation engine and the UI:

- `NeuralPad.Core` — deterministic neural-network engine with no WPF dependency.
- `NeuralPad.App` — WPF debugger, visualization, scripting and training UI.

A core design principle is that a debugger step corresponds to a real mathematical operation rather than to an animation.

For example, a forward pass is decomposed into operations such as:

```text
input
→ weighted contribution
→ weighted sum
→ bias
→ activation
```

Backpropagation is similarly exposed as an incremental process.

## C# scripting

NeuralPad can compile and execute C# scripts through Roslyn.

Example:

```csharp
var hidden = 3;

var net = Neural.Network(2)
    .Dense(hidden, ReLU)
    .Dense(1, Sigmoid)
    .Input(0.80, 0.35)
    .Target(1)
    .LearningRate(0.10);

for (var i = 0; i < 4; i++)
{
    var x1 = i >= 2 ? 1.0 : 0.0;
    var x2 = i % 2 == 1 ? 1.0 : 0.0;

    net.Sample(
        [x1, x2],
        [(x1 == x2) ? 0.0 : 1.0]);
}

Dump(net);
```

`Dump(net)` sends the scripted network to the NeuralPad debugger, in a workflow deliberately inspired by LINQPad.

## Requirements

- Windows
- Visual Studio with .NET 10 support
- .NET 10 SDK
- a valid DevExpress WPF license and access to the DevExpress NuGet feed

Open `NeuralPad.slnx` and run `NeuralPad.App`.

## Dependencies and licensing

NeuralPad's own source code is released under the MIT License.

Third-party dependencies retain their respective licenses:

- **DevExpress WPF 26.1** — commercial/proprietary software. NeuralPad does not include DevExpress binaries, source code, license keys or NuGet-feed credentials. A valid DevExpress license is required to restore and use the DevExpress packages according to the DevExpress EULA.
- **Microsoft.CodeAnalysis.CSharp.Scripting (Roslyn)** — MIT licensed.
- **AvalonEdit** — MIT licensed.

The MIT license of NeuralPad does not relicense DevExpress or any other third-party component.

## Project status

NeuralPad is a work in progress. APIs, scripting syntax, debugger behavior and UI may change while the architecture evolves.

Some planned areas include:

- richer Roslyn IntelliSense and parameter information
- inline diagnostics and error adorners
- Softmax + categorical cross entropy
- additional optimizers
- contribution tree visualization
- improved breakpoint state preservation
- richer datasets and training workflows
- comparison of multiple network runs

## License

MIT. See [LICENSE](LICENSE).
