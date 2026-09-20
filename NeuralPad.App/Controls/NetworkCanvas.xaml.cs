using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NeuralPad.Core;

namespace NeuralPad.App.Controls;

public partial class NetworkCanvas : UserControl
{
    public static readonly DependencyProperty NetworkProperty = DependencyProperty.Register(
        nameof(Network), typeof(NeuralNetwork), typeof(NetworkCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnVisualChanged));

    public static readonly DependencyProperty CurrentStepProperty = DependencyProperty.Register(
        nameof(CurrentStep), typeof(ForwardStep), typeof(NetworkCanvas),
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
            var selected = ReferenceEquals(SelectedObject, connection);
            var executed = connection.HasContribution;

            // Wide transparent hit target: thin visual weights remain easy to click.
            var hitLine = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = Brushes.Transparent,
                StrokeThickness = 14,
                Cursor = Cursors.Hand,
                Tag = connection
            };
            hitLine.MouseLeftButtonDown += SelectGraphObject;
            PART_Canvas.Children.Add(hitLine);

            var line = new Line
            {
                X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y,
                Stroke = selected ? Brushes.White : active ? Brushes.Gold :
                    !executed ? Brushes.DimGray :
                    connection.Weight >= 0 ? Brushes.DodgerBlue : Brushes.IndianRed,
                StrokeThickness = selected ? 6 : active ? 5 : 1.2 + Math.Min(4, Math.Abs(connection.Weight) * 3),
                Opacity = selected || active ? 1 : executed ? 0.75 : 0.25,
                IsHitTestVisible = false,
                ToolTip = executed
                    ? $"{connection}\nWeight = {connection.Weight:0.####}\nContribution = {connection.Contribution:0.####}"
                    : $"{connection}\nWeight = {connection.Weight:0.####}\nContribution = not executed"
            };
            PART_Canvas.Children.Add(line);
        }

        foreach (var (neuron, point) in positions)
        {
            var active = CurrentStep?.Neuron == neuron;
            var selected = ReferenceEquals(SelectedObject, neuron);
            var ellipse = new Ellipse
            {
                Width = 54, Height = 54,
                Fill = active ? Brushes.Gold : neuron.HasValue ? Brushes.SlateGray : Brushes.Black,
                Stroke = selected ? Brushes.White : active ? Brushes.Gold : neuron.HasValue ? Brushes.White : Brushes.DimGray,
                StrokeThickness = selected ? 5 : active ? 3 : 1.5,
                Cursor = Cursors.Hand,
                Tag = neuron,
                ToolTip = neuron.HasValue
                    ? $"{neuron.Name}\nBias = {neuron.Bias:0.####}\nZ = {neuron.Z:0.####}\nA = {neuron.Activation:0.####}"
                    : $"{neuron.Name}\nBias = {neuron.Bias:0.####}\nNot executed"
            };
            ellipse.MouseLeftButtonDown += SelectGraphObject;
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