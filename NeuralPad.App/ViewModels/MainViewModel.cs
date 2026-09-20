using System.Collections.ObjectModel;
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
    private readonly WatchEvaluator _watchEvaluator;

    public MainViewModel()
    {
        Network = NeuralNetwork.CreateDemo();
        _watchEvaluator = new WatchEvaluator(Network);

        foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons))
            neuron.PropertyChanged += ParameterChanged;
        foreach (var connection in Network.Connections)
            connection.PropertyChanged += ParameterChanged;

        Watches =
        [
            CreateWatch("H1.Activation"),
            CreateWatch("H2.Activation"),
            CreateWatch("O1.Z"),
            CreateWatch("O1.Activation"),
            CreateWatch("W(H1,O1)")
        ];

        RunCommand = new RelayCommand(Run);
        StepCommand = new RelayCommand(Step);
        ResetCommand = new RelayCommand(Reset);
        AddWatchCommand = new RelayCommand(AddWatch);
        RemoveWatchCommand = new RelayCommand(RemoveSelectedWatch, () => SelectedWatch is not null);
        Reset();
    }

    public NeuralNetwork Network { get; }
    public IReadOnlyList<ForwardStep> Trace => Network.LastTrace;
    public ObservableCollection<WatchItem> Watches { get; }

    private WatchItem? _selectedWatch;
    public WatchItem? SelectedWatch
    {
        get => _selectedWatch;
        set
        {
            if (SetField(ref _selectedWatch, value) && RemoveWatchCommand is RelayCommand command)
                command.RaiseCanExecuteChanged();
        }
    }

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
    public ICommand AddWatchCommand { get; }
    public ICommand RemoveWatchCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private WatchItem CreateWatch(string expression)
    {
        var item = new WatchItem(expression);
        item.PropertyChanged += WatchChanged;
        return item;
    }

    private void AddWatch()
    {
        var item = CreateWatch("H1.Activation");
        Watches.Add(item);
        SelectedWatch = item;
        RefreshWatches();
    }

    private void RemoveSelectedWatch()
    {
        if (SelectedWatch is null) return;
        SelectedWatch.PropertyChanged -= WatchChanged;
        Watches.Remove(SelectedWatch);
        SelectedWatch = null;
    }

    private void WatchChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WatchItem.Expression) && sender is WatchItem item)
            RefreshWatch(item);
    }

    private void RefreshWatches()
    {
        foreach (var watch in Watches) RefreshWatch(watch);
    }

    private void RefreshWatch(WatchItem watch)
    {
        var result = _watchEvaluator.Evaluate(watch.Expression);
        watch.Value = result.Value;
        watch.Error = result.Error;
    }

    private void ParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
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
        RefreshWatches();
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