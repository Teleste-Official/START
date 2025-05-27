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
  public int TickLength { get; set; }

  public float StopApproachSpeed { get; set; }
  public double SlowZoneLengthMeters { get; set; }

  // How close is considered "at stop"
  public double StopArrivalThresholdMeters { get; set; }

  public double DoorsOpenThreshold { get; set; }

  // How long the train will stop at a given platform/stop.
  public int TimeSpentAtStopSeconds { get; set; }



  // TBD if this should be configurable or calculated internally.
  //public int TickBufferAroundStops { get; set; }

  public Dictionary<RouteCoordinate, bool> StopsDictionary { get; set; }
  public List<bool> StopsBooleans { get; set; }
  public List<RouteCoordinate> Stops { get; set; }

  private bool _playSimulationButtonEnabled;
  private bool _stopSimulationButtonEnabled;
  private bool _createSimulationButtonEnabled;
  private bool _pauseSimulationButtonEnabled;

  private bool _simulationRunning;
  private bool _simulationPaused;

  private string _playSimulationButtonText = "Play";
  public string PlaySimulationButtonText {
    get => _playSimulationButtonText;
    set {
      if (_playSimulationButtonText != value) {
        _playSimulationButtonText = value;
        RaisePropertyChanged(nameof(PlaySimulationButtonText));
      }
    }
  }

  public bool PlaySimulationButtonEnabled {
    get => _playSimulationButtonEnabled;
    set {
      if (_playSimulationButtonEnabled != value) {
        _playSimulationButtonEnabled = value;
        RaisePropertyChanged(nameof(PlaySimulationButtonEnabled));
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

  public bool CreateSimulationButtonEnabled {
    get => _createSimulationButtonEnabled;
    set {
      if (_createSimulationButtonEnabled != value) {
        _createSimulationButtonEnabled = value;
        RaisePropertyChanged(nameof(CreateSimulationButtonEnabled));
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
      DataManager.Trains = FileManager.ReadTrainsFromFolder(SettingsManager.CurrentSettings.TrainDirectories);

    if (Icons == null)
      SetIcons();

    Trains = new List<ListedTrain>();
    TickLength = 1;
    StopArrivalThresholdMeters = 3;
    SlowZoneLengthMeters = 200;
    StopApproachSpeed = 20;
    DoorsOpenThreshold = 300;
    TimeSpentAtStopSeconds = 10;
    _simulationRunning = false;
    _simulationPaused = false;
    SetTrainsToUI();

    Stops = DataManager.GetStops();
    StopsDictionary = Stops.ToDictionary(x => x, x => false);
    StopsBooleans = new List<bool>();

    // Switch view in file manager
    FileManager.CurrentView = "Simulation";
    Logger.Debug($"Current view: {FileManager.CurrentView}");
    LayerManager.ClearFocusedStopsLayer();

    CreateSimulationButtonEnabled = true;
    PlaySimulationButtonEnabled = false;
  }

  public void PlaySimulationButton() {
    if (!_simulationPaused && !_simulationRunning) {
      if (Simulation.LatestSimulation != null) {
        _simulationRunning = true;
        Simulation.StartSimulationPlayback();
        StopSimulationButtonEnabled = true;
        PlaySimulationButtonText = "Pause";
        CreateSimulationButtonEnabled = false;
      }
    } else if (_simulationRunning) {
      PauseSimulation();
    } else {
      ResumeSimulation();
    }

  }

  public void PauseSimulation() {
    PlaySimulationButtonText = "Resume";
    _simulationPaused = true;
    _simulationRunning = false;
    Simulation.PauseSimulationPlayback();
  }

  public void ResumeSimulation() {
    PlaySimulationButtonText = "Pause";
    _simulationPaused = false;
    _simulationRunning = true;
    Simulation.ResumeSimulationPlayback();
  }

  public void StopSimulationButton() {
    PlaySimulationButtonEnabled = true;
    StopSimulationButtonEnabled = false;
    CreateSimulationButtonEnabled = true;
    _simulationRunning = false;
    _simulationPaused = false;
    PlaySimulationButtonText = "Play";
    Simulation.StopSimulationPlayback();
  }

  public void CreateSimulationButton() {

    if (DataManager.TrainRoutes.Any() && DataManager.Trains.Any()) {
      Train selectedTrain = DataManager.Trains[DataManager.CurrentTrain];
      TrainRoute selectedRoute = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
      SimulationData createdSimulation = Simulation.GenerateSimulationData(StopsDictionary, selectedTrain, selectedRoute, TickLength, StopApproachSpeed, SlowZoneLengthMeters, StopArrivalThresholdMeters, TimeSpentAtStopSeconds, DoorsOpenThreshold);

      FileManager.SaveSimulationData(createdSimulation);
      PlaySimulationButtonEnabled = true;
      StopSimulationButtonEnabled = false;
      CreateSimulationButtonEnabled = true;
      _simulationRunning = false;
      _simulationPaused = false;
      PlaySimulationButtonText = "Play";
    }
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