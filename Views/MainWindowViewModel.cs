
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace SmartTrainApplication.Views
{
    public class MainWindowViewModel : ViewModelBase
    {
    private UserControl _currentView = new TrackEditorView { DataContext = new TrackEditorViewModel() };

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

    public void NavigateToTackEditor()
    { 
        CurrentView = new TrackEditorView { DataContext = new TrackEditorViewModel() };
    }

    public void NavigateToTrainEditor()
    {
        CurrentView = new TrainEditorView { DataContext = new TrainEditorViewModel() };
    }

    public void NavigateToSimulation()
    {
        CurrentView = new SimulationView { DataContext = new SimulationViewModel() };
    }


    }
}
