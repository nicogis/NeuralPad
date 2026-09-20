namespace NeuralPad.Core;

public sealed class Connection
{
    public Connection(Neuron from, Neuron to, double weight)
    {
        From = from;
        To = to;
        Weight = weight;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Neuron From { get; }
    public Neuron To { get; }
    public double Weight { get; set; }
    public double Contribution { get; internal set; }

    public override string ToString() => $"{From.Name} -> {To.Name}";
}