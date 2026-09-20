namespace NeuralPad.Core;

public sealed class BackwardSession
{
    private readonly NeuralNetwork _network;
    private readonly double _target;
    private readonly List<BackwardStep> _steps = [];
    private readonly Queue<Func<BackwardStep>> _operations = new();
    private int _sequence;

    internal BackwardSession(NeuralNetwork network, double target)
    {
        _network = network;
        _target = target;
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
        var output = _network.Layers[^1].Neurons.Single();
        _operations.Enqueue(() =>
        {
            var yHat = Math.Clamp(output.Activation, 1e-12, 1 - 1e-12);
            var loss = -(_target * Math.Log(yHat) + (1 - _target) * Math.Log(1 - yHat));
            _network.Loss = loss;
            return New(BackwardStepKind.Loss, "Binary Cross Entropy",
                $"-[y*ln(y_hat)+(1-y)*ln(1-y_hat)] = {loss:0.######}", loss, output);
        });

        _operations.Enqueue(() =>
        {
            output.Delta = output.Activation - _target;
            output.HasGradient = true;
            return New(BackwardStepKind.OutputDelta, $"{output.Name} delta",
                $"dL/dz = y_hat - y = {output.Activation:0.######} - {_target:0.######} = {output.Delta:0.######}",
                output.Delta, output);
        });

        foreach (var connection in _network.Connections.Where(c => c.To == output))
        {
            var local = connection;
            _operations.Enqueue(() =>
            {
                local.Gradient = local.From.Activation * output.Delta;
                local.HasGradient = true;
                return New(BackwardStepKind.WeightGradient, $"{local} gradient",
                    $"dL/dw = a_prev * delta = {local.From.Activation:0.######} * {output.Delta:0.######} = {local.Gradient:0.######}",
                    local.Gradient, output, local);
            });
        }

        _operations.Enqueue(() =>
        {
            output.BiasGradient = output.Delta;
            return New(BackwardStepKind.BiasGradient, $"{output.Name} bias gradient",
                $"dL/db = delta = {output.BiasGradient:0.######}", output.BiasGradient, output);
        });

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