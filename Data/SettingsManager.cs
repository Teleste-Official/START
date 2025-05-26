#region

using System.Collections.Generic;
using System.IO;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Data;

internal class SettingsManager {
  // Default coordinates point to Tampere
  private static readonly string DATA_DIR = Path.Combine(Directory.GetCurrentDirectory(), "START-data");
  private static readonly double DEFAULT_LONGITUDE = 23.76227433384882;
  private static readonly double DEFAULT_LATITUDE = 61.49741016814548;

  public static Settings CurrentSettings;

  /// <summary>
  /// Generate new default settings -file and set it as the current one.
  /// </summary>
  public static void GenerateNewSettings() {


    List<string> newRouteDirectories = new() {
      Path.Combine(DATA_DIR, "Routes")
    };

    List<string> newTrainDirectories = new() {
      Path.Combine(DATA_DIR, "Trains")
    };

    List<string> newSimulationDirectories = new() {
      Path.Combine(DATA_DIR, "Simulations")
    };

    Settings? settings = new(DEFAULT_LONGITUDE, DEFAULT_LATITUDE, "", newRouteDirectories, newTrainDirectories, newSimulationDirectories);

    // Save the settings
    FileManager.SaveSettings(settings);
    CurrentSettings = settings;
  }
}