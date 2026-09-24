using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace NeuralPad.App.Scripting;

public sealed class NeuralCompletionData : ICompletionData
{
    public NeuralCompletionData(string text, string description, string? insertionText = null, double priority = 0)
    {
        Text = text;
        Description = description;
        _insertionText = insertionText ?? text;
        Priority = priority;
    }

    private readonly string _insertionText;

    public ImageSource? Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority { get; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, _insertionText);
    }
}