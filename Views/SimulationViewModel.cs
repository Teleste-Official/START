#region

using System.Collections.Generic;
using System.Linq;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using static SmartTrainApplication.Views.TrainEditorViewModel;

#endregion

namespace SmartTrainApplication.Views;

public class SimulationViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  public List<TrainRoute> Routes { get; set; }
  public List<ListedTrain> Trains { get; set; }
  public float Interval { get; set; }
  public Dictionary<RouteCoordinate, bool> StopsDictionary { get; set; }
  public List<bool> StopsBooleans { get; set; }
  public List<RouteCoordinate> Stops { get; set; }

  private bool _startSimulationButtonEnabled;
  private bool _stopSimulationButtonEnabled;

  public bool StartSimulationButtonEnabled {
    get => _startSimulationButtonEnabled;
    set {
      if (_startSimulationButtonEnabled != value) {
        _startSimulationButtonEnabled = value;
        RaisePropertyChanged(nameof(StartSimulationButtonEnabled));
      }
    }
  }

  public bool StopSimulationButtonEnabled {
    get => _stopSimulationButtonEnabled;
    set {
      if (_stopSimulationButtonEnabled != value) {
        _stopSimulationButtonEnabled = value;
        RaisePropertyChanged(nameof(StopSimulationButtonEnabled));
      }
    }
  }

  public SimulationViewModel() {
    // Get routes
    if (DataManager.TrainRoutes.Count == 0) {
      LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);
    }

    Routes = DataManager.TrainRoutes.ToList();

    // Get trains
    if (DataManager.Trains.Count == 0)
      DataManager.Trains = FileManager.StartupTrainFolderImport(SettingsManager.CurrentSettings.TrainDirectories);

    if (Icons == null)
      SetIcons();

    Trains = new List<ListedTrain>();
    Interval = 1.0f;
    SetTrainsToUI();

    Stops = DataManager.GetStops();
    StopsDictionary = Stops.ToDictionary(x => x, x => false);
    StopsBooleans = new List<bool>();

    // Switch view in file manager
    FileManager.CurrentView = "Simulation";
    Logger.Debug($"Current view: {FileManager.CurrentView}");
    LayerManager.ClearFocusedStopsLayer();

    if (DataManager.TrainRoutes.Any() && DataManager.Trains.Any()) {
      StartSimulationButtonEnabled = true;
    } else {
      StartSimulationButtonEnabled = false;
    }

  }

  public void StartSimulationButton() {
    StopSimulationButtonEnabled = true;
    StartSimulationButtonEnabled = false;

    if (DataManager.TrainRoutes.Any() && DataManager.Trains.Any()) {
      Train selectedTrain = DataManager.Trains[DataManager.CurrentTrain];
      Simulation.GenerateSimulationData(StopsDictionary, selectedTrain.Acceleration, selectedTrain.MaxSpeed, Interval);
      Simulation.StartAnimationPlayback();
    }
  }

  public void StopSimulationButton() {
    StartSimulationButtonEnabled = true;
    StopSimulationButtonEnabled = false;
    Simulation.StopAnimationPlayback();
  }

  public void SetTrainsToUI() {
    Trains.Clear();
    foreach (Train? Train in DataManager.Trains) Trains.Add(new ListedTrain(Train, Icons[Train.Icon]));
    Trains = Trains.ToList(); // This needs to be here for the UI to update on its own -Metso
  }

  public void SetStopsToUI() {
    Stops = DataManager.GetStops();
    StopsDictionary = Stops.ToDictionary(x => x, x => false);
    StopsBooleans = new List<bool>();
    RaisePropertyChanged(nameof(Stops));
  }

  public void SetBool(string _Id, bool value) {
    RouteCoordinate? selectedStop = Stops.First(item => item.Id == _Id);
    StopsDictionary[selectedStop] = value;
  }

  public void DrawFocusedStop(string _Id) {
    RouteCoordinate? selectedStop = Stops.First(item => item.Id == _Id);
    LayerManager.AddFocusStop(selectedStop);
  }

  public void UnDrawFocusedStop(string _Id) {
    RouteCoordinate? selectedStop = Stops.First(item => item.Id == _Id);
    LayerManager.RemoveFocusStop(selectedStop);
  }
}