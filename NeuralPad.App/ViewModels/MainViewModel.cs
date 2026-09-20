using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private int _stepIndex = -1;
    private object? _selectedObject;
    private double _x1 = 0.80;
    private double _x2 = 0.35;
    private ForwardStep? _currentStep;

    public MainViewModel()
    {
        Network = NeuralNetwork.CreateDemo();
        RunCommand = new RelayCommand(Run);
        StepCommand = new RelayCommand(Step);
        ResetCommand = new RelayCommand(Reset);
        Run();
    }

    public NeuralNetwork Network { get; }
    public IReadOnlyList<ForwardStep> Trace => Network.LastTrace;

    public double X1
    {
        get => _x1;
        set { if (SetField(ref _x1, value)) Run(); }
    }

    public double X2
    {
        get => _x2;
        set { if (SetField(ref _x2, value)) Run(); }
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
        ? "Ready"
        : $"Step {CurrentStep.Sequence}/{Trace.Count}: {CurrentStep.Title}";

    public string Formula => CurrentStep?.Formula ?? "Press Step to inspect the forward pass.";
    public double Output => Network.Layers[^1].Neurons[0].Activation;

    public ICommand RunCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand ResetCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Run()
    {
        Network.Forward(X1, X2);
        _stepIndex = Trace.Count - 1;
        CurrentStep = Trace.Count == 0 ? null : Trace[^1];
        SelectedObject = CurrentStep?.Neuron;
        RefreshComputed();
    }

    private void Step()
    {
        if (Trace.Count == 0 || _stepIndex >= Trace.Count - 1)
        {
            Network.Forward(X1, X2);
            _stepIndex = -1;
        }

        _stepIndex++;
        CurrentStep = Trace[_stepIndex];
        SelectedObject = (object?)CurrentStep.Connection ?? CurrentStep.Neuron;
        RefreshComputed();
    }

    private void Reset()
    {
        _stepIndex = -1;
        CurrentStep = null;
        SelectedObject = null;
        OnPropertyChanged(nameof(StepStatus));
        OnPropertyChanged(nameof(Formula));
    }

    private void RefreshComputed()
    {
        OnPropertyChanged(nameof(Trace));
        OnPropertyChanged(nameof(Output));
        OnPropertyChanged(nameof(StepStatus));
        OnPropertyChanged(nameof(Formula));
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