using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Xpf.Core;
using ICSharpCode.AvalonEdit.CodeCompletion;
using NeuralPad.App.Scripting;
using NeuralPad.App.ViewModels;

namespace NeuralPad.App;

public partial class MainWindow : ThemedWindow
{
    private readonly DispatcherTimer _diagnosticsTimer;
    private CompletionWindow? _completionWindow;

    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainViewModel();
        DataContext = viewModel;

        CodeEditor.Text = viewModel.ScriptText;
        CodeEditor.TextChanged += (_, _) =>
        {
            viewModel.ScriptText = CodeEditor.Text;
            _diagnosticsTimer.Stop();
            _diagnosticsTimer.Start();
        };

        CodeEditor.TextArea.TextEntered += (_, e) =>
        {
            if (e.Text == ".")
                ShowCompletion();
        };

        CodeEditor.TextArea.TextEntering += (_, e) =>
        {
            if (_completionWindow is null || string.IsNullOrEmpty(e.Text))
                return;

            var ch = e.Text[0];
            if (!char.IsLetterOrDigit(ch) && ch != '_')
                _completionWindow.CompletionList.RequestInsertion(e);
        };

        CodeEditor.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ShowCompletion();
                e.Handled = true;
            }
        };

        _diagnosticsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _diagnosticsTimer.Tick += (_, _) =>
        {
            _diagnosticsTimer.Stop();
            RefreshDiagnostics();
        };

        RefreshDiagnostics();
    }

    private void ShowCompletion()
    {
        _completionWindow?.Close();

        var items = NeuralEditorLanguageService.GetCompletions(
            CodeEditor.Text,
            CodeEditor.CaretOffset);

        if (items.Count == 0)
            return;

        var window = new CompletionWindow(CodeEditor.TextArea);
        _completionWindow = window;

        foreach (var item in items)
            window.CompletionList.CompletionData.Add(item);

        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_completionWindow, window))
                _completionWindow = null;
        };

        window.Show();
    }

    private void RefreshDiagnostics()
    {
        var diagnostics = NeuralEditorLanguageService.GetDiagnostics(CodeEditor.Text);
        ScriptDiagnosticsText.Text = diagnostics.Count == 0
            ? "No C# diagnostics."
            : string.Join(Environment.NewLine, diagnostics.Take(6));
    }
}