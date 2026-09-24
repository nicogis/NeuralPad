using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NeuralPad.App.ViewModels;

namespace NeuralPad.App.Scripting;

public static class RoslynScriptRunner
{
    public static async Task<NeuralScriptResult> RunAsync(string code)
    {
        ScriptNetwork? dumped = null;
        var globals = new NeuralScriptGlobals(network => dumped = network);

        var options = NeuralEditorLanguageService.CreateScriptOptions();

        await CSharpScript.RunAsync(code, options, globals, typeof(NeuralScriptGlobals));

        if (dumped is null)
            throw new InvalidOperationException("The script must call Dump(net) to send a network to NeuralPad.");

        return dumped.Build();
    }
}