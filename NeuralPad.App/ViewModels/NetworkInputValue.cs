using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.App.ViewModels;

public sealed class NetworkInputValue : INotifyPropertyChanged
{
    private double _value;

    public NetworkInputValue(string name, double value)
    {
        Name = name;
        _value = value;
    }

    public string Name { get; }

    public double Value
    {
        get => _value;
        set
        {
            if (Math.Abs(_value - value) < double.Epsilon) return;
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}