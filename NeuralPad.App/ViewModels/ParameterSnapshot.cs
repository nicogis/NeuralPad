namespace NeuralPad.App.ViewModels;

public sealed class ParameterSnapshot
{
    public required string Name { get; init; }
    public required int Epoch { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyDictionary<string, double> Values { get; init; }

    public override string ToString() => $"{Name} (epoch {Epoch})";
}