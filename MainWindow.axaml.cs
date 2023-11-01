using Avalonia.Controls;

namespace SmartTrainApplication
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var mapViewControl = new MapViewControl();

            this.Content = mapViewControl;
        }
    }
}