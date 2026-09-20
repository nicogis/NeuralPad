using DevExpress.Xpf.Core;
using NeuralPad.App.ViewModels;

namespace NeuralPad.App;

public partial class MainWindow : ThemedWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}