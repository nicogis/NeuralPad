namespace NeuralPad.Core;

public enum BackwardStepKind
{
    Loss,
    OutputDelta,
    WeightGradient,
    BiasGradient,
    HiddenDelta
}

public sealed record BackwardStep(
    int Sequence,
    BackwardStepKind Kind,
    string Title,
    string Formula,
    double Value,
    Neuron? Neuron = null,
    Connection? Connection = null);