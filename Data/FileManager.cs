#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using NLog;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Data;

/// <summary>
/// Functions used for saving and loading data to and from files
/// </summary>
internal class FileManager {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger(); 
  public static FilePickerFileType JSON { get; } = new("json") {
    Patterns = new[] { "*.json" },
    AppleUniformTypeIdentifiers = new[] { "public.json" },
    MimeTypes = new[] { "application/json" }
  };

  public static List<string>? ImportedRoutesAsStrings { get; set; }
  private static readonly string DEFAULT_ROUTE_DIR = Path.Combine(Directory.GetCurrentDirectory(), "Routes");
  private static readonly string DEFAULT_TRAIN_DIR = Path.Combine(Directory.GetCurrentDirectory(), "Trains");
  private static readonly string DEFAULT_SIMULATION_DIR = Path.Combine(Directory.GetCurrentDirectory(), "Simulations");

  // Currently active view
  public static string CurrentView = "";


  /// <summary>
  /// Export any of the JSON files with filepicker
  /// </summary>
  /// <param name="topLevel"></param>
  /// <param name="type">Route, Train & Simulation</param>
  public static async void Export(TopLevel topLevel, string type) {
    string nameSuggestion = "";

    switch (type) {
      case "Route":
        string? currentTrainRouteName = DataManager.GetCurrentRoute()?.Name;
        nameSuggestion = currentTrainRouteName != ""
          ? currentTrainRouteName?.Replace("_", "-").Replace(" ", "-") + ".json"
          : "export.json";
        break;

      case "Train":
        string? currentTrainName = DataManager.GetCurrentTrain()?.Name;
        nameSuggestion = currentTrainName != ""
          ? currentTrainName?.Replace("_", "-").Replace(" ", "-") + ".json"
          : "export.json";
        break;

      case "Simulation":
        nameSuggestion = GetSimulationFileName(Simulation.LatestSimulation);
        break;

      case "Settings":
        nameSuggestion = "settings.json";
        break;

      default:
        nameSuggestion = "export.json";
        break;
    }

    IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
      Title = "Export JSON",
      FileTypeChoices = new[] { JSON },
      SuggestedFileName = nameSuggestion
    });

    if (file == null) return;

    await using Stream stream = await file.OpenWriteAsync();
    using StreamWriter streamWriter = new(stream);

    JsonSerializerOptions Json_options = new() { WriteIndented = true };
    switch (type) {
      case "Route":
        if (DataManager.TrainRoutes.Any()) {
          string RouteOutput =
            JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options);
          await streamWriter.WriteLineAsync(RouteOutput);
        }

        break;

      case "Train":
        if (DataManager.Trains.Any()) {
          string TrainOutput = JsonSerializer.Serialize(DataManager.Trains[DataManager.CurrentTrain], Json_options);
          await streamWriter.WriteLineAsync(TrainOutput);
        }

        break;

      case "Simulation":
        if (Simulation.LatestSimulation != null) {
          string SimulationOutput = JsonSerializer.Serialize(Simulation.LatestSimulation, Json_options);
          await streamWriter.WriteLineAsync(SimulationOutput);
        }

        break;

      case "Settings":
        string SettingsOutput = JsonSerializer.Serialize(SettingsManager.CurrentSettings, Json_options);
        await streamWriter.WriteLineAsync(SettingsOutput);
        break;

      default:
        break;
    }
  }

  /// <summary>
  /// Imports the specified JSON as a string
  /// </summary>
  /// <param name="Path">Location of the file</param>
  /// <returns>JSON object as a string</returns>
  public static string ImportAsString(string Path) {
    string FileAsString = "";
    using (StreamReader sr = File.OpenText(Path)) {
      string S;
      while ((S = sr.ReadLine()) != null) FileAsString += S;
    }

    return FileAsString;
  }

  /// <summary>
  /// Saves DataManager.CurrentTrainRoute to "export.json" file
  /// </summary>
  [Obsolete]
  public static void Save() {
    if (DataManager.TrainRoutes[DataManager.CurrentTrainRoute] == null)
      return;

    string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");

    // Save the current train route
    JsonSerializerOptions Json_options = new() { WriteIndented = true };
    File.WriteAllText(Path,
      JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options));
  }

  public static void SaveSpecific(TrainRoute route) {
    if (route == null) return;

    string filePath = route.FilePath;
    
    // Save the current train route
    JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    File.WriteAllText(filePath, JsonSerializer.Serialize(route, jsonOptions));
    
    Logger.Debug($"Route \"{route.Name}\" saved to {filePath}");
  }

  /// <summary>
  /// Imports all Json-files from folders defined by user in settings view. Also sets current train route.
  /// </summary>
  /// <param name="SavedPaths">Takes the list of saved paths from settings</param>
  /// <returns>Returns available routes a list of strings</returns>
    public static List<TrainRoute> ReadRoutesFromFolder(List<string> savedPaths) {
    List<string> files = new();
    List<string> paths = new();

    //Create default folder if it doesn't exist
    try {
      if (!Directory.Exists(GetRouteDirectory())) Directory.CreateDirectory(GetRouteDirectory());
    } catch (Exception ex) {
      Logger.Debug(ex.Message);
    }


    try {
      foreach (string path in savedPaths) {
        Logger.Debug(path);
        if (Directory.Exists(path)) {
          IEnumerable<string> filesInFolder = Directory.EnumerateFiles(path, "*.json");

          foreach (string file in filesInFolder) {
            string fileAsString = "";
            using (StreamReader sr = File.OpenText(file)) {
              string s;
              while ((s = sr.ReadLine()) != null) fileAsString += s;
            }

            if (fileAsString.Contains("Coords")) {
              files.Add(fileAsString);
              paths.Add(file);
            }
          }
        }
      }
    } catch (Exception ex) {
      Logger.Error($"ReadRoutesFromFolder() error: {ex}");
    }


    // Deserialize the JSON strings into objects and add to list
    JsonSerializerOptions jsonOptions = new() { IncludeFields = true };
    
    List<TrainRoute> trainRoutes = new();
    for (int i = 0; i < files.Count; i++) {
      TrainRoute? importedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(files[i], jsonOptions);
      importedTrainRoute.FilePath = paths[i];
      importedTrainRoute.Id = DataManager.CreateId();
      trainRoutes.Add(importedTrainRoute);
    }

    if (trainRoutes.Count != 0) {
      DataManager.CurrentTrainRoute = 0;
    }
    
    return trainRoutes;
    }

  /// <summary>
  /// Changes currently active train route
  /// </summary>
  /// <param name="RouteIndex">Index number of wanted route</param>
  /// <returns>New active route</returns>
  public static string ChangeCurrentRoute(int RouteIndex) {
    if (RouteIndex == -1) return DataManager.GetCurrentLinestring();

    if (DataManager.TrainRoutes[RouteIndex] == null) {
      string FirstRoute = ImportedRoutesAsStrings[0];
      DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = DataManager.TrainRoutes[0];
      return FirstRoute;
    }

    string NewCurrentRoute = ImportedRoutesAsStrings[RouteIndex];
    DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = DataManager.TrainRoutes[RouteIndex];
    return NewCurrentRoute;
  }

  /// <summary>
  /// Saves route's simulation data to "simulation.json" file
  /// </summary>
  /// <param name="sim">(SimulationData) Route's simulation data</param>
  public static void SaveSimulationData(SimulationData sim) {

    string fileName = GetSimulationFileName(sim);

    string path = Path.Combine(GetSimulationDirectory(), fileName);


    try {
      if (!Directory.Exists(GetSimulationDirectory())) Directory.CreateDirectory(GetSimulationDirectory());
    }
    catch (Exception Ex) {
      Logger.Debug(Ex.Message);
      return;
    }

    // Save the simulation
    JsonSerializerOptions Json_options = new() { WriteIndented = true };
    File.WriteAllText(path, JsonSerializer.Serialize(sim, Json_options));
  }

  private static string GetSimulationFileName(SimulationData? sim) {
    if (sim == null) return "";

    string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    string simulatedRouteName = sim.TrainRoute.Name;
    //string simulatedTrainName = sim.Train.Name;
    string fileName = "";

    if (simulatedRouteName != "") {
      fileName += simulatedRouteName.Replace("_", "-").Replace(" ", "-");
    }

    //if (simulatedTrainName != "") {
    //  fileName += "_" + simulatedTrainName.Replace("_", "-").Replace(" ", "-");
    //}

    fileName += "_" + timeStamp + ".json";

    return fileName;
  }

  /// <summary>
  /// Imports all train Json-files from folders defined by user in settings view.
  /// </summary>
  /// <param name="SavedPaths">Takes the list of saved paths from settings</param>
  /// <returns>Returns imported trains</returns>
  public static List<Train> ReadTrainsFromFolder(List<string> SavedPaths) {
    List<Train> Trains = new();
    List<string> Paths = new();

    try {
      if (!Directory.Exists(GetTrainDirectory())) Directory.CreateDirectory(GetTrainDirectory());
    } catch (Exception ex) {
      Logger.Debug(ex.Message);
    }

    if (SavedPaths == null) return Trains;

    JsonSerializerOptions Json_options = new() { IncludeFields = true };

    try {
      foreach (string Path in SavedPaths) {
        Logger.Debug(Path);
        if (Directory.Exists(Path)) {
          IEnumerable<string> FilesInFolder = Directory.EnumerateFiles(Path, "*.json");

          foreach (string file in FilesInFolder) {
            string FileAsString = "";
            using (StreamReader sr = File.OpenText(file)) {
              string S;
              while ((S = sr.ReadLine()) != null) FileAsString += S;
            }

            if (FileAsString.Contains("MaxSpeed")) {
              Train? LoadedTrain = JsonSerializer.Deserialize<Train>(FileAsString, Json_options);
              LoadedTrain.FilePath = file;
              LoadedTrain.Id = DataManager.CreateId();
              Trains.Add(LoadedTrain);
            }
          }
        }
      }
    }
    catch (Exception Ex) {
      Logger.Debug(Ex.Message);
    }

    return Trains;
  }

  /// <summary>
  /// Saves DataManager.CurrentTrain to folder
  /// </summary>
  public static void SaveTrain(Train train) {
    string path = System.IO.Path.Combine(train.FilePath);

    try {
      if (!Directory.Exists(GetTrainDirectory())) Directory.CreateDirectory(GetTrainDirectory());
      // Save the train
      JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
      File.WriteAllText(path, JsonSerializer.Serialize(train, jsonOptions));
      Logger.Debug($"Train \"{train.Name}\" saved to {path}");
    }
    catch (Exception ex) {
      Logger.Error($"Trying to save train \"{train.Name}\" to \"{path}\", error: {ex}");
    }


  }

  public static void SaveSettings(Settings settings) {
    string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");

    // Save the settings
    JsonSerializerOptions Json_options = new() { WriteIndented = true };
    File.WriteAllText(Path, JsonSerializer.Serialize(settings, Json_options));
  }

  public static void LoadSettings() {
    string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");
    string FileAsString = "";

    // Check if the file exists
    if (!File.Exists(Path)) {
      SettingsManager.GenerateNewSettings();
      return;
    }

    // Open the file to read from
    using (StreamReader Sr = File.OpenText(Path)) {
      // Read the lines on the file and gather a list from them
      string S;
      while ((S = Sr.ReadLine()) != null) FileAsString += S;
    }

    // Deserialise the JSON string into a object
    JsonSerializerOptions Json_options = new() { IncludeFields = true };
    Settings? LoadedSettings = JsonSerializer.Deserialize<Settings>(FileAsString, Json_options);

    // Set the settings
    SettingsManager.CurrentSettings = LoadedSettings;
  }

  internal static void OpenGuide() {
    string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "StartManual.pdf");

    try {
      Process p = new();
      p.StartInfo = new ProcessStartInfo(Path) {
        UseShellExecute = true
      };
      p.Start();
    }
    catch (Exception ex) {
    }

    {
      Logger.Debug("No guide");
    }
  }

  public static async Task<string> OpenFolder(TopLevel topLevel, string title = "Choose folder") {
    string Path = "";
    IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
      Title = title
    });

    if (folder.Count > 0) Path = folder[0].Path.AbsolutePath;
    return Path;
  }

  public static string GetRouteDirectory() {
    List<string> currentRouteDirectories = SettingsManager.CurrentSettings.RouteDirectories;
    if (currentRouteDirectories.Count == 0) {
      return DEFAULT_ROUTE_DIR;
    }
    return currentRouteDirectories[0];

  }

  public static string GetTrainDirectory() {
    List<string> currentTrainDirectories = SettingsManager.CurrentSettings.TrainDirectories;
    if (currentTrainDirectories.Count == 0) {
      return DEFAULT_TRAIN_DIR;
    }
    return currentTrainDirectories[0];
  }

  public static string GetSimulationDirectory() {
    List<string> currentSimulationDirectories = SettingsManager.CurrentSettings.SimulationDirectories;
    if (currentSimulationDirectories.Count == 0) {
      return DEFAULT_SIMULATION_DIR;
    }
    return currentSimulationDirectories[0];
  }

}