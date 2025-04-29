#region

using System.Collections.Generic;
using System.Linq;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Views;

public class RouteEditorViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  //public string RouteName { get; set; }
  public List<TrainRoute> Routes { get; set; }
  public List<RouteCoordinate> Stops { get; set; }

  public enum EditorAction {
    None,
    AddRoute,
    AddTunnel,
    AddStop,
    ModifyRoute
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

  private string _routeName;

  public string RouteName {
    get => _routeName;
    set {
      if (_routeName != value) {
        _routeName = value;
        RaisePropertyChanged(nameof(RouteName));
      }

      if (_routeName == "")
        ConfirmButtonEnabled = false;
      else
        ConfirmButtonEnabled = true;
    }
  }

  private bool _routeComboBoxEnabled;
  private bool _routeNameFieldEnabled;
  private bool _addRouteButtonEnabled;
  private bool _addTunnelButtonEnabled;
  private bool _addStopButtonEnabled;
  private bool _modifyRouteButtonEnabled;
  private bool _stopListEnabled;
  private bool _confirmButtonEnabled;

  public bool RouteComboBoxEnabled {
    get => _routeComboBoxEnabled;
    set {
      if (_routeComboBoxEnabled != value) {
        _routeComboBoxEnabled = value;
        RaisePropertyChanged(nameof(RouteComboBoxEnabled));
      }
    }
  }

  public bool RouteNameFieldEnabled {
    get => _routeNameFieldEnabled;
    set {
      if (_routeNameFieldEnabled != value) {
        _routeNameFieldEnabled = value;
        RaisePropertyChanged(nameof(RouteNameFieldEnabled));
      }
    }
  }

  public bool AddRouteButtonEnabled {
    get => _addRouteButtonEnabled;
    set {
      if (_addRouteButtonEnabled != value) {
        _addRouteButtonEnabled = value;
        RaisePropertyChanged(nameof(AddRouteButtonEnabled));
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


  public bool ModifyRouteButtonEnabled {
    get => _modifyRouteButtonEnabled;
    set {
      if (_modifyRouteButtonEnabled != value) {
        _modifyRouteButtonEnabled = value;
        RaisePropertyChanged(nameof(ModifyRouteButtonEnabled));
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

  public bool ConfirmButtonEnabled {
    get => _confirmButtonEnabled;
    set {
      if (_confirmButtonEnabled != value) {
        _confirmButtonEnabled = value;
        RaisePropertyChanged(nameof(ConfirmButtonEnabled));
      }
    }
  }


  public bool AddingNew { get; set; }

  public RouteEditorViewModel() {
    Logger.Debug("RouteEditorViewModel");

    if (DataManager.TrainRoutes.Count == 0)
      LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);

    Routes = DataManager.TrainRoutes;

    RouteName = "";
    CurrentAction = EditorAction.None;

    ResetAllButtons();
    AddingNew = false;
    Stops = DataManager.GetStops();

    FileManager.CurrentView = "Route";
    LayerManager.ClearFocusedStopsLayer();
  }

  private void ResetAllButtons() {
    RouteComboBoxEnabled = true;
    RouteNameFieldEnabled = false;
    AddRouteButtonEnabled = true;
    if (DataManager.TrainRoutes.Count != 0) {
      AddTunnelButtonEnabled = true;
      AddStopButtonEnabled = true;
      ModifyRouteButtonEnabled = true;
      StopListEnabled = true;
      ConfirmButtonEnabled = false;
    }
  }

  public void AddRouteButton() {
    AddRouteButtonEnabled = false;
    RouteComboBoxEnabled = false;
    RouteNameFieldEnabled = true;
    AddStopButtonEnabled = false;
    AddTunnelButtonEnabled = false;
    ModifyRouteButtonEnabled = false;
    ConfirmButtonEnabled = true;
    StopListEnabled = false;
    RouteName = string.Empty;

    //LayerManager.ClearFeatures();
    LayerManager.ClearAllLayers();
    LayerManager.AddLine();
    CurrentAction = EditorAction.AddRoute;
  }

  public void AddTunnelButton() {
    DisableAllActionButtons();
    LayerManager.ClearFeatures();
    LayerManager.AddTunnel();
    CurrentAction = EditorAction.AddTunnel;
  }

  public void AddStopButton() {
    DisableAllActionButtons();
    LayerManager.ClearFeatures();
    LayerManager.AddTunnel();
    CurrentAction = EditorAction.AddStop;
  }

  public void ModifyButton() {
    if (CurrentAction != EditorAction.ModifyRoute) {
      DisableAllActionButtons();
      LayerManager.ClearFeatures();
      LayerManager.TurnImportToEdit();
      CurrentAction = EditorAction.ModifyRoute;
    }
  }

  public void CancelButton() {
    ResetAllButtons();
    CurrentAction = EditorAction.None;
    LayerManager.ClearFeatures();
    LayerManager.ClearAllLayers();
    LayerManager.ResetCurrentRoute();
  }

  private void DisableAllActionButtons() {
    AddRouteButtonEnabled = false;
    RouteNameFieldEnabled = false;
    ModifyRouteButtonEnabled = false;
    AddTunnelButtonEnabled = false;
    AddStopButtonEnabled = false;
    ConfirmButtonEnabled = true;
    RouteComboBoxEnabled = false;
    StopListEnabled = false;
  }

  public void ConfirmButton() {
    switch (CurrentAction) {
      case EditorAction.AddRoute:
        AddingNew = true;
        LayerManager.ClearAllLayers();
        //LayerManager.ApplyEditing("Route " + (DataManager.TrainRoutes.Count + 1));
        LayerManager.AddNewRoute(RouteName);

        Routes = DataManager.TrainRoutes.ToList();
        RaisePropertyChanged(nameof(DataManager.CurrentTrainRoute));
        RaisePropertyChanged(nameof(Routes));
        AddingNew = false;
        break;
      case EditorAction.ModifyRoute:
        LayerManager.ApplyEditing();
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
        if (RouteName != "" && !DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name.Equals(RouteName))
          DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name = RouteName;
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
    RouteName = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name;

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(RouteName));
  }

  public void UpdateRoutesToUI() {
    RouteName = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name;

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(RouteName));
    RaisePropertyChanged(nameof(Routes));
  }

  public void SetStopsToUI() {
    Stops = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates(); //DataManager.GetStops();
    RaisePropertyChanged(nameof(Stops));
  }

  public void DrawFocusedStop(string id) {
    RouteCoordinate selectedStop = Stops.First(item => item.Id == id);
    LayerManager.AddFocusStop(selectedStop);
  }

}