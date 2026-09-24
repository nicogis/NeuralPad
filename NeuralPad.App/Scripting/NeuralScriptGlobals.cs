using NeuralPad.Core;

namespace NeuralPad.App.Scripting;

public sealed class NeuralScriptGlobals
{
    private readonly Action<ScriptNetwork> _dump;

    public NeuralScriptGlobals(Action<ScriptNetwork> dump)
    {
        _dump = dump;
        Neural = new NeuralFactory();
    }

    public NeuralFactory Neural { get; }

    public void Dump(ScriptNetwork network) => _dump(network);

    public ActivationKind Linear => ActivationKind.Linear;
    public ActivationKind ReLU => ActivationKind.ReLU;
    public ActivationKind Sigmoid => ActivationKind.Sigmoid;
    public ActivationKind Tanh => ActivationKind.Tanh;
}