namespace NeuralPad.Core;

public sealed class Neuron
{
    public Neuron(string name, int layerIndex, int index, ActivationKind activation)
    {
        Name = name;
        LayerIndex = layerIndex;
        Index = index;
        ActivationKind = activation;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public int LayerIndex { get; }
    public int Index { get; }
    public ActivationKind ActivationKind { get; }
    public double Bias { get; set; }
    public double Z { get; internal set; }
    public double Activation { get; internal set; }
    public bool HasValue { get; internal set; }

    internal void ResetState()
    {
        Z = 0;
        Activation = 0;
        HasValue = false;
    }

    public override string ToString() => Name;
}