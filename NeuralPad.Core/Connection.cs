using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.Core;

public sealed class Connection : INotifyPropertyChanged
{
    private double _weight;

    public Connection(Neuron from, Neuron to, double weight)
    {
        From = from;
        To = to;
        _weight = weight;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Neuron From { get; }
    public Neuron To { get; }

    public double Weight
    {
        get => _weight;
        set
        {
            if (Math.Abs(_weight - value) < double.Epsilon) return;
            _weight = value;
            OnPropertyChanged();
        }
    }

    public double Contribution { get; internal set; }
    public bool HasContribution { get; internal set; }
    public double Gradient { get; internal set; }
    public bool HasGradient { get; internal set; }

    internal void ResetState()
    {
        Contribution = 0;
        HasContribution = false;
        Gradient = 0;
        HasGradient = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public override string ToString() => $"{From.Name} -> {To.Name}";
}