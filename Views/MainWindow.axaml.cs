using Avalonia.Controls;

namespace SmartTrainApplication
{
    public partial class MainWindow : Window
    {
        public static TopLevel TopLevel { get; set; }
        public MainWindow()
        {
            InitializeComponent();


            // TODO: move these to a seperate container/view -Metso
            var mapViewControl = new MapViewControl();
            var topLevel = GetTopLevel(this);
            TopLevel = topLevel;
            this.Content = mapViewControl;
        }
    }
}