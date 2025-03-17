#region

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Layers.AnimatedLayers;
using Mapsui.Nts;
using Mapsui.Nts.Editing;
using Mapsui.Styles;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.MapComponents;
using SmartTrainApplication.Models;
using SmartTrainApplication.Views;

#endregion

namespace SmartTrainApplication;

internal class LayerManager {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  private static WritableLayer? _targetLayer =
    MapViewControl.map.Layers.FirstOrDefault(f => f.Name == "Layer 3") as WritableLayer;

  /// <summary>
  ///   Prepares the EditMode and features for adding lines/TrainRoutes
  /// </summary>
  public static void AddLine() {
    IEnumerable<IFeature> features = _targetLayer.GetFeatures().Copy() ?? Array.Empty<IFeature>();

    foreach (IFeature feature in features) feature.RenderedGeometry.Clear();

    MapViewControl._tempFeatures = new List<IFeature>(features);

    MapViewControl._editManager.EditMode = EditMode.AddLine;
  }

  /// <summary>
  ///   Clears the target layer and EditMode
  /// </summary>
  public static void ClearFeatures() {
    if (_targetLayer != null && MapViewControl._tempFeatures != null) {
      Logger.Debug("Clearing features...");
      _targetLayer.Clear();
      _targetLayer.AddRange(MapViewControl._tempFeatures.Copy());
      MapViewControl._mapControl?.RefreshGraphics();
    }

    MapViewControl._editManager.Layer?.Clear();

    MapViewControl._mapControl?.RefreshGraphics();

    MapViewControl._editManager.EditMode = EditMode.None;

    MapViewControl._tempFeatures = null;
  }

  public static void ClearAllLayers() {
    Logger.Debug("ClearAllLayers()");
    WritableLayer importLayer = CreateImportLayer();
    WritableLayer tunnelLayer = CreateTunnelLayer();
    WritableLayer tunnelStringLayer = CreateTunnelStringLayer();
    WritableLayer stopsLayer = CreateStopsLayer();
    WritableLayer focusedStopsLayer = CreateFocusStopsLayer();

    importLayer.Clear();
    tunnelLayer.Clear();
    tunnelStringLayer.Clear();
    stopsLayer.Clear();
    focusedStopsLayer.Clear();
    RemoveAnimationLayer();

    MapViewControl._mapControl?.RefreshGraphics();

    MapViewControl._editManager.EditMode = EditMode.None;

    MapViewControl._tempFeatures = null;
  }

  /// <summary>
  ///   Adds new Routes from imports to an import layer and redraws the map
  /// </summary>
  /// <param name="SavedPaths">Saved Paths</param>
  public static void ImportNewRoute(List<string> SavedPaths) {
    //var importedRoutes = FileManager.StartupFolderImport(SavedPaths); // After this, DataManager.trainroutes contains latest routes as actual objects.
    List<TrainRoute> trainRoutes = FileManager.ReadRoutesFromFolder(SavedPaths);
    DataManager.TrainRoutes = trainRoutes;
    
    try {
      
      List<string> tunnelStrings = DataManager.GetTunnelStrings();


      // TODO remove these
      /*
      TrainRoute currentRoute = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
      List<String> stopNames = currentRoute.Coords.Select(c => c.StopName).ToList();
      
      List<RouteCoordinate> stops = new List<RouteCoordinate>();
      foreach (RouteCoordinate coord in currentRoute.Coords) {
        if (coord.Type == "STOP") {
          stops.Add(coord);
          Logger.Debug("found stop " + coord.StopName);
        }
      }
      */
      
      WritableLayer importLayer = CreateImportLayer();
      SetTrainRouteToImportLayer(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], importLayer);
      RedrawTunnelsToMap(tunnelStrings);
      RedrawStopsToMap(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates());
      
      

    }
    catch (Exception Ex) {
      Logger.Debug(Ex);
    }
  }

  public static void ChangeCurrentRoute(int routeIndex) {
    string geometryData = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetGeometry();//FileManager.ChangeCurrentRoute(RouteIndex);

    WritableLayer importLayer = CreateImportLayer();
    List<string> tunnelStrings = DataManager.GetTunnelStrings();
    //List<string> stopsStrings = DataManager.GetStopStrings();

    TurnImportToFeature(geometryData, importLayer);
    RedrawTunnelsToMap(tunnelStrings);
    //RedrawStopsToMap2(DataManager.TrainRoutes[RouteIndex]);
    RedrawStopsToMap(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates());
  }

  /// <summary>
  ///   Adds the new Route to data, turns it to a feature and redraws the map
  /// </summary>
  public static void ConfirmNewRoute(string name = "Route", string id = "", string filePath = "") {
    Logger.Debug($"ConfirmNewRoute() name={name}, id={id}, filePath={filePath}");
    string routeString = GetRouteAsString();

    if (routeString == "")
      return;

    TrainRoute newRoute = DataManager.CreateNewRoute(routeString, name, id, filePath);
    DataManager.AddToRoutes(newRoute);

    WritableLayer importLayer = CreateImportLayer();
    TurnImportToFeature(routeString, importLayer);

    List<string> tunnelStrings = DataManager.GetTunnelStrings();
    RedrawTunnelsToMap(tunnelStrings);

    //List<string> stopStrings = DataManager.GetStopStrings();
    //RedrawStopsToMap(stopStrings);
    
    RedrawStopsToMap(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates());
  }

  /// <summary>
  ///   Gets the selected features/TrainRoutes as a string
  /// </summary>
  /// <returns>(string) Selected features</returns>
  private static string GetRouteAsString() {
    string? routeString = "";

    //(WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Stops");
    IEnumerable<IFeature>? selectedFeatures = MapViewControl._editManager.Layer?.GetFeatures();
    if (selectedFeatures.Any())
      foreach (IFeature selectedFeature in selectedFeatures) {
        GeometryFeature? testFeature = selectedFeature as GeometryFeature;

        // If there is multiple feature this overrides all others and only gets the frist one
        // Fix when routes can be named -Metso
        routeString = testFeature.Geometry.ToString();
        // Currently this deletes all features, from the edit layer -Metso
        //MapViewControl._editManager.Layer?.TryRemove(selectedFeature);
      }
    return routeString;
  }

  /// <summary>
  ///   Prepares the EditMode and features for adding tunnels
  /// </summary>
  public static void AddTunnel() {
    IEnumerable<IFeature> features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

    foreach (IFeature feature in features) feature.RenderedGeometry.Clear();

    MapViewControl._tempFeatures = new List<IFeature>(features);

    MapViewControl._editManager.EditMode = EditMode.AddPoint;
  }

  /// <summary>
  ///   Creates a new, if doesn't already exist, layer for imports
  /// </summary>
  /// <returns>(WritableLayer) Import layer</returns>
  public static WritableLayer CreateImportLayer() {
    WritableLayer? importLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Import");
    if (importLayer == null) {
      // Import layer doesnt exist yet, create the import layer
      MapViewControl.map.Layers.Add(MapViewControl.CreateImportLayer());
      importLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Import");
    }

    return importLayer;
  }

  /// <summary>
  ///   Creates a new, if doesn't already exist, layer for animations
  /// </summary>
  /// <returns>(AnimatedPointLayer) Animation layer</returns>
  public static AnimatedPointLayer CreateAnimationLayer() {
    AnimatedPointLayer? animationLayer = (AnimatedPointLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Playback");
    if (animationLayer == null) {
      // Animation layer doesnt exist yet, create the import layer
      MapViewControl.map.Layers.Add(new AnimatedPointLayer(new TrainPointProvider()) {
        Name = "Playback",
        Style = new VectorStyle {
          Fill = new Brush(Color.WhiteSmoke),
          Line = null,
          Outline = new Pen(Color.FromString("Blue"), 5)
        }
      });
      animationLayer = (AnimatedPointLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Playback");
    }

    return animationLayer;
  }

  /// <summary>
  ///   Removes the animation layer
  /// </summary>
  public static void RemoveAnimationLayer() {
    AnimatedPointLayer? animationLayer = (AnimatedPointLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Playback");
    if (animationLayer != null)
      MapViewControl.map.Layers.Remove(animationLayer);
  }

  /// <summary>
  ///   Copies the import layer feature, without tunnels or stops, to edit layer for editing
  /// </summary>
  /// <returns>(WritableLayer) Import layer</returns>
  public static WritableLayer TurnImportToEdit() {
    WritableLayer? importLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Import");
    if (importLayer != null) {
      // Throw the imported feature into edit layer for editing
      MapViewControl._editManager.Layer.AddRange(importLayer.GetFeatures().Copy());
      importLayer.Clear();
    }

    MapViewControl._editManager.EditMode = EditMode.Modify;
    MapViewControl._mapControl?.RefreshGraphics();

    return importLayer;
  }

  /// <summary>
  ///   Applies the edits to the TrainRoute and clears the edit layer
  /// </summary>
  public static void AddNewTrack(string name = "Route", string id = "", string filePath = "") {
    Logger.Debug($"AddNewTrack() name={name}, id={id}");
    ConfirmNewRoute(name, id, filePath);

    MapViewControl._editManager.Layer.Clear();
    MapViewControl._editManager.EditMode = EditMode.None;
    MapViewControl._mapControl?.RefreshGraphics();
  }
  
  
  public static void ApplyEditing() {
    
    List<RouteCoordinate> newCoords = GetEditLayerRouteCoordinates();

    if (newCoords.Count == 0) {
      Logger.Debug("EditLayer is empty, returning");
      return;
    }
    
    TrainRoute editedRoute = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
    editedRoute.Coords = newCoords;
    
    DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = editedRoute;
    
    List<RouteCoordinate> editLayerStopCoordinates = GetStopRouteCoordinates();
    
    DataManager.AddStops(editLayerStopCoordinates);
    
    WritableLayer importLayer = CreateImportLayer();
    SetTrainRouteToImportLayer(editedRoute, importLayer);
    
    
    List<string> tunnelStrings = DataManager.GetTunnelStrings();
    RedrawTunnelsToMap(tunnelStrings);
    RedrawStopsToMap(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates());
    MapViewControl._editManager.Layer.Clear();
    MapViewControl._editManager.EditMode = EditMode.None;
    MapViewControl._mapControl?.RefreshGraphics();
  }
  
  private static List<RouteCoordinate> GetEditLayerRouteCoordinates() {
    List<RouteCoordinate> routeCoordinates = new();
    
    IEnumerable<IFeature>? features = MapViewControl._editManager.Layer?.GetFeatures();
    foreach (IFeature feature in features) {
      if (feature is not GeometryFeature testFeature) {
        continue;
      }

      if (testFeature.Geometry is not LineString lineString) {
        continue;
      }

      foreach (Coordinate coordinate in lineString.Coordinates) {
        RouteCoordinate newCoord = new(coordinate.X.ToString(), coordinate.Y.ToString()) {
          Id = (testFeature["Coord"] as RouteCoordinate)?.Id ?? DataManager.CreateId(),
          Type = (testFeature["Coord"] as RouteCoordinate)?.Type ?? "NORMAL",
          StopName = (testFeature["Coord"] as RouteCoordinate)?.StopName ?? ""
        };
        
        routeCoordinates.Add(newCoord);
      }

    }
    return routeCoordinates;
  }
  
  public static List<RouteCoordinate> GetStopRouteCoordinates() {
    List<RouteCoordinate> stopRouteCoordinates = new();
    
    WritableLayer stopsLayer = CreateStopsLayer();
    stopsLayer.AddRange(MapViewControl._editManager.Layer.GetFeatures().Copy());
    
    IEnumerable<IFeature> features = stopsLayer.GetFeatures().Copy();
    
    foreach (IFeature feature in features) {
      if (feature is not GeometryFeature testFeature) {
        continue;
      }

      if (testFeature.Geometry is not Point point) {
        continue;
      }
      
      RouteCoordinate newCoord = new(point.X.ToString(), point.Y.ToString()) {
        Id = (testFeature["Coord"] as RouteCoordinate)?.Id ?? DataManager.CreateId(),
        Type = "STOP",
        StopName = (testFeature["Coord"] as RouteCoordinate)?.StopName ?? ""
      };
        
      stopRouteCoordinates.Add(newCoord);
    }
    
    return stopRouteCoordinates;
  }


  /// <summary>
  ///   Makes a new GeometryFeature from the given GeometryData and adds it to the given importLayer
  /// </summary>
  /// <param name="GeometryData">(string) Imported GeometryData</param>
  /// <param name="importLayer">(WritableLayer) The importLayer on which to add the import</param>
  public static void TurnImportToFeature(string GeometryData, WritableLayer importLayer) {
    Geometry? lineString = new WKTReader().Read(GeometryData);
    IFeature feature = new GeometryFeature { Geometry = lineString };
    // TODO ? feature["RouteName"] = "ASDF";
    feature["Route"] = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
    importLayer.Add(feature);
  }

  private static void SetTrainRouteToImportLayer(TrainRoute trainRoute, WritableLayer importLayer) {
    // TODO could be straight up geometry
    Geometry? lineString = new WKTReader().Read(trainRoute.GetGeometry());
    IFeature feature = new GeometryFeature { Geometry = lineString };
    feature["Route"] = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
    importLayer.Add(feature);
  }

  /// <summary>
  ///   Creates a new, if doesn't already exist, layer for tunnels
  /// </summary>
  /// <returns>(WritableLayer) Tunnel layer</returns>
  public static WritableLayer CreateTunnelLayer() {
    WritableLayer? tunnelLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnel");
    if (tunnelLayer == null) {
      // Tunnel layer doesnt exist yet, create the import layer
      MapViewControl.map.Layers.Add(MapViewControl.CreateTunnelLayer());
      tunnelLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnel");
    }

    return tunnelLayer;
  }

  /// <summary>
  ///   Creates a new, if doesn't already exist, layer for tunnel strings
  /// </summary>
  /// <returns>(WritableLayer) Tunnel string layer</returns>
  public static WritableLayer CreateTunnelStringLayer() {
    WritableLayer? tunnelStringLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnelstring");
    if (tunnelStringLayer == null) {
      // TunnelString layer doesnt exist yet, create the import layer
      MapViewControl.map.Layers.Add(MapViewControl.CreateTunnelstringLayer());
      tunnelStringLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnelstring");
    }

    return tunnelStringLayer;
  }

  /// <summary>
  ///   Creates a new, if doesn't already exist, layer for stops
  /// </summary>
  /// <returns>(WritableLayer) Stops layer</returns>
  public static WritableLayer CreateStopsLayer() {

    WritableLayer? stopsLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Stops");
    if (stopsLayer == null) {
      Logger.Debug("StopsLayer is null, creating new one");
      // TunnelString layer doesnt exist yet, create the import layer
      MapViewControl.map.Layers.Add(MapViewControl.CreateStopsLayer());
      stopsLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Stops");
    }

    return stopsLayer;
  }

  /// <summary>
  ///   Creates a new, if doesn't already exist, layer for focused stops
  /// </summary>
  /// <returns>(WritableLayer) Focused Stops layer</returns>
  public static WritableLayer CreateFocusStopsLayer() {
    WritableLayer? focusedStopsLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "FocusedStops");
    if (focusedStopsLayer == null) {
      // TunnelString layer doesnt exist yet, create the import layer
      MapViewControl.map.Layers.Add(MapViewControl.CreateFocusedStopsLayer());
      focusedStopsLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "FocusedStops");
    }

    return focusedStopsLayer;
  }

  /// <summary>
  ///   Takes the inputted tunnel points, lists them, adds them to data, (re)draws tunnels to map and clears the edit layer
  /// </summary>
  public static void ConfirmTunnel() {
    WritableLayer tunnelLayer = CreateTunnelLayer();
    WritableLayer tunnelstringLayer = CreateTunnelStringLayer();

    // Take created tunnel points
    tunnelLayer.AddRange(MapViewControl._editManager.Layer.GetFeatures().Copy());
    // Clear the editlayer
    MapViewControl._editManager.Layer?.Clear();

    // List of the tunnel points added
    List<string> tunnelPoints = new();

    IEnumerable<IFeature>? features = tunnelLayer?.GetFeatures().Copy();

    foreach (IFeature feature in features) {
      GeometryFeature? pointFeature = feature as GeometryFeature;

      string? point = pointFeature.Geometry.ToString();
      tunnelPoints.Add(point);
    }

    if (tunnelPoints.Count == 0)
      return;

    // Add tunnels to data
    List<string> tunnelStrings = DataManager.AddTunnels(tunnelPoints);

    RedrawTunnelsToMap(tunnelStrings);

    tunnelLayer.Clear();

    MapViewControl._mapControl?.RefreshGraphics();

    MapViewControl._editManager.EditMode = EditMode.None;

    MapViewControl._tempFeatures = null;
  }

  /// <summary>
  ///   Takes the inputted stop points, lists them, adds them to data, (re)draws stops to map and clears the edit layer
  /// </summary>
  public static void ConfirmStops() {
    List<RouteCoordinate> routeCoordinates = GetStopRouteCoordinates();
    MapViewControl._editManager.Layer?.Clear();
    
    DataManager.AddStops(routeCoordinates);
    
    RedrawStopsToMap(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates());
    MapViewControl._mapControl?.RefreshGraphics();
    MapViewControl._editManager.EditMode = EditMode.None;
    MapViewControl._tempFeatures = null;
  }

  /// <summary>
  ///   Draws the focused stop to the map
  /// </summary>
  public static void AddFocusStop(RouteCoordinate focusedStop) {
    WritableLayer focusedStopsLayer = CreateFocusStopsLayer();
    Geometry? pointString = new WKTReader().Read(focusedStop.GetCoordinateString());
    IFeature feature = new GeometryFeature { Geometry = pointString };
    focusedStopsLayer.Add(feature);

    MapViewControl._mapControl?.RefreshGraphics();
  }

  /// <summary>
  ///   Clears the focused stops layer
  /// </summary>
  public static void RemoveFocusStop() {
    WritableLayer focusedStopsLayer = CreateFocusStopsLayer();
    focusedStopsLayer.Clear();

    MapViewControl._mapControl?.RefreshGraphics();
  }

  public static void SwitchRoute() {
    ClearAllLayers();
    string geometryString = DataManager.GetCurrentLinestring();
    WritableLayer importLayer = CreateImportLayer();
    TurnImportToFeature(geometryString, importLayer);

    List<string> tunnelStrings = DataManager.GetTunnelStrings();
    RedrawTunnelsToMap(tunnelStrings);

    //List<string> stopStrings = DataManager.GetStopStrings();
    //RedrawStopsToMap(stopStrings);
    RedrawStopsToMap(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].GetStopCoordinates());
  }

  /// <summary>
  ///   (Re)draws the given list of tunnels to the map
  /// </summary>
  /// <param name="tunnelStrings">(List of string) The tunnel strings</param>
  public static void RedrawTunnelsToMap(List<string> tunnelStrings) {
    WritableLayer tunnelStringLayer = CreateTunnelStringLayer();
    tunnelStringLayer.Clear();
    foreach (string tunnelString in tunnelStrings) {
      Geometry? lineString = new WKTReader().Read(tunnelString);
      IFeature feature = new GeometryFeature { Geometry = lineString };
      tunnelStringLayer.Add(feature);
    }
  }
  
  /// <summary>
  ///   (Re)draws the given list of stops to the map
  /// </summary>
  /// <param name="coords">(List of RouteCoordinate) The stop coordinates</param>
  public static void RedrawStopsToMap(List<RouteCoordinate> coords) {
    WritableLayer stopsLayer = CreateStopsLayer();
    stopsLayer.Clear();
    
    if (coords.Count <= 0) return;
    foreach (RouteCoordinate stopCoordinate in coords) {
      Geometry? pointString = new WKTReader().Read(stopCoordinate.GetCoordinateString());
      IFeature feature = new GeometryFeature { Geometry = pointString };
      feature["Coord"] = stopCoordinate;
      stopsLayer.Add(feature);
    }

  }
}