using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.App.ViewModels;

public sealed class TrainingSample : INotifyPropertyChanged
{
    private IReadOnlyList<double>? _prediction;
    private double? _loss;

    public TrainingSample(IEnumerable<double> inputs, IEnumerable<double> targets)
    {
        Inputs = inputs.ToArray();
        Targets = targets.ToArray();
    }

    public TrainingSample(double x1, double x2, double target)
        : this([x1, x2], [target])
    {
    }

    public IReadOnlyList<double> Inputs { get; }
    public IReadOnlyList<double> Targets { get; }

    public string InputsText => string.Join(", ", Inputs.Select(x => x.ToString("0.####")));
    public string TargetsText => string.Join(", ", Targets.Select(x => x.ToString("0.####")));

    public IReadOnlyList<double>? Prediction
    {
        get => _prediction;
        set
        {
            if (ReferenceEquals(_prediction, value)) return;
            _prediction = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PredictionText));
        }
    }

    public double? Loss
    {
        get => _loss;
        set
        {
            if (_loss == value) return;
            _loss = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LossText));
        }
    }

    public string PredictionText => Prediction is null
        ? "—"
        : string.Join(", ", Prediction.Select(x => x.ToString("0.0000")));

    public string LossText => Loss?.ToString("0.0000") ?? "—";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}