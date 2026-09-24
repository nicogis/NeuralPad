using System.Globalization;
using System.Text.RegularExpressions;
using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public static class NeuralScriptParser
{
    private static readonly Regex NetworkRegex = new(
        @"Network\s*\(\s*(?<inputs>\d+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DenseRegex = new(
        @"\.Dense\s*\(\s*(?<count>\d+)\s*,\s*(?<activation>Linear|ReLU|Sigmoid|Tanh)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InputRegex = new(
        @"\.Input\s*\((?<values>[^)]*)\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TargetRegex = new(
        @"\.Target\s*\((?<values>[^)]*)\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SampleRegex = new(
        @"\.Sample\s*\(\s*\[(?<inputs>[^\]]*)\]\s*,\s*\[(?<targets>[^\]]*)\]\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LearningRateRegex = new(
        @"\.LearningRate\s*\(\s*(?<value>[-+0-9.eE]+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static NeuralScriptResult Parse(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new InvalidOperationException("Script is empty.");

        var normalized = Regex.Replace(script, @"//.*?$", string.Empty, RegexOptions.Multiline)
            .Replace("\r", " ")
            .Replace("\n", " ");

        var networkMatch = NetworkRegex.Match(normalized);
        if (!networkMatch.Success)
            throw new InvalidOperationException("Missing Network(n).");

        var inputCount = int.Parse(networkMatch.Groups["inputs"].Value, CultureInfo.InvariantCulture);
        if (inputCount <= 0)
            throw new InvalidOperationException("Network input count must be greater than zero.");

        var denseMatches = DenseRegex.Matches(normalized);
        if (denseMatches.Count == 0)
            throw new InvalidOperationException("Add at least one Dense layer.");

        var layerSpecs = denseMatches.Select(m => new LayerSpec(
            int.Parse(m.Groups["count"].Value, CultureInfo.InvariantCulture),
            Enum.Parse<ActivationKind>(m.Groups["activation"].Value, true))).ToArray();

        if (layerSpecs[^1].Activation != ActivationKind.Sigmoid)
            throw new InvalidOperationException("The final layer must use Sigmoid for BCE/backprop.");

        var inputMatch = InputRegex.Match(normalized);
        IReadOnlyList<double> inputs;
        if (inputMatch.Success)
        {
            inputs = inputMatch.Groups["values"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseDouble)
                .ToArray();

            if (inputs.Count != inputCount)
                throw new InvalidOperationException($"Network({inputCount}) requires exactly {inputCount} values in Input(...).");
        }
        else
        {
            inputs = Enumerable.Range(0, inputCount)
                .Select(i => i switch { 0 => 0.8, 1 => 0.35, _ => 0.0 })
                .ToArray();
        }

        var targetMatch = TargetRegex.Match(normalized);
        var outputCount = layerSpecs[^1].Count;
        IReadOnlyList<double> targets;
        if (targetMatch.Success)
        {
            targets = targetMatch.Groups["values"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseDouble)
                .ToArray();
            if (targets.Count != outputCount)
                throw new InvalidOperationException($"The output layer has {outputCount} neurons, so Target(...) requires {outputCount} values.");
        }
        else
        {
            targets = Enumerable.Repeat(1.0, outputCount).ToArray();
        }

        var lrMatch = LearningRateRegex.Match(normalized);
        var learningRate = lrMatch.Success ? ParseDouble(lrMatch.Groups["value"].Value) : 0.1;

        if (targets.Any(x => x is < 0 or > 1))
            throw new InvalidOperationException("Every target must be between 0 and 1.");
        if (learningRate < 0)
            throw new InvalidOperationException("LearningRate must be >= 0.");

        var samples = SampleRegex.Matches(normalized)
            .Select(match =>
            {
                var sampleInputs = ParseValues(match.Groups["inputs"].Value);
                var sampleTargets = ParseValues(match.Groups["targets"].Value);

                if (sampleInputs.Length != inputCount)
                    throw new InvalidOperationException($"Each Sample requires {inputCount} input values.");
                if (sampleTargets.Length != outputCount)
                    throw new InvalidOperationException($"Each Sample requires {outputCount} target values.");
                if (sampleTargets.Any(x => x is < 0 or > 1))
                    throw new InvalidOperationException("Every sample target must be between 0 and 1.");

                return new TrainingSample(sampleInputs, sampleTargets);
            })
            .ToArray();

        var network = BuildNetwork(inputCount, layerSpecs);
        return new NeuralScriptResult(network, inputs, targets, learningRate, samples);
    }

    private static NeuralNetwork BuildNetwork(int inputCount, LayerSpec[] specs)
    {
        var network = new NeuralNetwork();
        var previous = network.AddLayer(inputCount, "X", ActivationKind.Linear);
        var random = new Random(42);

        for (var index = 0; index < specs.Length; index++)
        {
            var spec = specs[index];
            var isOutput = index == specs.Length - 1;
            var prefix = isOutput ? "O" : HiddenPrefix(index);
            var current = network.AddLayer(spec.Count, prefix, spec.Activation);

            var fanIn = previous.Neurons.Count;
            var scale = Math.Sqrt(2.0 / Math.Max(1, fanIn));
            network.FullyConnect(previous, current, (_, _) => (random.NextDouble() * 2.0 - 1.0) * scale);
            previous = current;
        }

        return network;
    }

    private static string HiddenPrefix(int index) => index switch
    {
        0 => "H",
        1 => "J",
        2 => "K",
        3 => "L",
        _ => $"N{index}"
    };

    private sealed record LayerSpec(int Count, ActivationKind Activation);

    private static double[] ParseValues(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseDouble)
            .ToArray();

    private static double ParseDouble(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}