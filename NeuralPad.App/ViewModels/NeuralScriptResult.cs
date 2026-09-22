using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed record NeuralScriptResult(
    NeuralNetwork Network,
    IReadOnlyList<double> Inputs,
    IReadOnlyList<double> Targets,
    double LearningRate);