namespace NeuralPad.App.ViewModels;

public sealed record ParameterDelta(
    string Parameter,
    double FromValue,
    double ToValue,
    double Delta,
    double AbsoluteDelta);