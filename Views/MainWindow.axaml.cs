using Avalonia.Controls;

namespace SmartTrainApplication
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            // TODO: move these to a seperate container/view -Metso
            var mapViewControl = new MapViewControl();

            this.Content = mapViewControl;
        }
    }
}