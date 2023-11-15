using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartTrainApplication.Views;

namespace SmartTrainApplication;

public partial class Sidebar3 : UserControl
{
    public Sidebar3()
    {
        InitializeComponent();
        DataContext = new SideBar3ViewModel();
    }
}