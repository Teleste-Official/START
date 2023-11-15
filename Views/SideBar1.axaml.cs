using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartTrainApplication.Views;

namespace SmartTrainApplication;

public partial class SideBar1 : UserControl
{
    public SideBar1()
    {
        InitializeComponent();
        DataContext = new SideBarViewModel();
    }
}