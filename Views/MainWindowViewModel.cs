
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace SmartTrainApplication.Views
{
    public class MainWindowViewModel : ViewModelBase
    {
    private UserControl _currentView = new SideBar1 { DataContext = new SideBarViewModel() };

        public UserControl CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView != value)
            {
                _currentView = value;
                RaisePropertyChanged(nameof(CurrentView));
            }
        }
    }

    public void NavigateToSidebar1()
    { 
        CurrentView = new SideBar1 { DataContext = new SideBarViewModel() };
    }

    public void NavigateToSidebar2()
    {
        CurrentView = new Sidebar2 { DataContext = new SideBar2ViewModel() };
    }

    public void NavigateToSidebar3()
    {
        CurrentView = new Sidebar3 { DataContext = new SideBar3ViewModel() };
    }


    }
}
