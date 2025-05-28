#region

using Avalonia.Controls;
using NLog;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Views;

public class MainWindowViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  private UserControl _currentView = new RouteEditorView { DataContext = new RouteEditorViewModel() };
  private UserControl _bottomBar = new BottomBarView { DataContext = new BottomBarViewModel() };

  public UserControl CurrentView {
    get => _currentView;
    set {
      if (_currentView != value) {
        Logger.Trace($"Changing current view from {_currentView} to {value}");
        _currentView = value;
        RaisePropertyChanged(nameof(CurrentView));
      }
    }
  }

  public UserControl BottomBar {
    get => _bottomBar;
    set {
      if (_bottomBar != value) {
        _bottomBar = value;
        RaisePropertyChanged(nameof(CurrentView));
      }
    }
  }

  public void NavigateToRouteEditor() {
    CurrentView = new RouteEditorView { DataContext = new RouteEditorViewModel() };
  }

  public void NavigateToTrainEditor() {
    CurrentView = new TrainEditorView { DataContext = new TrainEditorViewModel() };
  }

  public void NavigateToSimulation() {
    CurrentView = new SimulationView { DataContext = new SimulationViewModel() };
  }

  public void NavigateToSettings() {
    CurrentView = new SettingsView { DataContext = new SettingsViewModel() };
  }

  public void GuideButton() {
    FileManager.OpenGuide();
  }
}