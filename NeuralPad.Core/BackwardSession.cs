namespace NeuralPad.Core;

public sealed class BackwardSession
{
    private readonly NeuralNetwork _network;
    private readonly double[] _targets;
    private readonly List<BackwardStep> _steps = [];
    private readonly Queue<Func<BackwardStep>> _operations = new();
    private int _sequence;

    internal BackwardSession(NeuralNetwork network, double[] targets)
    {
        _network = network;
        _targets = targets;
        BuildOperations();
    }

    public IReadOnlyList<BackwardStep> ExecutedSteps => _steps;
    public bool IsCompleted => _operations.Count == 0;
    public BackwardStep? CurrentStep => _steps.Count == 0 ? null : _steps[^1];

    public BackwardStep? Step()
    {
        if (_operations.Count == 0) return null;
        var step = _operations.Dequeue()();
        _steps.Add(step);
        return step;
    }

    public void RunToEnd()
    {
        while (!IsCompleted) Step();
    }

    private void BuildOperations()
    {
        var outputs = _network.Layers[^1].Neurons.ToArray();
        var output = outputs[0];
        _operations.Enqueue(() =>
        {
            var loss = 0.0;
            for (var i = 0; i < outputs.Length; i++)
            {
                var yHat = Math.Clamp(outputs[i].Activation, 1e-12, 1 - 1e-12);
                var target = _targets[i];
                loss += -(target * Math.Log(yHat) + (1 - target) * Math.Log(1 - yHat));
            }
            _network.Loss = loss;
            return New(BackwardStepKind.Loss, "Binary Cross Entropy",
                outputs.Length == 1 ? $"BCE = {loss:0.######}" : $"sum BCE(O1..O{outputs.Length}) = {loss:0.######}", loss, output);
        });

        for (var outputIndex = 0; outputIndex < outputs.Length; outputIndex++)
        {
            var currentOutput = outputs[outputIndex];
            var currentTarget = _targets[outputIndex];

            _operations.Enqueue(() =>
            {
                currentOutput.Delta = currentOutput.Activation - currentTarget;
                currentOutput.HasGradient = true;
                return New(BackwardStepKind.OutputDelta, $"{currentOutput.Name} delta",
                    $"dL/dz = y_hat - y = {currentOutput.Activation:0.######} - {currentTarget:0.######} = {currentOutput.Delta:0.######}",
                    currentOutput.Delta, currentOutput);
            });

            foreach (var connection in _network.Connections.Where(c => c.To == currentOutput))
            {
                var local = connection;
                _operations.Enqueue(() =>
                {
                    local.Gradient = local.From.Activation * currentOutput.Delta;
                    local.HasGradient = true;
                    return New(BackwardStepKind.WeightGradient, $"{local} gradient",
                        $"dL/dw = a_prev * delta = {local.From.Activation:0.######} * {currentOutput.Delta:0.######} = {local.Gradient:0.######}",
                        local.Gradient, currentOutput, local);
                });
            }

            _operations.Enqueue(() =>
            {
                currentOutput.BiasGradient = currentOutput.Delta;
                return New(BackwardStepKind.BiasGradient, $"{currentOutput.Name} bias gradient",
                    $"dL/db = delta = {currentOutput.BiasGradient:0.######}", currentOutput.BiasGradient, currentOutput);
            });
        }

        for (var layerIndex = _network.Layers.Count - 2; layerIndex >= 1; layerIndex--)
        {
            var layer = _network.Layers[layerIndex];
            foreach (var neuron in layer.Neurons)
            {
                var n = neuron;
                _operations.Enqueue(() =>
                {
                    var downstream = _network.Connections.Where(c => c.From == n).Sum(c => c.Weight * c.To.Delta);
                    var derivative = n.ActivationKind switch
                    {
                        ActivationKind.ReLU => n.Z > 0 ? 1.0 : 0.0,
                        ActivationKind.Sigmoid => n.Activation * (1 - n.Activation),
                        ActivationKind.Tanh => 1 - n.Activation * n.Activation,
                        _ => 1.0
                    };
                    n.Delta = downstream * derivative;
                    n.HasGradient = true;
                    return New(BackwardStepKind.HiddenDelta, $"{n.Name} delta",
                        $"delta = sum(w_next*delta_next) * f'(z) = {n.Delta:0.######}",
                        n.Delta, n);
                });

                foreach (var connection in _network.Connections.Where(c => c.To == n))
                {
                    var local = connection;
                    _operations.Enqueue(() =>
                    {
                        local.Gradient = local.From.Activation * n.Delta;
                        local.HasGradient = true;
                        return New(BackwardStepKind.WeightGradient, $"{local} gradient",
                            $"dL/dw = a_prev * delta = {local.From.Activation:0.######} * {n.Delta:0.######} = {local.Gradient:0.######}",
                            local.Gradient, n, local);
                    });
                }

                _operations.Enqueue(() =>
                {
                    n.BiasGradient = n.Delta;
                    return New(BackwardStepKind.BiasGradient, $"{n.Name} bias gradient",
                        $"dL/db = delta = {n.BiasGradient:0.######}", n.BiasGradient, n);
                });
            }
        }
    }

    private BackwardStep New(BackwardStepKind kind, string title, string formula, double value,
        Neuron? neuron = null, Connection? connection = null) =>
        new(++_sequence, kind, title, formula, value, neuron, connection);
}