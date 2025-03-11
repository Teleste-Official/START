#region

using System.Collections.Generic;
using System.Linq;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Views;

public class TrackEditorViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  //public string TrackName { get; set; }
  public List<TrainRoute> Routes { get; set; }
  public List<RouteCoordinate> Stops { get; set; }

  public enum EditorAction {
    None,
    AddLine,
    AddTunnel,
    AddStop,
    ModifyTrack
  }

  private EditorAction _currentAction;

  public EditorAction CurrentAction {
    get => _currentAction;
    set {
      if (_currentAction != value) {
        _currentAction = value;
        RaisePropertyChanged(nameof(CurrentAction));
      }
    }
  }
  
  private string _trackName;

  public string TrackName {
    get => _trackName;
    set {
      if (_trackName != value) {
        _trackName = value;
        RaisePropertyChanged(nameof(TrackName));
      }
    }
  }
  private bool _routeComboBoxEnabled;
  private bool _trackNameFieldEnabled;
  private bool _addLineButtonEnabled;
  private bool _addTunnelButtonEnabled;
  private bool _addStopButtonEnabled;
  private bool _modifyTrackButtonEnabled;
  private bool _stopListEnabled;
  
  public bool RouteComboBoxEnabled {
    get => _routeComboBoxEnabled;
    set {
      if (_routeComboBoxEnabled != value) {
        _routeComboBoxEnabled = value;
        RaisePropertyChanged(nameof(RouteComboBoxEnabled));
      }
    }
  }

  public bool TrackNameFieldEnabled {
    get => _trackNameFieldEnabled;
    set {
      if (_trackNameFieldEnabled != value) {
        _trackNameFieldEnabled = value;
        RaisePropertyChanged(nameof(TrackNameFieldEnabled));
      }
    }
  }

  public bool AddLineButtonEnabled {
    get => _addLineButtonEnabled;
    set {
      if (_addLineButtonEnabled != value) {
        _addLineButtonEnabled = value;
        RaisePropertyChanged(nameof(AddLineButtonEnabled));
      }
    }
  }

  public bool AddTunnelButtonEnabled {
    get => _addTunnelButtonEnabled;
    set {
      if (_addTunnelButtonEnabled != value) {
        _addTunnelButtonEnabled = value;
        RaisePropertyChanged(nameof(AddTunnelButtonEnabled));
      }
    }
  }

  public bool AddStopButtonEnabled {
    get => _addStopButtonEnabled;
    set {
      if (_addStopButtonEnabled != value) {
        _addStopButtonEnabled = value;
        RaisePropertyChanged(nameof(AddStopButtonEnabled));
      }
    }
  }
  
  
  public bool ModifyTrackButtonEnabled {
    get => _modifyTrackButtonEnabled;
    set {
      if (_modifyTrackButtonEnabled != value) {
        _modifyTrackButtonEnabled = value;
        RaisePropertyChanged(nameof(ModifyTrackButtonEnabled));
      }
    }
  }

  public bool StopListEnabled {
    get => _stopListEnabled;
    set {
      if (_stopListEnabled != value) {
        _stopListEnabled = value;
        RaisePropertyChanged(nameof(StopListEnabled));
      }
    }
  }
  

  public bool AddingNew { get; set; }

  public TrackEditorViewModel() {
    Logger.Debug("TrackEditorViewModel");
    if (DataManager.TrainRoutes.Count == 0) {
      LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);
    }
      

    Routes = DataManager.TrainRoutes;

    //TrackName = DataManager.GetCurrentRouteName().Name;
    //SetValuesToUI();
    TrackName = "";
    CurrentAction = EditorAction.None;
    
    ResetAllButtons();
    
    AddingNew = false;

    //Stops = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates();
    Stops = DataManager.GetStops();

    // Switch view in file manager
    FileManager.CurrentView = "Route";
  }

  private void ResetAllButtons() {
    RouteComboBoxEnabled = true;
    TrackNameFieldEnabled = false;
    
    AddLineButtonEnabled = true;
    AddTunnelButtonEnabled = true;
    AddStopButtonEnabled = true;
    ModifyTrackButtonEnabled = true;
    StopListEnabled = true;
  }

  public void AddLineButton() {
    AddLineButtonEnabled = false;
    RouteComboBoxEnabled = false;
    TrackNameFieldEnabled = true;
    AddStopButtonEnabled = false;
    AddTunnelButtonEnabled = false;
    ModifyTrackButtonEnabled = false;
    StopListEnabled = false;
    TrackName = string.Empty;
    
    //LayerManager.ClearFeatures();
    LayerManager.ClearAllLayers();
    LayerManager.AddLine();
    CurrentAction = EditorAction.AddLine;
  }

  public void AddTunnelButton() {
    TrackNameFieldEnabled = false;
    LayerManager.ClearFeatures();
    LayerManager.AddTunnel();
    CurrentAction = EditorAction.AddTunnel;
  }

  public void AddStopButton() {
    TrackNameFieldEnabled = false;
    LayerManager.ClearFeatures();
    LayerManager.AddTunnel();
    CurrentAction = EditorAction.AddStop;
  }

  public void ModifyButton() {
    if (CurrentAction != EditorAction.ModifyTrack) {
      TrackNameFieldEnabled = true;
      LayerManager.ClearFeatures();
      LayerManager.TurnImportToEdit();
      CurrentAction = EditorAction.ModifyTrack;
    }
    
  }
  
  public void CancelButton() {
    ResetAllButtons();
    CurrentAction = EditorAction.None;
    LayerManager.ClearFeatures();
    LayerManager.ClearAllLayers();
    LayerManager.ChangeCurrentRoute(-1); // TODO do something about this...
  }

  public void ConfirmButton() {
    switch (CurrentAction) {
      
      case EditorAction.AddLine:
        AddingNew = true;
        LayerManager.ClearAllLayers();
        //LayerManager.ApplyEditing("Route " + (DataManager.TrainRoutes.Count + 1));
        LayerManager.AddNewTrack(TrackName);
        
        Routes = DataManager.TrainRoutes.ToList();
        RaisePropertyChanged(nameof(DataManager.CurrentTrainRoute));
        RaisePropertyChanged(nameof(Routes));
        AddingNew = false;
        break;
      case EditorAction.ModifyTrack:
        string trackId = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Id;
        LayerManager.ApplyEditing(TrackName, trackId,
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].FilePath);
        SetStopsToUI();
        break;

      
      case EditorAction.AddStop:
        LayerManager.ConfirmStops();
        SetStopsToUI();
        break;

      case EditorAction.AddTunnel:
        LayerManager.ConfirmTunnel();
        break;
      
      case EditorAction.None: 
        if (TrackName != "" && !DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name.Equals(TrackName)) {
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name = TrackName;
        }
 
        DataManager.SetStopsNames(Stops); // This CAN produce unhandled exception when: Modify -> confirm -> confirm
        Routes = DataManager.TrainRoutes;
        SetStopsToUI();
        // If commented out, Fixes dropdown box resetting when pressing confirm after adding new line
        RaisePropertyChanged(nameof(Routes)); 
        return;
    }
    ResetAllButtons();
    CurrentAction = EditorAction.None;
  }



  public void SetValuesToUI() {
    TrackName = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name;

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(TrackName));
  }

  public void UpdateRoutesToUI() {
    TrackName = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name;

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(TrackName));
    RaisePropertyChanged(nameof(Routes));
  }

  public void SetStopsToUI() {
    Stops = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates();//DataManager.GetStops();
    RaisePropertyChanged(nameof(Stops));
  }

  public void DrawFocusedStop(string id) {
    RouteCoordinate selectedStop = Stops.First(item => item.Id == id);
    LayerManager.AddFocusStop(selectedStop);
  }
  
  /*
  public void DrawFocusedStop(RouteCoordinate selectedStop) {
    foreach (RouteCoordinate c in Stops) {
      // Could be better I know...
      if (c.Latitude == selectedStop.Latitude && c.Longitude == selectedStop.Longitude) {
        LayerManager.AddFocusStop(c);
        break;
      }
    }
    
    //var selectedStop = Stops.First(item => item.Id == _Id);
    //LayerManager.AddFocusStop(selectedStop);
  }*/
}