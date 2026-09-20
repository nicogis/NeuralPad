namespace NeuralPad.Core;

public enum ForwardStepKind
{
    Input,
    Contribution,
    WeightedSum,
    Bias,
    Activation,
    Output
}

public sealed record ForwardStep(
    int Sequence,
    ForwardStepKind Kind,
    string Title,
    string Formula,
    double Value,
    Neuron? Neuron = null,
    Connection? Connection = null);