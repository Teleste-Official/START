﻿#region

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
  public static int CurrentTrainRoute;

  public static List<Train> Trains = new();
  public static int CurrentTrain;

  private static List<string> AllIdentities = new();

  /// <summary>
  ///   Creates a new TrainRoute with a list of RouteCoordinates from a given GeometryString
  /// </summary>
  /// <param name="GeometryString">(String) TrainRoute's GeometryString to be parsed</param>
  /// <returns>(TrainRoute) TrainRoute with name & list of RouteCoordinates</returns>
  public static TrainRoute CreateNewRoute(string GeometryString, string Name = "Route", string ID = "",
    string FilePath = "") {
    Logger.Debug($"CreateNewRoute() name={Name} id={ID}");
    var Geometry = ParseGeometryString(GeometryString);
    var NewTrainRoute = new TrainRoute(Name, Geometry, ID, FilePath);

    return NewTrainRoute;
  }

  /// <summary>
  ///   Adds the given TrainRoute to the list of TrainRoutes
  ///   <br />
  ///   Or If TrainRoute is already in the list, updates it's tunnels and stops
  /// </summary>
  /// <param name="NewRoute">(TrainRoute) The TrainRoute to be added (or updated) to the list</param>
  public static void AddToRoutes(TrainRoute NewRoute) {
    Logger.Debug($"AddToRoutes() name={NewRoute.Name} id={NewRoute.Id}");
    TrainRoute? oldRoute = null;

    for (var i = 0; i < TrainRoutes.Count; i++)
      if (TrainRoutes[i].Id == NewRoute.Id) {
        Logger.Debug($"Updating route name={NewRoute.Name} id={NewRoute.Id}");
        oldRoute = TrainRoutes[i];
        TrainRoutes[i] = NewRoute;
        CurrentTrainRoute = i;
        break;
      }

    if (oldRoute == null) {
      Logger.Debug("oldRoute is null, adding new route");
      // Add the new route to the list if no matching route is found
      TrainRoutes.Add(NewRoute);
      CurrentTrainRoute = TrainRoutes.Count - 1;
    }
    else {
      foreach (var t in oldRoute.Coords)
        if (t.Type == "STOP" || t.Type == "TUNNEL_STOP" ||
            t.Type == "TUNNEL_ENTRANCE_STOP")
          for (var j = 0; j < TrainRoutes[CurrentTrainRoute].Coords.Count; j++)
            if (TrainRoutes[CurrentTrainRoute].Coords[j].Latitude == t.Latitude &&
                TrainRoutes[CurrentTrainRoute].Coords[j].Longitude == t.Longitude) {
              TrainRoutes[CurrentTrainRoute].Coords.RemoveAt(j);
              TrainRoutes[CurrentTrainRoute].Coords.Insert(j, t);
              break;
            }
    }

    var tunnelPoints = GetTunnelPoints();
    //List<string> stopPoints = GetStopStrings();
    //LayerManager.RedrawStopsToMap(stopPoints);
    LayerManager.RedrawTunnelsToMap(tunnelPoints);
    LayerManager.RedrawStopsToMap(DataManager.TrainRoutes[CurrentTrainRoute].GetStopCoordinates());
    
  }

  /// <summary>
  ///   Parses the given GeometryString to a list of RouteCoordinates
  /// </summary>
  /// <param name="GeometryString">(String) TrainRoute's GeometryString to be parsed </param>
  /// <returns>(List of RoudeCoordinate(string X, string Y)) TrainRoute's coordinates</returns>
  private static List<RouteCoordinate> ParseGeometryString(string GeometryString) {
    // Parse the line string into individual values
    var ParsedGeometry = GeometryString.Split("(");
    var Geometry = ParsedGeometry[1].Remove(ParsedGeometry[1].Length - 1);
    string[] IndividualCoords = Geometry.Split(",");

    // Create a list of coordinate values from the parsed string
    List<RouteCoordinate> Coordinates = new List<RouteCoordinate>();
    for (var i = 0; i < IndividualCoords.Length; i++) {
      var xy = IndividualCoords[i].Split(" ");
      if (i == 0)
        Coordinates.Add(new RouteCoordinate(xy[0], xy[1]));
      else
        Coordinates.Add(new RouteCoordinate(xy[1], xy[2]));
    }

    return Coordinates;
  }

  /// <summary>
  ///   Adds tunnel data types to the given list of TunnelPoints
  /// </summary>
  /// <param name="TunnelPoints">(List of string) Points that contain Tunnels</param>
  /// <returns>(List of string) List of TunnelPoints with added tunnel data types</returns>
  public static List<string> AddTunnels(List<string> TunnelPoints) {
    var tunnelStrings = new List<string>();
    if (TrainRoutes[CurrentTrainRoute] == null) return tunnelStrings;

    foreach (var PointString in TunnelPoints) {
      // Parse the line string into individual values
      var ParsedGeometry = PointString.Split("(");
      var Geometry = ParsedGeometry[1].Remove(ParsedGeometry[1].Length - 1);
      var xy = Geometry.Split(" ");
      var routePoint = new RoutePoint(xy[0], xy[1]);

      double closestDiff = 1000000;
      var pointStatusToBeChanged = -1;
      var diffpoints = new List<double>();

      for (var i = 0; i < TrainRoutes[CurrentTrainRoute].Coords.Count; i++) {
        var diff = CalculateDistance(
          new RoutePoint(TrainRoutes[CurrentTrainRoute].Coords[i].Longitude,
            TrainRoutes[CurrentTrainRoute].Coords[i].Latitude), routePoint);
        diffpoints.Add(diff);
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
    var points = new List<string>();

    foreach (var coord in TrainRoutes[CurrentTrainRoute].Coords)
      if (coord.Type == "TUNNEL_ENTRANCE")
        points.Add("POINT (" + coord.Longitude + " " + coord.Latitude + ")");

    return points;
  }

  /// <summary>
  ///   Parses the routes coordinates back into a mapsui linestring
  /// </summary>
  /// <returns>string "LINESTRING (...."</returns>
  public static string GetCurrentLinestring() {
    var GeometryString = "LINESTRING (";

    foreach (var coord in TrainRoutes[CurrentTrainRoute].Coords)
      GeometryString += coord.Longitude + " " + coord.Latitude + ",";
    GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";

    return GeometryString;
  }

  /// <summary>
  ///   Gets a list of TunnelStrings from the CurrentTrainRoute's coordinates
  /// </summary>
  /// <returns>(List of string) List of tunnelStrings (points that contain tunnels) gotten from CurrentTrainRoute.Coords</returns>
  public static List<string> GetTunnelStrings() {
    var tunnelStrings = new List<string>();

    var EntranceCount = 0;
    var GeometryString = "LINESTRING (";
    foreach (var coord in TrainRoutes[CurrentTrainRoute].Coords) {
      if (coord.Type == "TUNNEL" || coord.Type == "TUNNEL_ENTRANCE" || coord.Type == "TUNNEL_STOP" ||
          coord.Type == "TUNNEL_ENTRANCE_STOP")
        GeometryString += coord.Longitude + " " + coord.Latitude + ",";

      if (coord.Type == "TUNNEL_ENTRANCE" || coord.Type == "TUNNEL_ENTRANCE_STOP")
        EntranceCount++;

      if (EntranceCount == 2) {
        GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
        tunnelStrings.Add(GeometryString);
        EntranceCount = 0;
        GeometryString = "LINESTRING (";
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
    var stopStrings = new List<string>();

    foreach (var coord in TrainRoutes[CurrentTrainRoute].Coords)
      if (coord.Type == "STOP" || coord.Type == "TUNNEL_STOP" || coord.Type == "TUNNEL_ENTRANCE_STOP")
        stopStrings.Add("POINT (" + coord.Longitude + " " + coord.Latitude + ")");

    return stopStrings;
  }

  public static List<RouteCoordinate> GetStops() {
    var stopsCount = 1;
    var stops = new List<RouteCoordinate>();
    if (!TrainRoutes.Any()) return stops;
    foreach (var coord in TrainRoutes[CurrentTrainRoute].Coords)
      if (coord.Type == "STOP" || coord.Type == "TUNNEL_STOP" || coord.Type == "TUNNEL_ENTRANCE_STOP") {
        if (coord.Id == null)
          coord.Id = CreateID();
        if (coord.StopName == "")
          coord.StopName = "Stop " + stopsCount.ToString();
        stops.Add(coord);
        stopsCount++;
      }

    return stops;
  }

  public static void SetStopsNames(List<RouteCoordinate> stops) {
    foreach (var stop in stops)
      TrainRoutes[CurrentTrainRoute].Coords.First(item => item.Id == stop.Id).StopName = stop.StopName;
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
    var deltaX = double.Parse(point2.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture) -
                 double.Parse(point1.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
    var deltaY = double.Parse(point2.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture) -
                 double.Parse(point1.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);

    return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
  }

  /// <summary>
  ///   Updates the data types of points in-between Tunnel Entrances to Tunnels in CurrentTrainRoute.Coords
  /// </summary>
  private static void FixTunnelTypes() {
    var tunnelEntrance = false;
    foreach (var coords in TrainRoutes[CurrentTrainRoute].Coords)
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
  /// <param name="StopsPoints">(List of string) Points that contain Stops</param>
  /// <returns>(List of string) List of stopStrings with added stop data types</returns>
  public static List<string> AddStops(List<string> StopsPoints) {
    
    //TODO check if previous stop names are not handled correctly here!!!
    var stopStrings = new List<string>();
    if (TrainRoutes[CurrentTrainRoute] == null) return stopStrings;

    foreach (var PointString in StopsPoints) {
      // Parse the line string into individual values
      var ParsedGeometry = PointString.Split("(");
      var Geometry = ParsedGeometry[1].Remove(ParsedGeometry[1].Length - 1);
      var xy = Geometry.Split(" ");
      var routePoint = new RoutePoint(xy[0], xy[1]);

      double closestDiff = 1000000;
      var pointStatusToBeChanged = -1;
      var diffpoints = new List<double>();

      for (var i = 0; i < TrainRoutes[CurrentTrainRoute].Coords.Count; i++) {
        var diff = CalculateDistance(
          new RoutePoint(TrainRoutes[CurrentTrainRoute].Coords[i].Longitude,
            TrainRoutes[CurrentTrainRoute].Coords[i].Latitude), routePoint);
        diffpoints.Add(diff);
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
        if (TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].Type == "TUNNEL")
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("TUNNEL_STOP");
        else if (TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].Type == "TUNNEL_ENTRANCE")
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("TUNNEL_ENTRANCE_STOP");
        else
          TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetType("STOP");
        //TrainRoutes[CurrentTrainRoute].Coords[pointStatusToBeChanged].SetName("No name");
      }
    }


    stopStrings = GetStopStrings();

    return stopStrings;
  }

  /// <summary>
  ///   Creates a unique identifier for trains and routes
  /// </summary>
  /// <returns>string "xxxx-xxxx-xxxx-xxxx"</returns>
  public static string CreateID() {
    var NewID = RandomNumberGenerator.GetInt32(1000, 9999).ToString() + "-" +
                RandomNumberGenerator.GetInt32(1000, 9999).ToString() + "-" +
                RandomNumberGenerator.GetInt32(1000, 9999).ToString() + "-" +
                RandomNumberGenerator.GetInt32(1000, 9999).ToString();
    if (AllIdentities.Count != 0)
      foreach (var ExistingID in AllIdentities)
        if (ExistingID.Contains(NewID))
          NewID = CreateID();

    AllIdentities.Add(NewID);
    return NewID;
  }

  /// <summary>
  ///   Creates a unique FilePath for trains and routes
  /// </summary>
  /// <param name="Id">The routes or trains unique ID-number</param>
  /// <returns>String, for example "C:/Start/Routes/export1234.json"</returns>
  public static string CreateFilePath(string Id, string Specifier) {
    var NewPath = "";
    if (Specifier == "Route") {
      // Generate the file path and name the file export with last 4 digits of the id for unique name.
      NewPath = Path.Combine(FileManager.DefaultRouteFolderPath, Specifier + Id[..4] + ".json");
      Logger.Debug("created path: " + NewPath);
    }

    if (Specifier == "Train") {
      // Generate the file path and name the file export with last 4 digits of the id for unique name.
      NewPath = Path.Combine(FileManager.DefaultTrainFolderPath, Specifier + Id[..4] + ".json");
      Logger.Debug("created path: " + NewPath);
    }

    if (Specifier == "Simulation") {
      var currentTime = DateTime.Now;
      NewPath = Path.Combine(FileManager.DefaultSimulationsFolderPath,
        currentTime.ToString("ddMMyyyy_HHmmss") + ".json");
      Logger.Debug("created path: " + NewPath);
    }

    return NewPath;
  }

  /// <summary>
  ///   Updates the trains values from the UI
  /// </summary>
  public static void UpdateTrain(Train newTrain) {
    foreach (var oldTrain in Trains)
      if (oldTrain.Id == newTrain.Id)
        oldTrain.SetValues(newTrain);
  }
}