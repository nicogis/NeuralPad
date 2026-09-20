using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private object? _selectedObject;
    private double _x1 = 0.80;
    private double _x2 = 0.35;
    private ForwardStep? _currentStep;
    private ForwardSession? _session;

    public MainViewModel()
    {
        Network = NeuralNetwork.CreateDemo();
        RunCommand = new RelayCommand(Run);
        StepCommand = new RelayCommand(Step);
        ResetCommand = new RelayCommand(Reset);
        Reset();
    }

    public NeuralNetwork Network { get; }
    public IReadOnlyList<ForwardStep> Trace => Network.LastTrace;

    public double X1
    {
        get => _x1;
        set { if (SetField(ref _x1, value)) Reset(); }
    }

    public double X2
    {
        get => _x2;
        set { if (SetField(ref _x2, value)) Reset(); }
    }

    public ForwardStep? CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public object? SelectedObject
    {
        get => _selectedObject;
        set => SetField(ref _selectedObject, value);
    }

    public string StepStatus => CurrentStep is null
        ? "Ready - press Step to begin"
        : _session?.IsCompleted == true
            ? $"Completed: {Trace.Count} steps"
            : $"Step {CurrentStep.Sequence}: {CurrentStep.Title}";

    public string Formula => CurrentStep?.Formula ?? "The network has not executed any operation yet.";
    public string OutputText => Network.Layers[^1].Neurons[0] is { HasValue: true } output
        ? output.Activation.ToString("0.000000")
        : "—";

    public ICommand RunCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand ResetCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void EnsureSession() => _session ??= Network.BeginForward(X1, X2);

    private void Run()
    {
        EnsureSession();
        _session!.RunToEnd();
        CurrentStep = _session.CurrentStep;
        SelectedObject = CurrentStep?.Neuron;
        RefreshComputed();
    }

    private void Step()
    {
        if (_session?.IsCompleted == true)
            Reset();

        EnsureSession();
        CurrentStep = _session!.Step();
        SelectedObject = (object?)CurrentStep?.Connection ?? CurrentStep?.Neuron;
        RefreshComputed();
    }

    private void Reset()
    {
        Network.ResetExecutionState();
        _session = null;
        CurrentStep = null;
        SelectedObject = null;
        RefreshComputed();
    }

    private void RefreshComputed()
    {
        OnPropertyChanged(nameof(Trace));
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(StepStatus));
        OnPropertyChanged(nameof(Formula));
        OnPropertyChanged(nameof(Network));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}