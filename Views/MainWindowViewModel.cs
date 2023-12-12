using Avalonia.Controls;

namespace SmartTrainApplication.Views
{
    public class MainWindowViewModel : ViewModelBase
    {
        private UserControl _currentView = new TrackEditorView { DataContext = new TrackEditorViewModel() };
        private UserControl _bottomBar = new BottomBarView { DataContext = new BottomBarViewModel() };

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

        public UserControl BottomBar
        {
            get => _bottomBar;
            set
            {
                if (_bottomBar != value)
                {
                    _bottomBar = value;
                    RaisePropertyChanged(nameof(CurrentView));
                }
            }
        }

        public void NavigateToTrackEditor()
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

        public void NavigateToSettings()
        {
            CurrentView = new SettingsView { DataContext = new SettingsViewModel() };
        }
    }
}
