using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NeuralPad.Core;

namespace NeuralPad.App.Controls;

public partial class NetworkCanvas : UserControl
{
    public static readonly DependencyProperty NetworkProperty = DependencyProperty.Register(
        nameof(Network), typeof(NeuralNetwork), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnNetworkChanged));

    public static readonly DependencyProperty CurrentStepProperty = DependencyProperty.Register(
        nameof(CurrentStep), typeof(ForwardStep), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnNetworkChanged));

    public NeuralNetwork? Network
    {
        get => (NeuralNetwork?)GetValue(NetworkProperty);
        set => SetValue(NetworkProperty, value);
    }

    public ForwardStep? CurrentStep
    {
        get => (ForwardStep?)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public NetworkCanvas()
    {
        InitializeComponent();
        SizeChanged += (_, _) => DrawNetwork();
    }

    private static void OnNetworkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NetworkCanvas)d).DrawNetwork();

    private void DrawNetwork()
    {
        if (PART_Canvas is null || Network is null || ActualWidth <= 0 || ActualHeight <= 0) return;

        PART_Canvas.Children.Clear();
        var width = Math.Max(300, ActualWidth - 40);
        var height = Math.Max(300, ActualHeight - 40);
        var positions = new Dictionary<Neuron, Point>();

        for (var l = 0; l < Network.Layers.Count; l++)
        {
            var layer = Network.Layers[l];
            var x = 55 + l * ((width - 110) / Math.Max(1, Network.Layers.Count - 1));
            for (var n = 0; n < layer.Neurons.Count; n++)
            {
                var y = (n + 1) * (height / (layer.Neurons.Count + 1));
                positions[layer.Neurons[n]] = new Point(x, y);
            }
        }

        foreach (var connection in Network.Connections)
        {
            var from = positions[connection.From];
            var to = positions[connection.To];
            var active = CurrentStep?.Connection == connection;
            var executed = connection.HasContribution;
            var line = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = active ? Brushes.Gold :
                    !executed ? Brushes.DimGray :
                    connection.Weight >= 0 ? Brushes.DodgerBlue : Brushes.IndianRed,
                StrokeThickness = active ? 5 : 1.2 + Math.Min(4, Math.Abs(connection.Weight) * 3),
                Opacity = active ? 1 : executed ? 0.75 : 0.25,
                ToolTip = executed
                    ? $"{connection}\nWeight = {connection.Weight:0.####}\nContribution = {connection.Contribution:0.####}"
                    : $"{connection}\nWeight = {connection.Weight:0.####}\nContribution = not executed"
            };
            PART_Canvas.Children.Add(line);
        }

        foreach (var (neuron, point) in positions)
        {
            var active = CurrentStep?.Neuron == neuron;
            var ellipse = new Ellipse
            {
                Width = 54, Height = 54,
                Fill = active ? Brushes.Gold : neuron.HasValue ? Brushes.SlateGray : Brushes.Black,
                Stroke = active ? Brushes.Gold : neuron.HasValue ? Brushes.White : Brushes.DimGray,
                StrokeThickness = active ? 3 : 1.5,
                ToolTip = neuron.HasValue
                    ? $"{neuron.Name}\nZ = {neuron.Z:0.####}\nA = {neuron.Activation:0.####}"
                    : $"{neuron.Name}\nNot executed"
            };
            Canvas.SetLeft(ellipse, point.X - 27);
            Canvas.SetTop(ellipse, point.Y - 27);
            PART_Canvas.Children.Add(ellipse);

            var label = new TextBlock
            {
                Text = $"{neuron.Name}\n{(neuron.HasValue ? neuron.Activation.ToString("0.###") : "—")}",
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Width = 70,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(label, point.X - 35);
            Canvas.SetTop(label, point.Y - 16);
            PART_Canvas.Children.Add(label);
        }
    }
}