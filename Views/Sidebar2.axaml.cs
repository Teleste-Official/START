using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartTrainApplication.Views;

namespace SmartTrainApplication;

public partial class Sidebar2 : UserControl
{
    public Sidebar2()
    {
        InitializeComponent();
        DataContext = new SideBar2ViewModel();
    }
}