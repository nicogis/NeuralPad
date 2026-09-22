using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed record NeuralScriptResult(
    NeuralNetwork Network,
    IReadOnlyList<double> Inputs,
    double Target,
    double LearningRate);