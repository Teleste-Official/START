#region

using System.Collections.Generic;
using System.Reflection;

#endregion

namespace SmartTrainApplication.Models;

/// <summary>
/// General application settings that are provided to the user AND version number
/// <list type="bullet">
/// <item>(string) Longitude (map's starting coordinates)</item>
/// <item>(string) Latitude (map's starting coordinates)</item>
/// <item>(List of string) RouteDirectories</item>
/// <item>(List of string) TrainDirectories</item>
/// <item>(string) VersionNumber</item>
/// </list>
/// </summary>
internal class Settings {
  public double Longitude { get; set; }
  public double Latitude { get; set; }
  public List<string> RouteDirectories { get; set; }
  public List<string> TrainDirectories { get; set; }

  public List<string> SimulationDirectories { get; set; }
  public string VersionNumber { get; set; }

  public Settings() {
  }

  public Settings(double longitude, double latitude, List<string> routeDirectories, List<string> trainDirectories, List<string> simulationDirectories) {
    Longitude = longitude;
    Latitude = latitude;
    RouteDirectories = routeDirectories;
    TrainDirectories = trainDirectories;
    SimulationDirectories = simulationDirectories;
    VersionNumber = Assembly.GetEntryAssembly().GetName().Version.ToString();
  }

  public void AddRouteDirectory(string routeDirectory) {
    //RouteDirectories.Add(routeDirectory);
    RouteDirectories.Clear();
    RouteDirectories.Add(routeDirectory);
  }

  public void AddTrainDirectory(string trainDirectory) {
    //TrainDirectories.Add(trainDirectory);
    TrainDirectories.Clear();
    TrainDirectories.Add(trainDirectory);
  }

  public void AddSimulationDirectory(string simulationDirectory) {
    //SimulationDirectories.Add(simulationDirectory);
    SimulationDirectories.Clear();
    SimulationDirectories.Add(simulationDirectory);
  }
}