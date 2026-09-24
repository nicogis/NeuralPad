using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace NeuralPad.App.Scripting;

public static class NeuralEditorLanguageService
{
    private static readonly NeuralCompletionData[] GlobalItems =
    [
        new("Neural", "NeuralPad network factory."),
        new("ReLU", "ReLU activation."),
        new("Sigmoid", "Sigmoid activation."),
        new("Tanh", "Tanh activation."),
        new("Linear", "Linear activation."),
        new("Dump(net)", "Send a ScriptNetwork to NeuralPad.", "Dump(net);")
    ];

    private static readonly NeuralCompletionData[] FactoryItems =
    [
        new("Network(int inputCount)", "Create a new NeuralPad network.", "Network()")
    ];

    private static readonly NeuralCompletionData[] NetworkItems =
    [
        new("Dense(int count, ActivationKind activation)", "Add a fully connected layer.", "Dense()"),
        new("Input(params double[] values)", "Set the debugger input vector.", "Input()"),
        new("Target(params double[] values)", "Set the debugger target vector.", "Target()"),
        new("LearningRate(double value)", "Set the optimizer learning rate.", "LearningRate()"),
        new("Sample(double[] inputs, double[] targets)", "Add a training sample.", "Sample([], [])"),
        new("Build()", "Materialize the ScriptNetwork as a NeuralNetwork.", "Build()")
    ];

    public static IReadOnlyList<NeuralCompletionData> GetCompletions(string code, int offset)
    {
        if (offset < 0 || offset > code.Length)
            return [];

        var prefix = code[..offset];
        var dotMatch = Regex.Match(prefix, @"(?<expr>[A-Za-z_][A-Za-z0-9_]*).s*$");
        if (dotMatch.Success)
        {
            var expression = dotMatch.Groups["expr"].Value;
            if (string.Equals(expression, "Neural", StringComparison.Ordinal))
                return FactoryItems;

            if (LooksLikeScriptNetworkVariable(code, expression))
                return NetworkItems;
        }

        return GlobalItems;
    }

    public static IReadOnlyList<string> GetDiagnostics(string code)
    {
        try
        {
            var options = CreateScriptOptions();
            var script = CSharpScript.Create(code, options, typeof(NeuralScriptGlobals));
            return script.Compile()
                .Where(x => x.Severity == DiagnosticSeverity.Error || x.Severity == DiagnosticSeverity.Warning)
                .Select(x => x.ToString())
                .ToArray();
        }
        catch (Exception ex)
        {
            return [ex.Message];
        }
    }

    internal static ScriptOptions CreateScriptOptions() =>
        ScriptOptions.Default
            .AddReferences(typeof(NeuralEditorLanguageService).Assembly, typeof(NeuralPad.Core.NeuralNetwork).Assembly)
            .AddImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "NeuralPad.Core",
                "NeuralPad.App.Scripting");

    private static bool LooksLikeScriptNetworkVariable(string code, string identifier)
    {
        var escaped = Regex.Escape(identifier);
        return Regex.IsMatch(
            code,
            $@"vars+{escaped}s*=s*Neural.Networks*(",
            RegexOptions.Multiline);
    }
}