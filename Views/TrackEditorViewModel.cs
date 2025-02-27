#region

using System.Collections.Generic;
using System.Linq;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Views;

public class TrackEditorViewModel : ViewModelBase {
  public string TrackName { get; set; }
  public List<TrainRoute> Routes { get; set; }
  public List<RouteCoordinate> Stops { get; set; }

  private string _currentAction;

  public string CurrentAction {
    get => _currentAction;
    set {
      if (_currentAction != value) {
        _currentAction = value;
        RaisePropertyChanged(nameof(CurrentAction));
      }
    }
  }

  public bool AddingNew { get; set; }

  public TrackEditorViewModel() {
    if (DataManager.TrainRoutes.Count == 0)
      LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);

    Routes = DataManager.TrainRoutes.ToList();

    //TrackName = DataManager.GetCurrentRouteName().Name;
    //SetValuesToUI();
    TrackName = "";
    CurrentAction = "None";
    AddingNew = false;

    Stops = DataManager.GetStops();

    // Switch view in file manager
    FileManager.CurrentView = "Route";
  }

  public void AddLineButton() {
    LayerManager.ClearFeatures();
    LayerManager.AddLine();
    CurrentAction = "AddLine";
  }

  public void AddTunnelButton() {
    LayerManager.ClearFeatures();
    LayerManager.AddTunnel();
    CurrentAction = "AddTunnel";
  }

  public void AddStopButton() {
    LayerManager.ClearFeatures();
    LayerManager.AddTunnel();
    CurrentAction = "AddStop";
  }

  public void ModifyButton() {
    if (CurrentAction != "ModifyTrack") {
      LayerManager.ClearFeatures();
      LayerManager.TurnImportToEdit();
      CurrentAction = "ModifyTrack";
    }
  }

  public void ConfirmButton() {
    switch (CurrentAction) {
      case "ModifyTrack":
        LayerManager.ApplyEditing(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name,
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Id,
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].FilePath);
        DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Edited = true;
        break;

      case "AddLine":
        AddingNew = true;
        LayerManager.ClearAllLayers();
        LayerManager.ApplyEditing("Route " + (DataManager.TrainRoutes.Count + 1));
        Routes = DataManager.TrainRoutes.ToList();
        RaisePropertyChanged(nameof(Routes));
        AddingNew = false;
        DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Edited = true;
        break;

      case "AddStop":
        LayerManager.ConfirmStops();
        SetStopsToUI();
        DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Edited = true;
        break;

      case "AddTunnel":
        LayerManager.ConfirmTunnel();
        DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Edited = true;
        break;

      default:
        if (!DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name.Equals(TrackName)) {
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name = TrackName;
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Edited = true;
        }

        DataManager.SetStopsNames(Stops);
        Routes = DataManager.TrainRoutes.ToList();
        SetStopsToUI();
        RaisePropertyChanged(nameof(Routes));
        break;
    }

    CurrentAction = "None";
  }

  public void CancelButton() {
    CurrentAction = "None";
    LayerManager.ClearFeatures();
    LayerManager.ClearAllLayers();
    LayerManager.ChangeCurrentRoute(-1);
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
    Stops = DataManager.GetStops();
    RaisePropertyChanged(nameof(Stops));
  }

  public void DrawFocusedStop(string _Id) {
    var selectedStop = Stops.First(item => item.Id == _Id);
    LayerManager.AddFocusStop(selectedStop);
  }

  public void ChangeCurrentRouteIndex(int index) {
    LayerManager.ChangeCurrentRoute(index);
  }

  public void ImportButton() {
    LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);
  }
}