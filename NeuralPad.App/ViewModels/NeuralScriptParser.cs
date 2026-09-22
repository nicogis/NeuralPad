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
        @"\.Input\s*\(\s*(?<x1>[-+0-9.eE]+)\s*,\s*(?<x2>[-+0-9.eE]+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TargetRegex = new(
        @"\.Target\s*\(\s*(?<value>[-+0-9.eE]+)\s*\)",
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
            throw new InvalidOperationException("Missing Network(2).");

        var inputCount = int.Parse(networkMatch.Groups["inputs"].Value, CultureInfo.InvariantCulture);
        if (inputCount != 2)
            throw new InvalidOperationException("The current debugger supports exactly 2 inputs.");

        var denseMatches = DenseRegex.Matches(normalized);
        if (denseMatches.Count == 0)
            throw new InvalidOperationException("Add at least one Dense layer.");

        var layerSpecs = denseMatches.Select(m => new
        {
            Count = int.Parse(m.Groups["count"].Value, CultureInfo.InvariantCulture),
            Activation = Enum.Parse<ActivationKind>(m.Groups["activation"].Value, true)
        }).ToArray();

        if (layerSpecs[^1].Count != 1 || layerSpecs[^1].Activation != ActivationKind.Sigmoid)
            throw new InvalidOperationException("The final layer must be Dense(1, Sigmoid) for BCE/backprop.");

        var inputMatch = InputRegex.Match(normalized);
        var targetMatch = TargetRegex.Match(normalized);
        var lrMatch = LearningRateRegex.Match(normalized);

        var x1 = inputMatch.Success ? ParseDouble(inputMatch.Groups["x1"].Value) : 0.8;
        var x2 = inputMatch.Success ? ParseDouble(inputMatch.Groups["x2"].Value) : 0.35;
        var target = targetMatch.Success ? ParseDouble(targetMatch.Groups["value"].Value) : 1.0;
        var learningRate = lrMatch.Success ? ParseDouble(lrMatch.Groups["value"].Value) : 0.1;

        if (target is < 0 or > 1)
            throw new InvalidOperationException("Target must be between 0 and 1.");
        if (learningRate < 0)
            throw new InvalidOperationException("LearningRate must be >= 0.");

        var network = BuildNetwork(layerSpecs);
        return new NeuralScriptResult(network, x1, x2, target, learningRate);
    }

    private static NeuralNetwork BuildNetwork(dynamic[] specs)
    {
        var network = new NeuralNetwork();
        var previous = network.AddLayer(2, "X", ActivationKind.Linear);
        var random = new Random(42);

        for (var index = 0; index < specs.Length; index++)
        {
            var spec = specs[index];
            var isOutput = index == specs.Length - 1;
            var prefix = isOutput ? "O" : HiddenPrefix(index);
            var current = network.AddLayer((int)spec.Count, prefix, (ActivationKind)spec.Activation);

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

    private static double ParseDouble(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}