using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private object? _selectedObject;
    private double _x1 = 0.80, _x2 = 0.35, _target = 1.0, _learningRate = 0.10;
    private ForwardStep? _currentStep;
    private BackwardStep? _currentBackwardStep;
    private ForwardSession? _session;
    private BackwardSession? _backwardSession;
    private WatchEvaluator _watchEvaluator;
    private BreakpointEvaluator _breakpointEvaluator;
    private string _breakpointStatus = "No breakpoint hit";
    private TrainingSample? _selectedTrainingSample;
    private int _epoch;
    private int _sampleIndex;
    private int _snapshotNumber;
    private string _optimizerStatus = "Optimizer not executed";
    private string _scriptText = """
var net = Neural.Network(2)
    .Dense(3, ReLU)
    .Dense(1, Sigmoid)
    .Input(0.80, 0.35)
    .Target(1)
    .LearningRate(0.10);
""";
    private string _scriptStatus = "Ready";

    public MainViewModel()
    {
        Network = NeuralNetwork.CreateDemo();
        _watchEvaluator = new WatchEvaluator(Network);
        _breakpointEvaluator = new BreakpointEvaluator(Network);
        foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons)) neuron.PropertyChanged += ParameterChanged;
        foreach (var connection in Network.Connections) connection.PropertyChanged += ParameterChanged;

        Watches = [CreateWatch("H1.Activation"), CreateWatch("O1.Z"), CreateWatch("O1.Activation"), CreateWatch("W(H1,O1)")];
        TrainingSamples =
        [
            new(0, 0, 0),
            new(0, 1, 1),
            new(1, 0, 1),
            new(1, 1, 0)
        ];
        SelectedTrainingSample = TrainingSamples[0];
        Breakpoints =
        [
            new() { Kind = TrainingBreakpointKind.LossBelow, Threshold = 0.10, IsEnabled = false },
            new() { Kind = TrainingBreakpointKind.GradientAbove, Threshold = 1.00, IsEnabled = false },
            new() { Kind = TrainingBreakpointKind.WeightAbove, Expression = "W(H1,O1)", Threshold = 2.00, IsEnabled = false },
            new() { Kind = TrainingBreakpointKind.EpochEquals, Threshold = 10, IsEnabled = false }
        ];

        RunCommand = new RelayCommand(Run);
        StepCommand = new RelayCommand(Step);
        ResetCommand = new RelayCommand(Reset);
        StepBackwardCommand = new RelayCommand(StepBackward);
        RunBackwardCommand = new RelayCommand(RunBackward);
        OptimizerStepCommand = new RelayCommand(OptimizerStep);
        TrainSampleCommand = new RelayCommand(TrainSample);
        TrainEpochCommand = new RelayCommand(TrainEpoch);
        LoadTrainingSampleCommand = new RelayCommand(LoadSelectedTrainingSample, () => SelectedTrainingSample is not null);
        ResetTrainingCommand = new RelayCommand(ResetTrainingHistory);
        AddBreakpointCommand = new RelayCommand(AddBreakpoint);
        RemoveBreakpointCommand = new RelayCommand(RemoveSelectedBreakpoint, () => SelectedBreakpoint is not null);
        CaptureSnapshotCommand = new RelayCommand(CaptureSnapshot);
        CompareSnapshotsCommand = new RelayCommand(CompareSnapshots, () => SnapshotFrom is not null && SnapshotTo is not null);
        RemoveSnapshotCommand = new RelayCommand(RemoveSelectedSnapshot, () => SelectedSnapshot is not null);
        AddWatchCommand = new RelayCommand(AddWatch);
        RemoveWatchCommand = new RelayCommand(RemoveSelectedWatch, () => SelectedWatch is not null);
        ApplyScriptCommand = new RelayCommand(ApplyScript);
        Reset();
        EvaluateDataset();
    }

    public NeuralNetwork Network { get; private set; }
    public IReadOnlyList<ForwardStep> Trace => Network.LastTrace;
    public ObservableCollection<BackwardStep> BackwardTrace { get; } = [];
    public ObservableCollection<WatchItem> Watches { get; }
    public ObservableCollection<TrainingSample> TrainingSamples { get; }
    public ObservableCollection<TrainingPoint> TrainingHistory { get; } = [];
    public ObservableCollection<TrainingBreakpoint> Breakpoints { get; }
    public ObservableCollection<ParameterSnapshot> Snapshots { get; } = [];
    public ObservableCollection<ParameterDelta> SnapshotComparison { get; } = [];

    private TrainingBreakpoint? _selectedBreakpoint;
    public TrainingBreakpoint? SelectedBreakpoint
    {
        get => _selectedBreakpoint;
        set { if (SetField(ref _selectedBreakpoint, value) && RemoveBreakpointCommand is RelayCommand c) c.RaiseCanExecuteChanged(); }
    }

    private ParameterSnapshot? _selectedSnapshot;
    private ParameterSnapshot? _snapshotFrom;
    private ParameterSnapshot? _snapshotTo;
    public ParameterSnapshot? SelectedSnapshot
    {
        get => _selectedSnapshot;
        set { if (SetField(ref _selectedSnapshot, value) && RemoveSnapshotCommand is RelayCommand c) c.RaiseCanExecuteChanged(); }
    }
    public ParameterSnapshot? SnapshotFrom
    {
        get => _snapshotFrom;
        set { if (SetField(ref _snapshotFrom, value) && CompareSnapshotsCommand is RelayCommand c) c.RaiseCanExecuteChanged(); }
    }
    public ParameterSnapshot? SnapshotTo
    {
        get => _snapshotTo;
        set { if (SetField(ref _snapshotTo, value) && CompareSnapshotsCommand is RelayCommand c) c.RaiseCanExecuteChanged(); }
    }
    public string SnapshotStatus => SnapshotComparison.Count == 0 ? "Capture two snapshots to compare parameters." :
        $"{SnapshotComparison.Count} parameters | total |delta| = {SnapshotComparison.Sum(x => x.AbsoluteDelta):0.######}";

    public string ScriptText { get => _scriptText; set => SetField(ref _scriptText, value); }
    public string ScriptStatus { get => _scriptStatus; private set => SetField(ref _scriptStatus, value); }

    public string OptimizerStatus { get => _optimizerStatus; private set => SetField(ref _optimizerStatus, value); }

    public string BreakpointStatus { get => _breakpointStatus; private set => SetField(ref _breakpointStatus, value); }

    private WatchItem? _selectedWatch;
    public WatchItem? SelectedWatch
    {
        get => _selectedWatch;
        set { if (SetField(ref _selectedWatch, value) && RemoveWatchCommand is RelayCommand c) c.RaiseCanExecuteChanged(); }
    }

    public TrainingSample? SelectedTrainingSample
    {
        get => _selectedTrainingSample;
        set
        {
            if (SetField(ref _selectedTrainingSample, value) && LoadTrainingSampleCommand is RelayCommand c)
                c.RaiseCanExecuteChanged();
        }
    }

    public int Epoch { get => _epoch; private set => SetField(ref _epoch, value); }
    public string TrainingStatus => $"Epoch {Epoch} | next sample {_sampleIndex + 1}/{TrainingSamples.Count}";
    public double X1 { get => _x1; set { if (SetField(ref _x1, value)) ResetExecution(true); } }
    public double X2 { get => _x2; set { if (SetField(ref _x2, value)) ResetExecution(true); } }
    public double Target { get => _target; set { if (SetField(ref _target, Math.Clamp(value, 0, 1))) ResetBackward(); } }
    public double LearningRate { get => _learningRate; set => SetField(ref _learningRate, Math.Max(0, value)); }

    public ForwardStep? CurrentStep { get => _currentStep; private set => SetField(ref _currentStep, value); }
    public BackwardStep? CurrentBackwardStep { get => _currentBackwardStep; private set => SetField(ref _currentBackwardStep, value); }

    public object? SelectedObject
    {
        get => _selectedObject;
        set { if (SetField(ref _selectedObject, value)) OnPropertyChanged(nameof(SelectionText)); }
    }

    public string SelectionText => SelectedObject switch
    {
        Connection c => $"Connection {c.From.Name} -> {c.To.Name} | W={c.Weight:0.####} | grad={(c.HasGradient ? c.Gradient.ToString("0.######") : "—")}",
        Neuron n => $"Neuron {n.Name} | Bias={n.Bias:0.####} | delta={(n.HasGradient ? n.Delta.ToString("0.######") : "—")}",
        _ => "Click a neuron or connection to inspect it."
    };

    public string StepStatus => CurrentBackwardStep is not null
        ? $"Backward {CurrentBackwardStep.Sequence}: {CurrentBackwardStep.Title}"
        : CurrentStep is null ? "Ready - press Step to begin"
        : _session?.IsCompleted == true ? $"Forward completed: {Trace.Count} steps"
        : $"Forward {CurrentStep.Sequence}: {CurrentStep.Title}";

    public string Formula => Network.Connections.Any(x => x.HasOptimizerUpdate)
        ? OptimizerStatus
        : CurrentBackwardStep?.Formula ?? CurrentStep?.Formula ?? "The network has not executed any operation yet.";
    public string OutputText => Network.Layers[^1].Neurons[0] is { HasValue: true } o ? o.Activation.ToString("0.000000") : "—";
    public string LossText => Network.Loss?.ToString("0.000000") ?? "—";

    public ICommand RunCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StepBackwardCommand { get; }
    public ICommand RunBackwardCommand { get; }
    public ICommand OptimizerStepCommand { get; }
    public ICommand TrainSampleCommand { get; }
    public ICommand TrainEpochCommand { get; }
    public ICommand LoadTrainingSampleCommand { get; }
    public ICommand ResetTrainingCommand { get; }
    public ICommand AddBreakpointCommand { get; }
    public ICommand RemoveBreakpointCommand { get; }
    public ICommand CaptureSnapshotCommand { get; }
    public ICommand CompareSnapshotsCommand { get; }
    public ICommand RemoveSnapshotCommand { get; }
    public ICommand AddWatchCommand { get; }
    public ICommand RemoveWatchCommand { get; }
    public ICommand ApplyScriptCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void ApplyScript()
    {
        try
        {
            var result = NeuralScriptParser.Parse(ScriptText);

            foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons))
                neuron.PropertyChanged -= ParameterChanged;
            foreach (var connection in Network.Connections)
                connection.PropertyChanged -= ParameterChanged;

            Network = result.Network;
            _watchEvaluator = new WatchEvaluator(Network);
            _breakpointEvaluator = new BreakpointEvaluator(Network);

            foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons))
                neuron.PropertyChanged += ParameterChanged;
            foreach (var connection in Network.Connections)
                connection.PropertyChanged += ParameterChanged;

            _x1 = result.X1;
            _x2 = result.X2;
            _target = result.Target;
            _learningRate = result.LearningRate;
            SelectedObject = null;
            _session = null;
            _backwardSession = null;
            CurrentStep = null;
            CurrentBackwardStep = null;
            BackwardTrace.Clear();
            TrainingHistory.Clear();
            Snapshots.Clear();
            SnapshotComparison.Clear();
            Epoch = 0;
            _sampleIndex = 0;

            OnPropertyChanged(nameof(Network));
            OnPropertyChanged(nameof(X1));
            OnPropertyChanged(nameof(X2));
            OnPropertyChanged(nameof(Target));
            OnPropertyChanged(nameof(LearningRate));
            OnPropertyChanged(nameof(Trace));
            OnPropertyChanged(nameof(TrainingStatus));

            RefreshComputed();
            ScriptStatus = $"Applied: {Network.Layers.Count} layers, {Network.Connections.Count} connections";
        }
        catch (Exception ex)
        {
            ScriptStatus = $"Error: {ex.Message}";
        }
    }

    private WatchItem CreateWatch(string expression) { var x = new WatchItem(expression); x.PropertyChanged += WatchChanged; return x; }
    private void AddWatch() { var x = CreateWatch("H1.Activation"); Watches.Add(x); SelectedWatch = x; RefreshWatches(); }
    private void RemoveSelectedWatch() { if (SelectedWatch is null) return; SelectedWatch.PropertyChanged -= WatchChanged; Watches.Remove(SelectedWatch); SelectedWatch = null; }
    private void WatchChanged(object? s, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(WatchItem.Expression) && s is WatchItem w) RefreshWatch(w); }
    private void RefreshWatches() { foreach (var w in Watches) RefreshWatch(w); }
    private void RefreshWatch(WatchItem w) { var r = _watchEvaluator.Evaluate(w.Expression); w.Value = r.Value; w.Error = r.Error; }

    private void ParameterChanged(object? sender, PropertyChangedEventArgs e) { ResetExecution(true); OnPropertyChanged(nameof(SelectionText)); }
    private void EnsureSession() => _session ??= Network.BeginForward(X1, X2);

    private void Run()
    {
        EnsureSession(); _session!.RunToEnd(); CurrentStep = _session.CurrentStep; CurrentBackwardStep = null; RefreshComputed();
    }

    private void Step()
    {
        if (_session?.IsCompleted == true) ResetExecution(true);
        EnsureSession(); CurrentStep = _session!.Step(); CurrentBackwardStep = null;
        if (SelectedObject is null) SelectedObject = (object?)CurrentStep?.Connection ?? CurrentStep?.Neuron;
        RefreshComputed();
    }

    private bool EnsureForwardCompleted()
    {
        if (_session?.IsCompleted == true) return true;
        Run();
        return _session?.IsCompleted == true;
    }

    private void EnsureBackward()
    {
        if (_backwardSession is not null) return;
        if (!EnsureForwardCompleted()) return;
        _backwardSession = Network.BeginBackward(Target);
        BackwardTrace.Clear();
    }

    private void StepBackward()
    {
        EnsureBackward();
        var step = _backwardSession?.Step();
        if (step is null) return;
        BackwardTrace.Add(step);
        CurrentBackwardStep = step;
        SelectedObject = (object?)step.Connection ?? step.Neuron;
        RefreshComputed();
    }

    private void RunBackward()
    {
        EnsureBackward();
        while (_backwardSession?.IsCompleted == false) StepBackward();
    }

    private void OptimizerStep()
    {
        EnsureBackward();
        if (_backwardSession?.IsCompleted == false) RunBackward();
        ApplyGradientsWithoutInvalidation();

        var updates = Network.Connections.Where(x => x.HasOptimizerUpdate).ToArray();
        var maxUpdate = updates.Select(x => Math.Abs(x.WeightUpdate)).DefaultIfEmpty(0).Max();
        OptimizerStatus = $"Optimizer applied | LR={LearningRate:0.######} | max |Δw|={maxUpdate:0.######}";
        CurrentBackwardStep = null;
        OnPropertyChanged(nameof(Formula));
        RefreshComputed();
    }

    private void TrainSample()
    {
        var currentIndex = _sampleIndex;
        var sample = TrainingSamples[currentIndex];
        TrainOne(sample);
        if (CheckBreakpoints(currentIndex))
        {
            LoadSampleIntoDebugger(sample);
            OnPropertyChanged(nameof(TrainingStatus));
            return;
        }
        _sampleIndex++;
        if (_sampleIndex >= TrainingSamples.Count)
        {
            _sampleIndex = 0;
            Epoch++;
            RecordEpochLoss();
        }
        EvaluateDataset();
        LoadSampleIntoDebugger(sample);
        OnPropertyChanged(nameof(TrainingStatus));
    }

    private void TrainEpoch()
    {
        for (var i = 0; i < TrainingSamples.Count; i++)
        {
            var sample = TrainingSamples[i];
            TrainOne(sample);
            _sampleIndex = i;
            if (CheckBreakpoints(i))
            {
                EvaluateDataset();
                LoadSampleIntoDebugger(sample);
                OnPropertyChanged(nameof(TrainingStatus));
                return;
            }
        }
        _sampleIndex = 0;
        Epoch++;
        EvaluateDataset();
        RecordEpochLoss();
        LoadSampleIntoDebugger(TrainingSamples[0]);
        OnPropertyChanged(nameof(TrainingStatus));
    }

    private bool CheckBreakpoints(int sampleIndex)
    {
        foreach (var bp in Breakpoints)
        {
            bp.Status = string.Empty;
            if (_breakpointEvaluator.IsMatch(bp, Epoch, sampleIndex, out var reason))
            {
                bp.Status = "HIT";
                BreakpointStatus = $"Breakpoint hit: {reason}";
                SelectedBreakpoint = bp;
                return true;
            }
        }
        BreakpointStatus = "No breakpoint hit";
        return false;
    }

    private void AddBreakpoint()
    {
        var bp = new TrainingBreakpoint { Kind = TrainingBreakpointKind.LossBelow, Threshold = 0.10 };
        Breakpoints.Add(bp);
        SelectedBreakpoint = bp;
    }

    private void RemoveSelectedBreakpoint()
    {
        if (SelectedBreakpoint is null) return;
        Breakpoints.Remove(SelectedBreakpoint);
        SelectedBreakpoint = null;
    }

    private void CaptureSnapshot()
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var connection in Network.Connections)
            values[$"W({connection.From.Name},{connection.To.Name})"] = connection.Weight;
        foreach (var neuron in Network.Layers.Skip(1).SelectMany(l => l.Neurons))
            values[$"B({neuron.Name})"] = neuron.Bias;

        var snapshot = new ParameterSnapshot
        {
            Name = $"S{++_snapshotNumber}",
            Epoch = Epoch,
            CreatedAt = DateTime.Now,
            Values = values
        };
        Snapshots.Add(snapshot);
        SelectedSnapshot = snapshot;
        if (SnapshotFrom is null) SnapshotFrom = snapshot;
        else SnapshotTo = snapshot;
        OnPropertyChanged(nameof(SnapshotStatus));
    }

    private void CompareSnapshots()
    {
        SnapshotComparison.Clear();
        if (SnapshotFrom is null || SnapshotTo is null) return;

        foreach (var parameter in SnapshotFrom.Values.Keys.Union(SnapshotTo.Values.Keys).OrderBy(x => x))
        {
            if (!SnapshotFrom.Values.TryGetValue(parameter, out var from) ||
                !SnapshotTo.Values.TryGetValue(parameter, out var to))
                continue;
            SnapshotComparison.Add(new ParameterDelta(parameter, from, to, to - from, Math.Abs(to - from)));
        }
        OnPropertyChanged(nameof(SnapshotStatus));
    }

    private void RemoveSelectedSnapshot()
    {
        if (SelectedSnapshot is null) return;
        var removed = SelectedSnapshot;
        Snapshots.Remove(removed);
        if (ReferenceEquals(SnapshotFrom, removed)) SnapshotFrom = null;
        if (ReferenceEquals(SnapshotTo, removed)) SnapshotTo = null;
        SelectedSnapshot = null;
        SnapshotComparison.Clear();
        OnPropertyChanged(nameof(SnapshotStatus));
    }

    private void TrainOne(TrainingSample sample)
    {
        var forward = Network.BeginForward(sample.X1, sample.X2);
        forward.RunToEnd();
        var backward = Network.BeginBackward(sample.Target);
        backward.RunToEnd();
        ApplyGradientsWithoutInvalidation();
    }

    private void ApplyGradientsWithoutInvalidation()
    {
        // Temporarily detach parameter notifications so SGD can update all parameters atomically.
        foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons)) neuron.PropertyChanged -= ParameterChanged;
        foreach (var connection in Network.Connections) connection.PropertyChanged -= ParameterChanged;
        try { Network.ApplyGradients(LearningRate); }
        finally
        {
            foreach (var neuron in Network.Layers.SelectMany(x => x.Neurons)) neuron.PropertyChanged += ParameterChanged;
            foreach (var connection in Network.Connections) connection.PropertyChanged += ParameterChanged;
        }
    }

    private void EvaluateDataset()
    {
        foreach (var sample in TrainingSamples)
        {
            var forward = Network.BeginForward(sample.X1, sample.X2);
            forward.RunToEnd();
            var prediction = Network.Layers[^1].Neurons[0].Activation;
            var p = Math.Clamp(prediction, 1e-12, 1 - 1e-12);
            sample.Prediction = prediction;
            sample.Loss = -(sample.Target * Math.Log(p) + (1 - sample.Target) * Math.Log(1 - p));
        }
        Network.ResetExecutionState();
        _session = null;
        _backwardSession = null;
        CurrentStep = null;
        CurrentBackwardStep = null;
        BackwardTrace.Clear();
        RefreshComputed();
    }

    private void RecordEpochLoss()
    {
        var average = TrainingSamples.Average(s => s.Loss ?? 0);
        TrainingHistory.Add(new TrainingPoint(Epoch, average));
    }

    private void LoadSelectedTrainingSample()
    {
        if (SelectedTrainingSample is not null) LoadSampleIntoDebugger(SelectedTrainingSample);
    }

    private void LoadSampleIntoDebugger(TrainingSample sample)
    {
        _x1 = sample.X1;
        _x2 = sample.X2;
        _target = sample.Target;
        OnPropertyChanged(nameof(X1)); OnPropertyChanged(nameof(X2)); OnPropertyChanged(nameof(Target));
        ResetExecution(true);
    }

    private void ResetTrainingHistory()
    {
        Epoch = 0;
        _sampleIndex = 0;
        TrainingHistory.Clear();
        BreakpointStatus = "No breakpoint hit";
        EvaluateDataset();
        OnPropertyChanged(nameof(TrainingStatus));
    }

    private void Reset() => ResetExecution(false);

    private void ResetBackward()
    {
        _backwardSession = null;
        BackwardTrace.Clear();
        CurrentBackwardStep = null;
        Network.ResetBackwardState();
        RefreshComputed();
    }

    private void ResetExecution(bool keepSelection)
    {
        Network.ResetExecutionState();
        _session = null; _backwardSession = null;
        CurrentStep = null; CurrentBackwardStep = null;
        BackwardTrace.Clear();
        if (!keepSelection) SelectedObject = null;
        RefreshComputed();
    }

    private void RefreshComputed()
    {
        RefreshWatches();
        OnPropertyChanged(nameof(Trace)); OnPropertyChanged(nameof(OutputText)); OnPropertyChanged(nameof(LossText));
        OnPropertyChanged(nameof(StepStatus)); OnPropertyChanged(nameof(Formula)); OnPropertyChanged(nameof(Network));
        OnPropertyChanged(nameof(SelectedObject)); OnPropertyChanged(nameof(SelectionText));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; OnPropertyChanged(propertyName); return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}