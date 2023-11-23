using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SmartTrainApplication.Views
{
    public partial class MainWindow : Window
    {
        public static TopLevel TopLevel { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            var topLevel = GetTopLevel(this);
            TopLevel = topLevel;
        }
    }
}