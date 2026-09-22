using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NeuralPad.Core;
using WpfPoint = System.Windows.Point;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfCursors = System.Windows.Input.Cursors;

namespace NeuralPad.App.Controls;

public partial class NetworkCanvas : WpfUserControl
{
    public static readonly DependencyProperty NetworkProperty = DependencyProperty.Register(
        nameof(Network), typeof(NeuralNetwork), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged));

    public static readonly DependencyProperty CurrentStepProperty = DependencyProperty.Register(
        nameof(CurrentStep), typeof(ForwardStep), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged));

    public static readonly DependencyProperty CurrentBackwardStepProperty = DependencyProperty.Register(
        nameof(CurrentBackwardStep), typeof(BackwardStep), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged));

    public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(
        nameof(SelectedObject), typeof(object), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualChanged));

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

    public BackwardStep? CurrentBackwardStep
    {
        get => (BackwardStep?)GetValue(CurrentBackwardStepProperty);
        set => SetValue(CurrentBackwardStepProperty, value);
    }

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public NetworkCanvas()
    {
        InitializeComponent();
        SizeChanged += (_, _) => DrawNetwork();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NetworkCanvas)d).DrawNetwork();

    private void DrawNetwork()
    {
        if (PART_Canvas is null || Network is null || ActualWidth <= 0 || ActualHeight <= 0) return;

        PART_Canvas.Children.Clear();
        var width = Math.Max(300, ActualWidth - 40);
        var height = Math.Max(300, ActualHeight - 40);
        var positions = new Dictionary<Neuron, WpfPoint>();

        for (var l = 0; l < Network.Layers.Count; l++)
        {
            var layer = Network.Layers[l];
            var x = 55 + l * ((width - 110) / Math.Max(1, Network.Layers.Count - 1));
            for (var n = 0; n < layer.Neurons.Count; n++)
            {
                var y = (n + 1) * (height / (layer.Neurons.Count + 1));
                positions[layer.Neurons[n]] = new WpfPoint(x, y);
            }
        }

        foreach (var connection in Network.Connections)
        {
            var from = positions[connection.From];
            var to = positions[connection.To];
            var forwardActive = CurrentStep?.Connection == connection;
            var backwardActive = CurrentBackwardStep?.Connection == connection;
            var selected = ReferenceEquals(SelectedObject, connection);
            var executed = connection.HasContribution;

            var hitLine = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = WpfBrushes.Transparent,
                StrokeThickness = 14,
                Cursor = WpfCursors.Hand,
                Tag = connection
            };
            hitLine.MouseLeftButtonDown += SelectGraphObject;
            PART_Canvas.Children.Add(hitLine);

            var line = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = selected ? WpfBrushes.White :
                    backwardActive ? WpfBrushes.MediumPurple :
                    forwardActive ? WpfBrushes.Gold :
                    !executed ? WpfBrushes.DimGray :
                    connection.Weight >= 0 ? WpfBrushes.DodgerBlue : WpfBrushes.IndianRed,
                StrokeThickness = selected ? 6 : backwardActive || forwardActive ? 5 : 1.2 + Math.Min(4, Math.Abs(connection.Weight) * 3),
                Opacity = selected || backwardActive || forwardActive ? 1 : executed ? 0.75 : 0.25,
                IsHitTestVisible = false
            };
            PART_Canvas.Children.Add(line);

            if (backwardActive)
                DrawBackwardArrow(from, to);

            DrawConnectionLabel(connection, from, to);
        }

        foreach (var (neuron, point) in positions)
        {
            var forwardActive = CurrentStep?.Neuron == neuron;
            var backwardActive = CurrentBackwardStep?.Neuron == neuron;
            var selected = ReferenceEquals(SelectedObject, neuron);

            var ellipse = new Ellipse
            {
                Width = 58, Height = 58,
                Fill = backwardActive ? WpfBrushes.MediumPurple :
                    forwardActive ? WpfBrushes.Gold :
                    neuron.HasValue ? WpfBrushes.SlateGray : WpfBrushes.Black,
                Stroke = selected ? WpfBrushes.White :
                    backwardActive ? WpfBrushes.Plum :
                    forwardActive ? WpfBrushes.Gold :
                    neuron.HasValue ? WpfBrushes.White : WpfBrushes.DimGray,
                StrokeThickness = selected ? 5 : backwardActive || forwardActive ? 3 : 1.5,
                Cursor = WpfCursors.Hand,
                Tag = neuron,
                ToolTip = BuildNeuronTooltip(neuron)
            };
            ellipse.MouseLeftButtonDown += SelectGraphObject;
            Canvas.SetLeft(ellipse, point.X - 29);
            Canvas.SetTop(ellipse, point.Y - 29);
            PART_Canvas.Children.Add(ellipse);

            var valueText = neuron.HasValue ? neuron.Activation.ToString("0.###") : "—";
            var deltaText = neuron.HasGradient ? $"δ {neuron.Delta:0.###}" : "δ —";
            var label = new TextBlock
            {
                Text = CurrentBackwardStep is not null
                    ? $"{neuron.Name}\n{valueText}\n{deltaText}"
                    : $"{neuron.Name}\n{valueText}",
                Foreground = WpfBrushes.White,
                TextAlignment = TextAlignment.Center,
                Width = 82,
                IsHitTestVisible = false,
                FontSize = 11
            };
            Canvas.SetLeft(label, point.X - 41);
            Canvas.SetTop(label, point.Y - (CurrentBackwardStep is not null ? 24 : 16));
            PART_Canvas.Children.Add(label);
        }
    }

    private void DrawConnectionLabel(Connection connection, WpfPoint from, WpfPoint to)
    {
        if (!connection.HasContribution && !connection.HasGradient) return;

        var mid = new WpfPoint((from.X + to.X) / 2, (from.Y + to.Y) / 2);
        var parts = new List<string> { $"w {connection.Weight:0.###}" };
        if (connection.HasContribution) parts.Add($"xw {connection.Contribution:0.###}");
        if (connection.HasGradient) parts.Add($"dw {connection.Gradient:0.###}");
        if (connection.HasOptimizerUpdate)
            parts.Add($"{connection.WeightBeforeUpdate:0.###}→{connection.Weight:0.###} (Δ {connection.WeightUpdate:+0.###;-0.###;0})");

        var border = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(210, 24, 26, 32)),
            BorderBrush = connection.HasGradient ? WpfBrushes.MediumPurple : WpfBrushes.DimGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 2, 4, 2),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = string.Join(" | ", parts),
                Foreground = WpfBrushes.White,
                FontSize = 10
            }
        };

        Canvas.SetLeft(border, mid.X - 45);
        Canvas.SetTop(border, mid.Y - 12);
        PART_Canvas.Children.Add(border);
    }

    private void DrawBackwardArrow(WpfPoint from, WpfPoint to)
    {
        // Arrow head points from target back toward source.
        var dx = from.X - to.X;
        var dy = from.Y - to.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) return;

        var ux = dx / length;
        var uy = dy / length;
        var px = -uy;
        var py = ux;

        var center = new WpfPoint(
            to.X + dx * 0.35,
            to.Y + dy * 0.35);

        const double size = 10;
        var tip = new WpfPoint(center.X + ux * size, center.Y + uy * size);
        var left = new WpfPoint(center.X - ux * size * 0.6 + px * size * 0.7, center.Y - uy * size * 0.6 + py * size * 0.7);
        var right = new WpfPoint(center.X - ux * size * 0.6 - px * size * 0.7, center.Y - uy * size * 0.6 - py * size * 0.7);

        var arrow = new Polygon
        {
            Points = new PointCollection { tip, left, right },
            Fill = WpfBrushes.MediumPurple,
            Stroke = WpfBrushes.White,
            StrokeThickness = 1,
            IsHitTestVisible = false
        };
        PART_Canvas.Children.Add(arrow);
    }

    private static string BuildNeuronTooltip(Neuron neuron)
    {
        var lines = new List<string>
        {
            neuron.Name,
            $"Bias = {neuron.Bias:0.####}"
        };

        if (neuron.HasValue)
        {
            lines.Add($"Z = {neuron.Z:0.####}");
            lines.Add($"A = {neuron.Activation:0.####}");
        }
        else
            lines.Add("Forward: not executed");

        if (neuron.HasGradient)
        {
            lines.Add($"Delta = {neuron.Delta:0.######}");
            lines.Add($"dL/db = {neuron.BiasGradient:0.######}");
        }
        if (neuron.HasOptimizerUpdate)
        {
            lines.Add($"Bias before = {neuron.BiasBeforeUpdate:0.######}");
            lines.Add($"Δbias = {neuron.BiasUpdate:+0.######;-0.######;0}");
            lines.Add($"Bias after = {neuron.Bias:0.######}");
        }
        else
            lines.Add("Backward: not executed");

        return string.Join("\n", lines);
    }

    private void SelectGraphObject(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: not null } element)
        {
            SelectedObject = element.Tag;
            e.Handled = true;
        }
    }

    public void Refresh() => DrawNetwork();
}