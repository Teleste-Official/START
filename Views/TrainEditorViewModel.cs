#region

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

  // State management properties
  private bool _trainComboBoxEnabled = true;
  private bool _nameFieldEnabled;
  private bool _confirmButtonEnabled;
  private EditorAction _currentAction = EditorAction.None;
  private bool _isEditing;
  private string _buttonText = "Edit";

  // Data properties
  private string _title;
  private string _description;
  private string _speed;
  private string _acceleration;
  private int _iconIndex;
  private List<ListedTrain> _trains;

  public enum EditorAction {
    None,
    Adding,
    Modifying
  }

  public EditorAction CurrentAction {
    get => _currentAction;
    set {
      if (_currentAction != value) {
        _currentAction = value;
        RaisePropertyChanged(nameof(CurrentAction));
      }
    }
  }

  public string ButtonText {
    get => _buttonText;
    set {
      if (_buttonText != value) {
        _buttonText = value;
        RaisePropertyChanged(nameof(ButtonText));
      }
    }
  }

  public bool IsEditing {
    get => _isEditing;
    set {
      if (_isEditing != value) {
        _isEditing = value;
        RaisePropertyChanged(nameof(IsEditing));
      }
    }
  }

  public void EditButton() {
    if (CurrentAction == EditorAction.None && DataManager.Trains.Any()) {
      CurrentAction = EditorAction.Modifying;
      IsEditing = true;
      ButtonText = "Confirm";
      ConfirmButtonEnabled = ValidateForm();
    }
    else {
      ConfirmButton();
    }
  }

  public bool TrainComboBoxEnabled {
    get => _trainComboBoxEnabled;
    set {
      if (_trainComboBoxEnabled != value) {
        _trainComboBoxEnabled = value;
        RaisePropertyChanged(nameof(TrainComboBoxEnabled));
      }
    }
  }

  public bool NameFieldEnabled {
    get => _nameFieldEnabled;
    set {
      if (_nameFieldEnabled != value) {
        _nameFieldEnabled = value;
        RaisePropertyChanged(nameof(NameFieldEnabled));
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

  public string Title {
    get => _title;
    set {
      if (_title != value) {
        _title = value;
        RaisePropertyChanged(nameof(Title));
        ValidateForm();
      }
    }
  }

  public string Description {
    get => _description;
    set {
      if (_description != value) {
        _description = value;
        RaisePropertyChanged(nameof(Description));
        ValidateForm();
      }
    }
  }

  public string Speed {
    get => _speed;
    set {
      if (_speed != value) {
        _speed = value;
        RaisePropertyChanged(nameof(Speed));
        ValidateForm();
      }
    }
  }

  public string Acceleration {
    get => _acceleration;
    set {
      if (_acceleration != value) {
        _acceleration = value;
        RaisePropertyChanged(nameof(Acceleration));
        ValidateForm();
      }
    }
  }

  public int IconIndex {
    get => _iconIndex;
    set {
      if (_iconIndex != value) {
        _iconIndex = value;
        RaisePropertyChanged(nameof(IconIndex));
      }
    }
  }

  public List<ListedTrain> Trains {
    get => _trains;
    set {
      _trains = value;
      RaisePropertyChanged(nameof(Trains));
    }
  }

  public static List<Bitmap> Icons { get; set; }

  public class ListedTrain : Train {
    public Bitmap Image { get; set; }

    public ListedTrain(Train train, Bitmap image) {
      // Copy all properties from base Train
      Id = train.Id;
      FilePath = train.FilePath;
      Edited = train.Edited;
      Name = train.Name;
      Description = train.Description;
      MaxSpeed = train.MaxSpeed;
      Acceleration = train.Acceleration;
      Icon = train.Icon;
      Specifier = train.Specifier;
      Image = image;
    }
  }

  public TrainEditorViewModel() {
    InitializeData();

    if (Trains.Count == 0) {
      AddTrainButton();
    } else {
      ResetAllControls();
    }
  }

  private void InitializeData() {
    if (DataManager.Trains.Count == 0)
      DataManager.Trains = FileManager.StartupTrainFolderImport(SettingsManager.CurrentSettings.TrainDirectories);

    SetIcons();
    Trains = DataManager.Trains.Select(t => new ListedTrain(t, Icons[t.Icon])).ToList();
    FileManager.CurrentView = "Train";
  }

  public void AddTrainButton() {
    CurrentAction = EditorAction.Adding;
    TrainComboBoxEnabled = false;
    NameFieldEnabled = true;
    ClearValues();
    IsEditing = true;
    ConfirmButtonEnabled = false;
  }

  public void ConfirmButton() {
    switch (CurrentAction) {
      case EditorAction.Adding:
        SaveNewTrain();
        break;
      case EditorAction.Modifying:
        UpdateExistingTrain();
        break;
    }

    ResetAllControls();
    CurrentAction = EditorAction.None;
  }

  private void SaveNewTrain() {
    if (!ValidateForm()) return;

    Train newTrain = new(Title, Description, float.Parse(Speed), float.Parse(Acceleration), IconIndex);
    DataManager.Trains.Add(newTrain);
    Trains.Add(new ListedTrain(newTrain, Icons[newTrain.Icon]));

    DataManager.CurrentTrain = DataManager.Trains.Count - 1;
    Trains = new List<ListedTrain>(Trains); // Refresh list
  }

  private void UpdateExistingTrain() {
    if (!ValidateForm() || DataManager.CurrentTrain < 0) return;

    Train updatedTrain = new(Title, Description, float.Parse(Speed), float.Parse(Acceleration), IconIndex,
      DataManager.Trains[DataManager.CurrentTrain].Id,
      DataManager.Trains[DataManager.CurrentTrain].FilePath);

    DataManager.UpdateTrain(updatedTrain);
    Trains[DataManager.CurrentTrain] = new ListedTrain(updatedTrain, Icons[updatedTrain.Icon]);
    Trains = new List<ListedTrain>(Trains); // Refresh list
  }

  public void CancelButton() {
    if (CurrentAction == EditorAction.Adding)
      ClearValues();
    else if (CurrentAction == EditorAction.Modifying)
      // Load first train's data
      if (DataManager.Trains.Count > 0) {
        DataManager.CurrentTrain = 0;
        SetValuesToUI();
      }

    ResetAllControls();
    CurrentAction = EditorAction.None;
  }

  private void ResetAllControls() {
    TrainComboBoxEnabled = true;
    IsEditing = false;
    ConfirmButtonEnabled = false;
    ButtonText = "Edit";
  }

  private bool ValidateForm() {
    var isValid = !string.IsNullOrWhiteSpace(Title) &&
                  !string.IsNullOrWhiteSpace(Speed) &&
                  !string.IsNullOrWhiteSpace(Acceleration);

    if (CurrentAction == EditorAction.Adding)
      isValid = isValid && !DataManager.Trains.Any(t =>
        t.Name.Equals(Title, StringComparison.OrdinalIgnoreCase));

    ConfirmButtonEnabled = isValid;
    return isValid;
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