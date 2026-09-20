namespace NeuralPad.Core;

public sealed class Layer
{
    public Layer(int index, string name, ActivationKind activation)
    {
        Index = index;
        Name = name;
        Activation = activation;
    }

    public int Index { get; }
    public string Name { get; }
    public ActivationKind Activation { get; }
    public List<Neuron> Neurons { get; } = [];
}