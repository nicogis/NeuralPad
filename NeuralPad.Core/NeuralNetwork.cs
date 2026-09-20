namespace NeuralPad.Core;

public sealed class NeuralNetwork
{
    public List<Layer> Layers { get; } = [];
    public List<Connection> Connections { get; } = [];
    public IReadOnlyList<ForwardStep> LastTrace { get; private set; } = [];

    public Layer AddLayer(int neuronCount, string name, ActivationKind activation)
    {
        if (neuronCount <= 0) throw new ArgumentOutOfRangeException(nameof(neuronCount));
        var layer = new Layer(Layers.Count, name, activation);
        for (var i = 0; i < neuronCount; i++)
            layer.Neurons.Add(new Neuron($"{name}{i + 1}", layer.Index, i, activation));
        Layers.Add(layer);
        return layer;
    }

    public void FullyConnect(Layer from, Layer to, Func<int, int, double> weightFactory)
    {
        foreach (var source in from.Neurons)
        foreach (var target in to.Neurons)
            Connections.Add(new Connection(source, target, weightFactory(source.Index, target.Index)));
    }

    public ForwardSession BeginForward(params double[] inputs)
    {
        if (Layers.Count < 2)
            throw new InvalidOperationException("The network must contain at least two layers.");
        if (inputs.Length != Layers[0].Neurons.Count)
            throw new ArgumentException("Input count does not match the input layer.", nameof(inputs));

        ResetExecutionState();
        return new ForwardSession(this, inputs.ToArray());
    }

    public IReadOnlyList<ForwardStep> Forward(params double[] inputs)
    {
        var session = BeginForward(inputs);
        session.RunToEnd();
        return LastTrace;
    }

    public void ResetExecutionState()
    {
        foreach (var neuron in Layers.SelectMany(l => l.Neurons)) neuron.ResetState();
        foreach (var connection in Connections) connection.ResetState();
        LastTrace = [];
    }

    internal void SetTrace(IReadOnlyList<ForwardStep> trace) => LastTrace = trace.ToArray();

    public static NeuralNetwork CreateDemo()
    {
        var network = new NeuralNetwork();
        var input = network.AddLayer(2, "X", ActivationKind.Linear);
        var hidden = network.AddLayer(3, "H", ActivationKind.ReLU);
        var output = network.AddLayer(1, "O", ActivationKind.Sigmoid);

        var hiddenWeights = new double[,] { { 0.72, -0.31, 0.84 }, { -0.42, 0.61, 0.11 } };
        network.FullyConnect(input, hidden, (i, j) => hiddenWeights[i, j]);
        hidden.Neurons[0].Bias = 0.10;
        hidden.Neurons[1].Bias = -0.05;
        hidden.Neurons[2].Bias = 0.02;

        var outputWeights = new[] { 0.82, -0.31, 0.46 };
        network.FullyConnect(hidden, output, (i, _) => outputWeights[i]);
        output.Neurons[0].Bias = 0.15;
        return network;
    }
}