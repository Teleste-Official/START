#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NLog;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Data;

/// <summary>
///   Holder for getting and setting trains and routes and functions used for adding or updating tunnels, stops, etc.
///   features to new or existing TrainRoutes
/// </summary>
internal class DataManager {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  public static List<TrainRoute> TrainRoutes = new();
  public static int CurrentTrainRoute = -1;

  public static List<Train> Trains = new();
  public static int CurrentTrain;

  private static List<string> _allIdentities = new();

  /// <summary>
  ///   Creates a new TrainRoute with a list of RouteCoordinates from a given GeometryString
  /// </summary>
  /// <param name="geometryString">(String) TrainRoute's GeometryString to be parsed</param>
  /// <param name="name">Name of the route to be added</param>
  /// <param name="id">id of the route to be added</param>
  /// <returns>(TrainRoute) TrainRoute with name & list of RouteCoordinates</returns>
  public static TrainRoute CreateNewRoute(string geometryString, string name = "Route", string id = "",
    string filePath = "") {
    Logger.Debug($"CreateNewRoute() geometryString={geometryString} name={name} id={id}");
    List<RouteCoordinate> geometry = ParseGeometryString(geometryString);
    TrainRoute newTrainRoute = new(name, geometry, id, filePath);

    return newTrainRoute;
  }

  public static TrainRoute? GetCurrentRoute() {
    if (CurrentTrainRoute == -1) {
      return null;
    } else if (TrainRoutes.Count - 1 < CurrentTrainRoute) {
      return null; 
    } else {
      return TrainRoutes[CurrentTrainRoute];
    }

  }

  public static Train? GetCurrentTrain() {
    if (CurrentTrain == -1) {
      return null;
    } else if (Trains.Count - 1 < CurrentTrain) {
      return null;
    } else {
      return Trains[CurrentTrain];
    }
  }
  
  //public static TrainRoute CreateTrainRouteFromGeometryString()

  /// <summary>
  ///   Adds the given TrainRoute to the list of TrainRoutes
  ///   <br />
  ///   Or If TrainRoute is already in the list, updates it's tunnels and stops
  /// </summary>
  /// <param name="newRoute">(TrainRoute) The TrainRoute to be added (or updated) to the list</param>
  public static void AddToRoutes(TrainRoute newRoute) {
    Logger.Debug($"AddToRoutes() name={newRoute.Name} id={newRoute.Id}");
    TrainRoute? oldRoute = null;

    for (int i = 0; i < TrainRoutes.Count; i++)
      if (TrainRoutes[i].Id == newRoute.Id) {
        Logger.Debug($"Updating route name={newRoute.Name} id={newRoute.Id}");
        oldRoute = TrainRoutes[i];
        TrainRoutes[i] = newRoute;
        CurrentTrainRoute = i;
        break;
      }

    if (oldRoute == null) {
      Logger.Debug("oldRoute is null, adding new route");
      // Add the new route to the list if no matching route is found
      TrainRoutes.Add(newRoute);
      CurrentTrainRoute = TrainRoutes.Count - 1;
    }
    else {
      foreach (RouteCoordinate t in oldRoute.Coords)
        if (t.Type == "STOP" || t.Type == "TUNNEL_STOP" ||
            t.Type == "TUNNEL_ENTRANCE_STOP")
          for (int j = 0; j < TrainRoutes[CurrentTrainRoute].Coords.Count; j++)
            if (TrainRoutes[CurrentTrainRoute].Coords[j].Latitude == t.Latitude &&
                TrainRoutes[CurrentTrainRoute].Coords[j].Longitude == t.Longitude) {
              TrainRoutes[CurrentTrainRoute].Coords.RemoveAt(j);
              TrainRoutes[CurrentTrainRoute].Coords.Insert(j, t);
              break;
            }
    }

    List<string> tunnelPoints = GetTunnelPoints();
    //List<string> stopPoints = GetStopStrings();
    //LayerManager.RedrawStopsToMap(stopPoints);
    LayerManager.RedrawTunnelsToMap(tunnelPoints);
    LayerManager.RedrawStopsToMap(DataManager.TrainRoutes[CurrentTrainRoute].GetStopCoordinates());
    
  }

  /// <summary>
  ///   Parses the given GeometryString to a list of RouteCoordinates
  /// </summary>
  /// <param name="geometryString">(String) TrainRoute's GeometryString to be parsed </param>
  /// <returns>(List of RoudeCoordinate(string X, string Y)) TrainRoute's coordinates</returns>
  public static List<RouteCoordinate> ParseGeometryString(string geometryString) {
    Logger.Debug($"ParseGeometryString() geometryString={geometryString}");
    // Parse the line string into individual values
    string[] parsedGeometry = geometryString.Split("(");
    string geometry = parsedGeometry[1].Remove(parsedGeometry[1].Length - 1);
    string[] individualCoords = geometry.Split(",");

    // Create a list of coordinate values from the parsed string
    List<RouteCoordinate> coordinates = new();
    for (int i = 0; i < individualCoords.Length; i++) {
      string[] xy = individualCoords[i].Split(" ");
      if (i == 0)
        coordinates.Add(new RouteCoordinate(xy[0], xy[1]));
      else
        coordinates.Add(new RouteCoordinate(xy[1], xy[2]));
    }

    return coordinates;
  }

  /// <summary>
  ///   Adds tunnel data types to the given list of TunnelPoints
  /// </summary>
  /// <param name="tunnelPoints">(List of string) Points that contain Tunnels</param>
  /// <returns>(List of string) List of TunnelPoints with added tunnel data types</returns>
  public static List<string> AddTunnels(List<string> tunnelPoints) {
    List<string> tunnelStrings = new();
    if (TrainRoutes[CurrentTrainRoute] == null) return tunnelStrings;

    foreach (string pointString in tunnelPoints) {
      // Parse the line string into individual values
      string[] parsedGeometry = pointString.Split("(");
      string geometry = parsedGeometry[1].Remove(parsedGeometry[1].Length - 1);
      string[] xy = geometry.Split(" ");
      RoutePoint routePoint = new(xy[0], xy[1]);

      double closestDiff = 1000000;
      int pointStatusToBeChanged = -1;
      //var diffPoints = new List<double>();

      for (int i = 0; i < TrainRoutes[CurrentTrainRoute].Coords.Count; i++) {
        double diff = CalculateDistance(
          new RoutePoint(TrainRoutes[CurrentTrainRoute].Coords[i].Longitude,
            TrainRoutes[CurrentTrainRoute].Coords[i].Latitude), routePoint);
        //diffPoints.Add(diff);
        if (i == 0) {
          closestDiff = diff;
          pointStatusToBeChanged = i;
        }
        else {
          if (diff < closestDiff) {
            closestDiff = diff;
            pointStatusToBeChanged = i;
          }
        }
      }

      if (pointStatusToBeChanged != -1)
        if (TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].Type == "STOP")
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("TUNNEL_ENTRANCE_STOP");
        else
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("TUNNEL_ENTRANCE");
    }

    FixTunnelTypes();

    tunnelStrings = GetTunnelStrings();

    return tunnelStrings;
  }

  /// <summary>
  ///   Gets a list of Tunnel Entrance Points from the CurrentTrainRoute's coordinates
  /// </summary>
  /// <returns>(List of string) List of Tunnel Entrance Points gotten from CurrentTrainRoute.Coords</returns>
  private static List<string> GetTunnelPoints() {
    List<string> points = new();

    foreach (RouteCoordinate coord in TrainRoutes[CurrentTrainRoute].Coords)
      if (coord.Type == "TUNNEL_ENTRANCE")
        points.Add("POINT (" + coord.Longitude + " " + coord.Latitude + ")");

    return points;
  }

  /// <summary>
  ///   Parses the routes coordinates back into a mapsui linestring
  /// </summary>
  /// <returns>string "LINESTRING (...."</returns>
  public static string GetCurrentLinestring() {
    string geometryString = "LINESTRING (";

    foreach (RouteCoordinate coord in TrainRoutes[CurrentTrainRoute].Coords)
      geometryString += coord.Longitude + " " + coord.Latitude + ",";
    geometryString = geometryString.Remove(geometryString.Length - 1) + ")";

    return geometryString;
  }

  /// <summary>
  ///   Gets a list of TunnelStrings from the CurrentTrainRoute's coordinates
  /// </summary>
  /// <returns>(List of string) List of tunnelStrings (points that contain tunnels) gotten from CurrentTrainRoute.Coords</returns>
  public static List<string> GetTunnelStrings() {
    List<string> tunnelStrings = new();

    int entranceCount = 0;
    string geometryString = "LINESTRING (";
    foreach (RouteCoordinate coord in TrainRoutes[CurrentTrainRoute].Coords) {
      if (coord.Type == "TUNNEL" || coord.Type == "TUNNEL_ENTRANCE" || coord.Type == "TUNNEL_STOP" ||
          coord.Type == "TUNNEL_ENTRANCE_STOP")
        geometryString += coord.Longitude + " " + coord.Latitude + ",";

      if (coord.Type == "TUNNEL_ENTRANCE" || coord.Type == "TUNNEL_ENTRANCE_STOP")
        entranceCount++;

      if (entranceCount == 2) {
        geometryString = geometryString.Remove(geometryString.Length - 1) + ")";
        tunnelStrings.Add(geometryString);
        entranceCount = 0;
        geometryString = "LINESTRING (";
      }
    }
    //GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
    //tunnelStrings.Add(GeometryString);

    return tunnelStrings;
  }

  /// <summary>
  ///   Gets a list of StopPoints from the CurrentTrainRoute's coordinates
  /// </summary>
  /// <returns>(List of string) List of stopStrings (points that contain stops) gotten from CurrenTrainRoute.Coords</returns>
  public static List<string> GetStopStrings() {
    List<string> stopStrings = new();

    foreach (RouteCoordinate coord in TrainRoutes[CurrentTrainRoute].Coords)
      if (coord.Type == "STOP" || coord.Type == "TUNNEL_STOP" || coord.Type == "TUNNEL_ENTRANCE_STOP")
        stopStrings.Add("POINT (" + coord.Longitude + " " + coord.Latitude + ")");

    return stopStrings;
  }

  public static List<RouteCoordinate> GetStops() {
    if (CurrentTrainRoute == -1) {
      Logger.Debug("No routes");
      return new List<RouteCoordinate>();
    } else {
      return TrainRoutes[CurrentTrainRoute].GetStopCoordinates();
    }
  }
  
  /// <summary>
  ///   Calculates a distance between 2 given RoutePoints
  ///   <br />
  ///   Used to determine the closest RoutePoint to which to add the tunnel entrance when creating tunnels
  /// </summary>
  /// <param name="point1">(RoutePoint) RoutePoint from which to calculate the distance</param>
  /// <param name="point2">(RoutePoint) RoutePoint to which to calculate the distance</param>
  /// <returns>(double) Distance between the 2 given RoutePoints</returns>
  public static double CalculateDistance(RoutePoint point1, RoutePoint point2) {
    //Logger.Debug($"Calculating distance from p1({point1.Longitude};{point1.Latitude}) -> p2({point2.Longitude};{point2.Latitude})");
    double deltaX = double.Parse(point2.Longitude, NumberStyles.Float) -
                    double.Parse(point1.Longitude, NumberStyles.Float);
    double deltaY = double.Parse(point2.Latitude, NumberStyles.Float) -
                    double.Parse(point1.Latitude, NumberStyles.Float);

    return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
  }

  /// <summary>
  ///   Updates the data types of points in-between Tunnel Entrances to Tunnels in CurrentTrainRoute.Coords
  /// </summary>
  private static void FixTunnelTypes() {
    bool tunnelEntrance = false;
    foreach (RouteCoordinate coords in TrainRoutes[CurrentTrainRoute].Coords)
      if (coords.Type == "TUNNEL_ENTRANCE" || coords.Type == "TUNNEL_ENTRANCE_STOP") {
        if (!tunnelEntrance)
          tunnelEntrance = true;
        else
          tunnelEntrance = false;
      }
      else if (coords.Type == "NORMAL" && tunnelEntrance) {
        coords.SetType("TUNNEL");
      }
      else if (coords.Type == "STOP" && tunnelEntrance) {
        coords.SetType("TUNNEL_STOP");
      }
  }
  
  /// <summary>
  ///   Adds stop data types to the given list of StopsPoints
  /// </summary>
  /// <param name="routeCoordinates">(List of RouteCoordinates) Points that contain Stops</param>
  public static void AddStops(List<RouteCoordinate> routeCoordinates) {
    if (TrainRoutes[CurrentTrainRoute] == null) return;

    foreach (RouteCoordinate coord in routeCoordinates) {
      Logger.Debug($"Adding {coord.GetCoordinateString()}");
      RoutePoint routePoint = new(coord.Longitude, coord.Latitude);

      double closestDiff = 1000000;
      int pointStatusToBeChanged = -1;

      for (int i = 0; i < TrainRoutes[CurrentTrainRoute].Coords.Count; i++) {
        double diff = CalculateDistance(
          new RoutePoint(TrainRoutes[CurrentTrainRoute].Coords[i].Longitude, 
            TrainRoutes[CurrentTrainRoute].Coords[i].Latitude), routePoint);
        if (i == 0) {
          closestDiff = diff;
          pointStatusToBeChanged = i;
        }
        else {
          if (diff < closestDiff) {
            closestDiff = diff;
            pointStatusToBeChanged = i;
          }
        }
      }

      if (pointStatusToBeChanged != -1) {
        if (TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].Type == "TUNNEL") {
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("TUNNEL_STOP");
        } else if (TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].Type == "TUNNEL_ENTRANCE") {
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("TUNNEL_ENTRANCE_STOP");
        } else {
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("STOP");
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetName(coord.StopName);
        }

      }
    }

  }
  

  /// <summary>
  ///   Creates a unique identifier for trains and routes
  /// </summary>
  /// <returns>string "xxxx-xxxx-xxxx-xxxx"</returns>
  public static string CreateId() {
    string newId = RandomNumberGenerator.GetInt32(1000, 9999).ToString() + "-" +
                   RandomNumberGenerator.GetInt32(1000, 9999).ToString() + "-" +
                   RandomNumberGenerator.GetInt32(1000, 9999).ToString() + "-" +
                   RandomNumberGenerator.GetInt32(1000, 9999).ToString();
    if (_allIdentities.Count != 0)
      foreach (string existingId in _allIdentities)
        if (existingId.Contains(newId))
          newId = CreateId();

    _allIdentities.Add(newId);
    return newId;
  }

  /// <summary>
  ///   Creates a unique FilePath for trains and routes
  /// </summary>
  /// <param name="id">The routes or trains unique ID-number</param>
  /// <param name="specifier">Route/Train/Simulation</param>
  /// <returns>String, for example "C:/Start/Routes/export1234.json"</returns>
  public static string CreateFilePath(string id, string specifier, string name = "") {
    string newPath = "";
    
    if (specifier == "Route") {
      newPath = Path.Combine(FileManager.GetRouteDirectory(), name == "" ? specifier + id[..4] + ".json" : name + ".json");
      
    } else if (specifier == "Train") {
      newPath = Path.Combine(FileManager.GetTrainDirectory(), name == "" ? specifier + id[..4] + ".json" : name + ".json");
      
    } else if (specifier == "Simulation") {
      DateTime currentTime = DateTime.Now;
      newPath = Path.Combine(FileManager.GetSimulationDirectory(),
        "simulation_" + currentTime.ToString("dd_MM_yyyy__HH_mm_ss") + ".json");
    }
    
    Logger.Debug($"Created path for created path for id={id}, specifier={specifier}, name={name} -> path: \"{newPath}\"");
    return newPath;
  }


  /// <summary>
  ///   Updates the trains values from the UI
  /// </summary>
  public static void UpdateTrain(Train newTrain) {
    foreach (Train oldTrain in Trains)
      if (oldTrain.Id == newTrain.Id)
        oldTrain.SetValues(newTrain);
  }
}