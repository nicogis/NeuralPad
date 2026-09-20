using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.App.ViewModels;

public sealed class WatchItem : INotifyPropertyChanged
{
    private string _expression = string.Empty;
    private string _value = "—";
    private string? _error;

    public WatchItem(string expression) => _expression = expression;

    public string Expression
    {
        get => _expression;
        set
        {
            if (_expression == value) return;
            _expression = value;
            OnPropertyChanged();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (_error == value) return;
            _error = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}