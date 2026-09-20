namespace NeuralPad.Core;

public static class ActivationFunctions
{
    public static double Apply(ActivationKind kind, double value) => kind switch
    {
        ActivationKind.Linear => value,
        ActivationKind.ReLU => Math.Max(0.0, value),
        ActivationKind.Sigmoid => 1.0 / (1.0 + Math.Exp(-value)),
        ActivationKind.Tanh => Math.Tanh(value),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}