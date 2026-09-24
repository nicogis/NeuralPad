using NeuralPad.App.ViewModels;
using NeuralPad.Core;

namespace NeuralPad.App.Scripting;

public sealed class ScriptNetwork
{
    private readonly List<(int Count, ActivationKind Activation)> _layers = [];
    private readonly List<TrainingSample> _samples = [];

    public ScriptNetwork(int inputCount)
    {
        if (inputCount <= 0) throw new ArgumentOutOfRangeException(nameof(inputCount));
        InputCount = inputCount;
        Inputs = new double[inputCount];
    }

    public int InputCount { get; }
    public IReadOnlyList<double> Inputs { get; private set; }
    public IReadOnlyList<double> Targets { get; private set; } = [1.0];
    public double LearningRateValue { get; private set; } = 0.1;
    public IReadOnlyList<TrainingSample> Samples => _samples;

    public ScriptNetwork Dense(int count, ActivationKind activation)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        _layers.Add((count, activation));
        Targets = Enumerable.Repeat(1.0, count).ToArray();
        return this;
    }

    public ScriptNetwork Input(params double[] values)
    {
        if (values.Length != InputCount)
            throw new ArgumentException($"Network({InputCount}) requires {InputCount} input values.");
        Inputs = values.ToArray();
        return this;
    }

    public ScriptNetwork Target(params double[] values)
    {
        if (_layers.Count == 0) throw new InvalidOperationException("Add a Dense layer before Target.");
        if (values.Length != _layers[^1].Count)
            throw new ArgumentException($"Output layer requires {_layers[^1].Count} target values.");
        if (values.Any(x => x is < 0 or > 1))
            throw new ArgumentOutOfRangeException(nameof(values), "Targets must be between 0 and 1.");
        Targets = values.ToArray();
        return this;
    }

    public ScriptNetwork LearningRate(double value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        LearningRateValue = value;
        return this;
    }

    public ScriptNetwork Sample(double[] inputs, double[] targets)
    {
        if (inputs.Length != InputCount)
            throw new ArgumentException($"Sample requires {InputCount} input values.", nameof(inputs));
        if (_layers.Count == 0 || targets.Length != _layers[^1].Count)
            throw new ArgumentException("Sample target count must match the output layer.", nameof(targets));
        _samples.Add(new TrainingSample(inputs, targets));
        return this;
    }

    public NeuralScriptResult Build()
    {
        if (_layers.Count == 0) throw new InvalidOperationException("Add at least one Dense layer.");
        if (_layers[^1].Activation != ActivationKind.Sigmoid)
            throw new InvalidOperationException("The final layer must use Sigmoid for BCE/backprop.");

        var network = new NeuralNetwork();
        var previous = network.AddLayer(InputCount, "X", ActivationKind.Linear);
        var random = new Random(42);

        for (var index = 0; index < _layers.Count; index++)
        {
            var spec = _layers[index];
            var prefix = index == _layers.Count - 1 ? "O" : HiddenPrefix(index);
            var current = network.AddLayer(spec.Count, prefix, spec.Activation);
            var scale = Math.Sqrt(2.0 / Math.Max(1, previous.Neurons.Count));
            network.FullyConnect(previous, current, (_, _) => (random.NextDouble() * 2.0 - 1.0) * scale);
            previous = current;
        }

        return new NeuralScriptResult(network, Inputs, Targets, LearningRateValue, _samples.ToArray());
    }

    private static string HiddenPrefix(int index) => index switch
    {
        0 => "H", 1 => "J", 2 => "K", 3 => "L", _ => $"N{index}"
    };
}