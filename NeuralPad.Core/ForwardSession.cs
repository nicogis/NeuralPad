namespace NeuralPad.Core;

public sealed class ForwardSession
{
    private readonly NeuralNetwork _network;
    private readonly double[] _inputs;
    private readonly List<ForwardStep> _executed = [];
    private int _inputIndex;
    private int _layerIndex = 1;
    private int _neuronIndex;
    private int _connectionIndex;
    private Phase _phase = Phase.Input;
    private double _sum;
    private int _sequence;

    internal ForwardSession(NeuralNetwork network, double[] inputs)
    {
        _network = network;
        _inputs = inputs;
    }

    public IReadOnlyList<ForwardStep> ExecutedSteps => _executed;
    public bool IsCompleted { get; private set; }
    public ForwardStep? CurrentStep => _executed.Count == 0 ? null : _executed[^1];

    public ForwardStep? Step()
    {
        if (IsCompleted) return null;

        ForwardStep step;
        if (_phase == Phase.Input)
        {
            var neuron = _network.Layers[0].Neurons[_inputIndex];
            var value = _inputs[_inputIndex];
            neuron.Z = value;
            neuron.Activation = value;
            neuron.HasValue = true;
            step = New(ForwardStepKind.Input, neuron.Name, $"{neuron.Name} = {value:0.####}", value, neuron);
            _inputIndex++;
            if (_inputIndex >= _inputs.Length) _phase = Phase.Contribution;
            return Add(step);
        }

        var layer = _network.Layers[_layerIndex];
        var target = layer.Neurons[_neuronIndex];
        var incoming = _network.Connections.Where(c => c.To == target).ToArray();

        switch (_phase)
        {
            case Phase.Contribution:
                var connection = incoming[_connectionIndex];
                connection.Contribution = connection.From.Activation * connection.Weight;
                connection.HasContribution = true;
                _sum += connection.Contribution;
                step = New(ForwardStepKind.Contribution, connection.ToString(),
                    $"{connection.From.Name}.a x {connection.Weight:0.####} = {connection.Contribution:0.####}",
                    connection.Contribution, target, connection);
                _connectionIndex++;
                if (_connectionIndex >= incoming.Length) _phase = Phase.Sum;
                return Add(step);

            case Phase.Sum:
                _phase = Phase.Bias;
                return Add(New(ForwardStepKind.WeightedSum, $"{target.Name} weighted sum",
                    $"sum(x_i*w_i) = {_sum:0.####}", _sum, target));

            case Phase.Bias:
                target.Z = _sum + target.Bias;
                _phase = Phase.Activation;
                return Add(New(ForwardStepKind.Bias, $"{target.Name} bias",
                    $"{_sum:0.####} + {target.Bias:0.####} = {target.Z:0.####}", target.Z, target));

            case Phase.Activation:
                target.Activation = ActivationFunctions.Apply(target.ActivationKind, target.Z);
                target.HasValue = true;
                var kind = _layerIndex == _network.Layers.Count - 1
                    ? ForwardStepKind.Output
                    : ForwardStepKind.Activation;
                step = New(kind, target.Name,
                    $"{target.ActivationKind}({target.Z:0.####}) = {target.Activation:0.####}",
                    target.Activation, target);
                AdvanceNeuron();
                return Add(step);

            default:
                throw new InvalidOperationException();
        }
    }

    public void RunToEnd()
    {
        while (!IsCompleted) Step();
    }

    private void AdvanceNeuron()
    {
        _sum = 0;
        _connectionIndex = 0;
        _neuronIndex++;

        if (_neuronIndex < _network.Layers[_layerIndex].Neurons.Count)
        {
            _phase = Phase.Contribution;
            return;
        }

        _neuronIndex = 0;
        _layerIndex++;
        if (_layerIndex >= _network.Layers.Count)
        {
            IsCompleted = true;
            return;
        }

        _phase = Phase.Contribution;
    }

    private ForwardStep New(ForwardStepKind kind, string title, string formula, double value,
        Neuron? neuron = null, Connection? connection = null) =>
        new(++_sequence, kind, title, formula, value, neuron, connection);

    private ForwardStep Add(ForwardStep step)
    {
        _executed.Add(step);
        _network.SetTrace(_executed);
        return step;
    }

    private enum Phase { Input, Contribution, Sum, Bias, Activation }
}