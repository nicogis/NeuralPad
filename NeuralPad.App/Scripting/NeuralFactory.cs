namespace NeuralPad.App.Scripting;

public sealed class NeuralFactory
{
    public ScriptNetwork Network(int inputCount) => new(inputCount);
}