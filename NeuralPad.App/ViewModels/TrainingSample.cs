using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeuralPad.App.ViewModels;

public sealed class TrainingSample : INotifyPropertyChanged
{
    private double? _prediction;
    private double? _loss;

    public TrainingSample(double x1, double x2, double target)
    {
        X1 = x1;
        X2 = x2;
        Target = target;
    }

    public double X1 { get; }
    public double X2 { get; }
    public double Target { get; }

    public double? Prediction
    {
        get => _prediction;
        set { if (_prediction == value) return; _prediction = value; OnPropertyChanged(); OnPropertyChanged(nameof(PredictionText)); }
    }

    public double? Loss
    {
        get => _loss;
        set { if (_loss == value) return; _loss = value; OnPropertyChanged(); OnPropertyChanged(nameof(LossText)); }
    }

    public string PredictionText => Prediction?.ToString("0.0000") ?? "—";
    public string LossText => Loss?.ToString("0.0000") ?? "—";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}