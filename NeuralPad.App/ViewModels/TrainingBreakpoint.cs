using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.App.ViewModels;

public enum TrainingBreakpointKind
{
    LossBelow,
    GradientAbove,
    WeightAbove,
    EpochEquals,
    SampleEquals
}

public sealed class TrainingBreakpoint : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private TrainingBreakpointKind _kind;
    private string _expression = string.Empty;
    private double _threshold;
    private string _status = string.Empty;

    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled == value) return; _isEnabled = value; OnPropertyChanged(); }
    }

    public TrainingBreakpointKind Kind
    {
        get => _kind;
        set { if (_kind == value) return; _kind = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConditionText)); }
    }

    public string Expression
    {
        get => _expression;
        set { if (_expression == value) return; _expression = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConditionText)); }
    }

    public double Threshold
    {
        get => _threshold;
        set { if (_threshold == value) return; _threshold = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConditionText)); }
    }

    public string ConditionText => Kind switch
    {
        TrainingBreakpointKind.LossBelow => $"Loss < {Threshold:0.######}",
        TrainingBreakpointKind.GradientAbove => $"max |gradient| > {Threshold:0.######}",
        TrainingBreakpointKind.WeightAbove => $"|{(string.IsNullOrWhiteSpace(Expression) ? "W(?,?)" : Expression)}| > {Threshold:0.######}",
        TrainingBreakpointKind.EpochEquals => $"Epoch = {(int)Threshold}",
        TrainingBreakpointKind.SampleEquals => $"Sample = {(int)Threshold}",
        _ => string.Empty
    };

    public string Status
    {
        get => _status;
        set { if (_status == value) return; _status = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}