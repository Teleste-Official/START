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
  public static string DefaultRouteFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Routes");
  public static string DefaultTrainFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Trains");
  public static string DefaultSimulationsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Simulations");

  // Currently active view
  public static string CurrentView = "";

  /// <summary>
  /// Export route into a file using filepicker.
  /// </summary>
  [Obsolete]
  public static async void ExportRoute(TopLevel topLevel) {
    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
      Title = "Export JSON",
      FileTypeChoices = new[] { JSON },
      SuggestedFileName = "export"
    });

    if (file == null) return;

    // Create a file and write empty the new route to it
    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    var output = JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options);
    if (file is not null) {
      await using var stream = await file.OpenWriteAsync();
      using var streamWriter = new StreamWriter(stream);
      await streamWriter.WriteLineAsync(output);
    }
  }

  /// <summary>
  /// Export any of the JSON files with filepicker
  /// </summary>
  /// <param name="topLevel"></param>
  /// <param name="type">Route, Train & Simulation</param>
  public static async void Export(TopLevel topLevel, string type) {
    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
      Title = "Export JSON",
      FileTypeChoices = new[] { JSON },
      SuggestedFileName = "export"
    });

    if (file == null) return;

    await using var stream = await file.OpenWriteAsync();
    using var streamWriter = new StreamWriter(stream);

    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    switch (type) {
      case "Route":
        if (DataManager.TrainRoutes.Any()) {
          var RouteOutput =
            JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options);
          await streamWriter.WriteLineAsync(RouteOutput);
        }

        break;

      case "Train":
        if (DataManager.Trains.Any()) {
          var TrainOutput = JsonSerializer.Serialize(DataManager.Trains[DataManager.CurrentTrain], Json_options);
          await streamWriter.WriteLineAsync(TrainOutput);
        }

        break;

      case "Simulation":
        if (Simulation.LatestSimulation != null) {
          var SimulationOutput = JsonSerializer.Serialize(Simulation.LatestSimulation, Json_options);
          await streamWriter.WriteLineAsync(SimulationOutput);
        }

        break;

      case "Settings":
        var SettingsOutput = JsonSerializer.Serialize(SettingsManager.CurrentSettings, Json_options);
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
    var FileAsString = "";
    using (var sr = File.OpenText(Path)) {
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

    var Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");

    // Save the current train route
    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(Path,
      JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options));
  }

  public static void SaveSpecific(TrainRoute route) {
    if (route == null) return;

    var Path = route.FilePath;
    Logger.Debug("Save: " + Path);
    // Save the current train route
    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(Path, JsonSerializer.Serialize(route, Json_options));
  }

  /// <summary>
  /// Imports all Json-files from folders defined by user in settings view. Also sets current train route.
  /// </summary>
  /// <param name="SavedPaths">Takes the list of saved paths from settings</param>
  /// <returns>Returns available routes a list of strings</returns>
    public static List<TrainRoute> ReadRoutesFromFolder(List<string> savedPaths) {
    List<string> files = new List<string>();
    var paths = new List<string>();

    //Create default folder if it doesn't exist
    try {
      if (!Directory.Exists(DefaultRouteFolderPath)) Directory.CreateDirectory(DefaultRouteFolderPath);
    } catch (Exception ex) {
      Logger.Debug(ex.Message);
    }


    try {
      foreach (var path in savedPaths) {
        Logger.Debug(path);
        if (Directory.Exists(path)) {
          var filesInFolder = Directory.EnumerateFiles(path, "*.json");

          foreach (var file in filesInFolder) {
            var fileAsString = "";
            using (var sr = File.OpenText(file)) {
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
      Logger.Debug(ex.Message);
    }


    // Deserialize the JSON strings into objects and add to list
    var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
    
    var trainRoutes = new List<TrainRoute>();
    for (var i = 0; i < files.Count; i++) {
      var importedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(files[i], jsonOptions);
      importedTrainRoute.FilePath = paths[i];
      importedTrainRoute.Id = DataManager.CreateId();
      trainRoutes.Add(importedTrainRoute);
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
      var FirstRoute = ImportedRoutesAsStrings[0];
      DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = DataManager.TrainRoutes[0];
      return FirstRoute;
    }

    var NewCurrentRoute = ImportedRoutesAsStrings[RouteIndex];
    DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = DataManager.TrainRoutes[RouteIndex];
    return NewCurrentRoute;
  }

  /// <summary>
  /// Saves route's simulation data to "simulation.json" file
  /// </summary>
  /// <param name="sim">(SimulationData) Route's simulation data</param>
  public static void SaveSimulationData(SimulationData sim) {
    var Path = DataManager.CreateFilePath("", "Simulation");
    try {
      if (!Directory.Exists(DefaultSimulationsFolderPath)) Directory.CreateDirectory(DefaultSimulationsFolderPath);
    }
    catch (Exception Ex) {
      Logger.Debug(Ex.Message);
    }

    // Save the simulation
    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(Path, JsonSerializer.Serialize(sim, Json_options));
  }

  /// <summary>
  /// Imports all train Json-files from folders defined by user in settings view.
  /// </summary>
  /// <param name="SavedPaths">Takes the list of saved paths from settings</param>
  /// <returns>Returns imported trains</returns>
  public static List<Train> StartupTrainFolderImport(List<string> SavedPaths) {
    List<Train> Trains = new List<Train>();
    var Paths = new List<string>();

    if (SavedPaths == null) return Trains;

    var Json_options = new JsonSerializerOptions { IncludeFields = true };

    try {
      foreach (var Path in SavedPaths) {
        Logger.Debug(Path);
        if (Directory.Exists(Path)) {
          var FilesInFolder = Directory.EnumerateFiles(Path, "*.json");

          foreach (var file in FilesInFolder) {
            var FileAsString = "";
            using (var sr = File.OpenText(file)) {
              string S;
              while ((S = sr.ReadLine()) != null) FileAsString += S;
            }

            if (FileAsString.Contains("MaxSpeed")) {
              var LoadedTrain = JsonSerializer.Deserialize<Train>(FileAsString, Json_options);
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
  /// Load saved trains from folder to Datamanager.Trains and Datamanager.CurrentTrain
  /// </summary>
  [Obsolete]
  public static void LoadTrains() {
    var TrainsDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains");
    var Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains", "train.json");
    var FileAsString = "";

    try {
      if (!Directory.Exists(TrainsDirectory)) Directory.CreateDirectory(TrainsDirectory);
    }
    catch (Exception Ex) {
      Logger.Debug(Ex.Message);
    }

    // Open the file to read from
    using (var Sr = File.OpenText(Path)) {
      // Read the lines on the file and gather a list from them
      string S;
      while ((S = Sr.ReadLine()) != null) FileAsString += S;
    }

    // Deserialise the JSON string into a object
    var Json_options = new JsonSerializerOptions { IncludeFields = true };
    var LoadedTrain = JsonSerializer.Deserialize<Train>(FileAsString, Json_options);

    // Set the imported train as the currently selected one
    DataManager.Trains.Add(LoadedTrain);
    DataManager.CurrentTrain = DataManager.Trains.Count - 1;
  }

  /// <summary>
  /// Saves DataManager.CurrentTrain to folder
  /// </summary>
  public static void SaveTrain(Train train) {
    var TrainsDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains");
    var Path = System.IO.Path.Combine(train.FilePath);

    try {
      if (!Directory.Exists(TrainsDirectory)) Directory.CreateDirectory(TrainsDirectory);
    }
    catch (Exception Ex) {
      Logger.Debug(Ex.Message);
    }

    // Save the train
    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(Path, JsonSerializer.Serialize(train, Json_options));
  }

  public static void SaveSettings(Settings settings) {
    var Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");

    // Save the settings
    var Json_options = new JsonSerializerOptions { WriteIndented = true };
    File.WriteAllText(Path, JsonSerializer.Serialize(settings, Json_options));
  }

  public static void LoadSettings() {
    var Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");
    var FileAsString = "";

    // Check if the file exists
    if (!File.Exists(Path)) {
      SettingsManager.GenerateNewSettings();
      return;
    }

    // Open the file to read from
    using (var Sr = File.OpenText(Path)) {
      // Read the lines on the file and gather a list from them
      string S;
      while ((S = Sr.ReadLine()) != null) FileAsString += S;
    }

    // Deserialise the JSON string into a object
    var Json_options = new JsonSerializerOptions { IncludeFields = true };
    var LoadedSettings = JsonSerializer.Deserialize<Settings>(FileAsString, Json_options);

    // Set the settings
    SettingsManager.CurrentSettings = LoadedSettings;
  }

  internal static void OpenGuide() {
    var Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "StartManual.pdf");

    try {
      var p = new Process();
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

  public static async Task<string> OpenFolder(TopLevel topLevel) {
    var Path = "";
    var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
      Title = "Choose folder"
    });

    if (folder.Count > 0) Path = folder[0].Path.AbsolutePath;
    return Path;
  }
}