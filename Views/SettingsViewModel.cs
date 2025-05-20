#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Views;

internal class SettingsViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  public string Longitude { get; set; }
  public string Latitude { get; set; }
  public string VersionNumber { get; set; }
  public string RouteDirectory { get; set; }
  public string TrainDirectory { get; set; }
  
  public string SimulationDirectory { get; set; }

  // Regex pattern explanation:
  // ^\d{1,3}    = 1-3 digits at start
  // \.          = literal decimal point
  // \d+$        = one or more digits at end
  private static readonly Regex CoordinateRegex = new(@"^\d{1,3}\.\d+$");


  public SettingsViewModel() {
    VersionNumber = "Version " + Assembly.GetEntryAssembly().GetName().Version.ToString();
    Longitude = SettingsManager.CurrentSettings.Longitude.ToString();
    Latitude = SettingsManager.CurrentSettings.Latitude.ToString();
    SetDirectoriesToUI();

    // Switch view in file manager
    FileManager.CurrentView = "Settings";
    Logger.Debug($"Current view: {FileManager.CurrentView}");
  }

  public void ResetButton() {
    SettingsManager.GenerateNewSettings();

    VersionNumber = "Version " + Assembly.GetEntryAssembly().GetName().Version.ToString();
    Longitude = SettingsManager.CurrentSettings.Longitude.ToString();
    Latitude = SettingsManager.CurrentSettings.Latitude.ToString();

    SetDirectoriesToUI();

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(VersionNumber));
    RaisePropertyChanged(nameof(Longitude));
    RaisePropertyChanged(nameof(Latitude));
  }

  public void SaveButton() {
    double x;
    double y;

    if (ValidateCoordinate(Longitude)) {
      x = double.Parse(Longitude, CultureInfo.InvariantCulture);
    } else {
      x = SettingsManager.CurrentSettings.Longitude;
      Longitude = SettingsManager.CurrentSettings.Longitude.ToString();
      RaisePropertyChanged(nameof(Longitude));
    }

    if (ValidateCoordinate(Latitude)) {
      y = double.Parse(Latitude, CultureInfo.InvariantCulture);
    }
    else {
      y = SettingsManager.CurrentSettings.Latitude;
      Latitude = SettingsManager.CurrentSettings.Latitude.ToString();
      RaisePropertyChanged(nameof(Latitude));
    }

    SettingsManager.CurrentSettings.Longitude = x;
    SettingsManager.CurrentSettings.Latitude = y;

    SettingsManager.CurrentSettings.RouteDirectories = GetRouteDirectoriesFromUI();
    SettingsManager.CurrentSettings.TrainDirectories = GetTrainDirectoriesFromUI();
    SettingsManager.CurrentSettings.SimulationDirectories = GetSimulationDirectoriesFromUI();

    FileManager.SaveSettings(SettingsManager.CurrentSettings);

  }

  private bool ValidateCoordinate(string value) {
    return !string.IsNullOrWhiteSpace(value) &&
           CoordinateRegex.IsMatch(value) &&
           double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
  }

  public void LogsButton() {
    return;
  }

  public async void AddRouteImportDirectoryButton() {
    string? NewPath = await FileManager.OpenFolder(MainWindow.TopLevel, "Choose Route Directory");
    if (!string.IsNullOrEmpty(NewPath)) {
      SettingsManager.CurrentSettings.AddRouteDirectory(Path.GetFullPath(NewPath));
      SetDirectoriesToUI();
    }
  }

  public async void AddTrainImportDirectoryButton() {
    string? NewPath = await FileManager.OpenFolder(MainWindow.TopLevel, "Choose Train Directory");
    if (!string.IsNullOrEmpty(NewPath)) {
      SettingsManager.CurrentSettings.AddTrainDirectory(Path.GetFullPath(NewPath));
      SetDirectoriesToUI();
    }
  }

  public async void AddSimulationDirectoryButton() {
    string? NewPath = await FileManager.OpenFolder(MainWindow.TopLevel, "Choose Simulation Directory");
    if (!string.IsNullOrEmpty(NewPath)) {
      SettingsManager.CurrentSettings.AddSimulationDirectory(Path.GetFullPath(NewPath));
      SetDirectoriesToUI();
    }
  }

  private void SetDirectoriesToUI() {
    RouteDirectory = "";
    TrainDirectory = "";
    SimulationDirectory = "";
    foreach (string? dir in SettingsManager.CurrentSettings.RouteDirectories) RouteDirectory += dir;
    foreach (string? dir in SettingsManager.CurrentSettings.TrainDirectories) TrainDirectory += dir;
    foreach (string? dir in SettingsManager.CurrentSettings.SimulationDirectories) SimulationDirectory += dir;
    RaisePropertyChanged(nameof(RouteDirectory));
    RaisePropertyChanged(nameof(TrainDirectory));
    RaisePropertyChanged(nameof(SimulationDirectory));
  }

  private List<string> GetRouteDirectoriesFromUI() {
    if (string.IsNullOrWhiteSpace(RouteDirectory)) {
      return new List<string>();
    }

    return Regex.Split(RouteDirectory, @"\r?\n")
      .Select(line => line.Trim())
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .ToList();
  }

  private List<string> GetTrainDirectoriesFromUI() {
    if (string.IsNullOrWhiteSpace(TrainDirectory)) {
      return new List<string>();
    }

    return Regex.Split(TrainDirectory, @"\r?\n")
      .Select(line => line.Trim())
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .ToList();
  }

  private List<string> GetSimulationDirectoriesFromUI() {
    if (string.IsNullOrWhiteSpace(SimulationDirectory)) {
      return new List<string>();
    }

    return Regex.Split(SimulationDirectory, @"\r?\n")
      .Select(line => line.Trim())
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .ToList();
  }
}