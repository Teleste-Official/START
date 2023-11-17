
namespace SmartTrainApplication.Views
{
    public class MainWindowViewModel : ViewModelBase
    {
    private ViewModelBase _currentView;


    public ViewModelBase CurrentView
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
        CurrentView = new SideBarViewModel();
    }

    public void NavigateToSidebar2()
    {
        CurrentView = new SideBar2ViewModel();
    }

    public void NavigateToSidebar3()
    {
        CurrentView = new SideBar3ViewModel();
    }
    }
}
