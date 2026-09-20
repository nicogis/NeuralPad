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
        foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons))
            neuron.PropertyChanged += ParameterChanged;
        foreach (var connection in Network.Connections)
            connection.PropertyChanged += ParameterChanged;

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
        set { if (SetField(ref _x1, value)) ResetExecution(keepSelection: true); }
    }

    public double X2
    {
        get => _x2;
        set { if (SetField(ref _x2, value)) ResetExecution(keepSelection: true); }
    }

    public ForwardStep? CurrentStep
    {
        get => _currentStep;
        private set => SetField(ref _currentStep, value);
    }

    public object? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (SetField(ref _selectedObject, value))
                OnPropertyChanged(nameof(SelectionText));
        }
    }

    public string SelectionText => SelectedObject switch
    {
        Connection c => $"Connection {c.From.Name} -> {c.To.Name} | W = {c.Weight:0.####}",
        Neuron n => $"Neuron {n.Name} | Bias = {n.Bias:0.####}",
        _ => "Click a neuron or connection to inspect it."
    };

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

    private void ParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Changing a parameter invalidates every downstream value. Keep the selected
        // graph object so the PropertyGrid remains useful while experimenting.
        ResetExecution(keepSelection: true);
        OnPropertyChanged(nameof(SelectionText));
    }

    private void EnsureSession() => _session ??= Network.BeginForward(X1, X2);

    private void Run()
    {
        EnsureSession();
        _session!.RunToEnd();
        CurrentStep = _session.CurrentStep;
        RefreshComputed();
    }

    private void Step()
    {
        if (_session?.IsCompleted == true)
            ResetExecution(keepSelection: true);

        EnsureSession();
        CurrentStep = _session!.Step();
        if (SelectedObject is null)
            SelectedObject = (object?)CurrentStep?.Connection ?? CurrentStep?.Neuron;
        RefreshComputed();
    }

    private void Reset() => ResetExecution(keepSelection: false);

    private void ResetExecution(bool keepSelection)
    {
        Network.ResetExecutionState();
        _session = null;
        CurrentStep = null;
        if (!keepSelection) SelectedObject = null;
        RefreshComputed();
    }

    private void RefreshComputed()
    {
        OnPropertyChanged(nameof(Trace));
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(StepStatus));
        OnPropertyChanged(nameof(Formula));
        OnPropertyChanged(nameof(Network));
        OnPropertyChanged(nameof(SelectedObject));
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