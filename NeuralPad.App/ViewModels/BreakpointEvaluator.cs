using System.Text.RegularExpressions;
using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed class BreakpointEvaluator(NeuralNetwork network)
{
    private static readonly Regex WeightExpression = new(
        @"^W\(\s*(?<from>[A-Za-z][A-Za-z0-9]*)\s*,\s*(?<to>[A-Za-z][A-Za-z0-9]*)\s*\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool IsMatch(TrainingBreakpoint bp, int epoch, int sampleIndex, out string reason)
    {
        reason = string.Empty;
        if (!bp.IsEnabled) return false;

        switch (bp.Kind)
        {
            case TrainingBreakpointKind.LossBelow:
                if (network.Loss is double loss && loss < bp.Threshold)
                {
                    reason = $"Loss {loss:0.######} < {bp.Threshold:0.######}";
                    return true;
                }
                return false;

            case TrainingBreakpointKind.GradientAbove:
                var gradient = network.Connections.Where(c => c.HasGradient)
                    .Select(c => Math.Abs(c.Gradient))
                    .DefaultIfEmpty(0)
                    .Max();
                if (gradient > bp.Threshold)
                {
                    reason = $"Max |gradient| {gradient:0.######} > {bp.Threshold:0.######}";
                    return true;
                }
                return false;

            case TrainingBreakpointKind.WeightAbove:
                var match = WeightExpression.Match(bp.Expression ?? string.Empty);
                if (!match.Success)
                {
                    bp.Status = "Use W(H1,O1)";
                    return false;
                }

                var connection = FindConnection(match.Groups["from"].Value, match.Groups["to"].Value);
                if (connection is null)
                {
                    bp.Status = "Connection not found";
                    return false;
                }

                if (Math.Abs(connection.Weight) > bp.Threshold)
                {
                    reason = $"|{bp.Expression}| = {Math.Abs(connection.Weight):0.######} > {bp.Threshold:0.######}";
                    return true;
                }
                return false;

            case TrainingBreakpointKind.EpochEquals:
                if (epoch == (int)bp.Threshold)
                {
                    reason = $"Epoch = {epoch}";
                    return true;
                }
                return false;

            case TrainingBreakpointKind.SampleEquals:
                if (sampleIndex + 1 == (int)bp.Threshold)
                {
                    reason = $"Sample = {sampleIndex + 1}";
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    private Connection? FindConnection(string from, string to) =>
        network.Connections.FirstOrDefault(c =>
            c.From.Name.Equals(from, StringComparison.OrdinalIgnoreCase) &&
            c.To.Name.Equals(to, StringComparison.OrdinalIgnoreCase));
}