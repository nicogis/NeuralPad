using System.Globalization;
using System.Text.RegularExpressions;
using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed class WatchEvaluator(NeuralNetwork network)
{
    private static readonly Regex NeuronExpression = new(
        @"^(?<name>[A-Za-z][A-Za-z0-9]*).(?<property>Activation|Z|Bias)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex WeightExpression = new(
        @"^W(s*(?<from>[A-Za-z][A-Za-z0-9]*)s*,s*(?<to>[A-Za-z][A-Za-z0-9]*)s*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public (string Value, string? Error) Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return ("—", null);
        expression = expression.Trim();

        var neuronMatch = NeuronExpression.Match(expression);
        if (neuronMatch.Success)
        {
            var neuron = FindNeuron(neuronMatch.Groups["name"].Value);
            if (neuron is null) return ("—", $"Unknown neuron '{neuronMatch.Groups["name"].Value}'.");

            return neuronMatch.Groups["property"].Value.ToUpperInvariant() switch
            {
                "BIAS" => (Format(neuron.Bias), null),
                "Z" when neuron.HasValue => (Format(neuron.Z), null),
                "ACTIVATION" when neuron.HasValue => (Format(neuron.Activation), null),
                _ => ("—", null)
            };
        }

        var weightMatch = WeightExpression.Match(expression);
        if (weightMatch.Success)
        {
            var from = FindNeuron(weightMatch.Groups["from"].Value);
            var to = FindNeuron(weightMatch.Groups["to"].Value);
            if (from is null || to is null) return ("—", "Unknown neuron in weight expression.");
            var connection = network.Connections.FirstOrDefault(c => c.From == from && c.To == to);
            return connection is null
                ? ("—", $"No connection {from.Name} -> {to.Name}.")
                : (Format(connection.Weight), null);
        }

        return ("—", "Supported: H1.Activation, O1.Z, H1.Bias, W(H1,O1)");
    }

    private Neuron? FindNeuron(string name) =>
        network.Layers.SelectMany(l => l.Neurons)
            .FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string Format(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}