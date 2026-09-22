using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.Core;

public sealed class Neuron : INotifyPropertyChanged
{
    private double _bias;

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

    public double Bias
    {
        get => _bias;
        set
        {
            if (Math.Abs(_bias - value) < double.Epsilon) return;
            _bias = value;
            OnPropertyChanged();
        }
    }

    public double Z { get; internal set; }
    public double Activation { get; internal set; }
    public bool HasValue { get; internal set; }

    public double Delta { get; internal set; }
    public double BiasGradient { get; internal set; }
    public bool HasGradient { get; internal set; }

    public double BiasBeforeUpdate { get; internal set; }
    public double BiasUpdate { get; internal set; }
    public bool HasOptimizerUpdate { get; internal set; }

    internal void ResetState()
    {
        Z = 0;
        Activation = 0;
        HasValue = false;
        Delta = 0;
        BiasGradient = 0;
        HasGradient = false;
        BiasBeforeUpdate = 0;
        BiasUpdate = 0;
        HasOptimizerUpdate = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public override string ToString() => Name;
}