﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Views;

public class TrainEditorViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  public string Title { get; set; }
  public string Description { get; set; }
  public string Speed { get; set; }
  public string Acceleration { get; set; }
  public int IconIndex { get; set; }

  public static List<Bitmap> Icons { get; set; }

  public List<ListedTrain> Trains { get; set; }

  public class ListedTrain : Train {
    public Bitmap Image { get; set; }

    public ListedTrain(Train train, Bitmap image) {
      Name = train.Name;
      Description = train.Description;
      MaxSpeed = train.MaxSpeed;
      Acceleration = train.Acceleration;
      Icon = train.Icon;
      Image = image;
    }
  }

  public TrainEditorViewModel() {
    if (DataManager.Trains.Count == 0)
      DataManager.Trains = FileManager.StartupTrainFolderImport(SettingsManager.CurrentSettings.TrainDirectories);

    SetIcons();

    Trains = new List<ListedTrain>();
    SetTrainsToUI();

    // Switch view in file manager
    FileManager.CurrentView = "Train";
    Logger.Debug(FileManager.CurrentView);
  }

  public void UpdateTrainButton() {
    if (!DataManager.Trains.Any())
      return;
    if (Title == null || Description == null || Speed == null || Acceleration == null) return;
    Train? newTrain = new(Title, Description, float.Parse(Speed), float.Parse(Acceleration), IconIndex,
      DataManager.Trains[DataManager.CurrentTrain].Id, DataManager.Trains[DataManager.CurrentTrain].FilePath);
    DataManager.UpdateTrain(newTrain);
    Trains[DataManager.Trains.FindIndex(a => a.Id.Contains(DataManager.Trains[DataManager.CurrentTrain].Id))].Name =
      Title;
    Trains = Trains;
    RaisePropertyChanged(nameof(Trains));
  }

  public void SaveTrainButton() {
    if (Title == null || Description == null || Speed == null || Acceleration == null) return;

    Train? newTrain = new(Title, Description, float.Parse(Speed), float.Parse(Acceleration), IconIndex);

    DataManager.Trains.Add(newTrain);
    newTrain.Edited = true;
    Trains.Add(new ListedTrain(newTrain, Icons[newTrain.Icon]));

    if (!DataManager.Trains.Any())
      DataManager.CurrentTrain = 0;
    else
      DataManager.CurrentTrain = DataManager.Trains.Count - 1;

    Trains = Trains;
    RaisePropertyChanged(nameof(Trains));
    SetValuesToUI();
  }

  public void SetValuesToUI() {
    Title = DataManager.Trains[DataManager.CurrentTrain].Name;
    Description = DataManager.Trains[DataManager.CurrentTrain].Description;
    Speed = DataManager.Trains[DataManager.CurrentTrain].MaxSpeed.ToString();
    Acceleration = DataManager.Trains[DataManager.CurrentTrain].Acceleration.ToString();
    IconIndex = DataManager.Trains[DataManager.CurrentTrain].Icon;

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(Title));
    RaisePropertyChanged(nameof(Description));
    RaisePropertyChanged(nameof(Speed));
    RaisePropertyChanged(nameof(Acceleration));
    RaisePropertyChanged(nameof(IconIndex));
  }

  public void ClearValues() {
    Title = null;
    Description = null;
    Speed = null;
    Acceleration = null;
    IconIndex = 0;

    // Notify the UI about the property changes
    RaisePropertyChanged(nameof(Title));
    RaisePropertyChanged(nameof(Description));
    RaisePropertyChanged(nameof(Speed));
    RaisePropertyChanged(nameof(Acceleration));
    RaisePropertyChanged(nameof(IconIndex));
  }

  public void SetTrainsToUI() {
    Trains.Clear();
    foreach (Train? train in DataManager.Trains) Trains.Add(new ListedTrain(train, Icons[train.Icon]));
    Trains = Trains.ToList(); // This needs to be here for the UI to update on its own -Metso
  }

  public static void SetIcons() {
    Icons = new List<Bitmap> {
      new(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_train2.png"))),
      new(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_train1.png"))),
      new(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_tram.png")))
    };
  }
}