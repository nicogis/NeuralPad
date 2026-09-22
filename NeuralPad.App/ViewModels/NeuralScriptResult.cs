using NeuralPad.Core;

namespace NeuralPad.App.ViewModels;

public sealed record NeuralScriptResult(
    NeuralNetwork Network,
    double X1,
    double X2,
    double Target,
    double LearningRate);