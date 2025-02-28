#region

using System.Text.Json.Serialization;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Models;

/// <summary>
/// The Train that will move along the railway TrainRoutes
/// <list type="bullet">
/// <item>(string) Name</item>
/// <item>(string) Description</item>
/// <item>(float) MaxSpeed</item>
/// <item>(int) Icon</item>
/// </list>
/// </summary>
public class Train {
  [JsonIgnore] public string Id { get; set; }
  [JsonIgnore] public string FilePath { get; set; }
  [JsonIgnore] public bool Edited { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
  public float MaxSpeed { get; set; }
  public float Acceleration { get; set; }
  public int Icon { get; set; }
  [JsonIgnore] public string Specifier = "Train";

  public Train() {
  }

  public Train(string name, string description, float maxSpeed, float acceleration, int icon, string ID = "",
    string filePath = "") {
    Name = name;
    Description = description;
    MaxSpeed = maxSpeed;
    Acceleration = acceleration;
    Icon = icon;
    if (ID == "")
      Id = DataManager.CreateId();
    else
      Id = ID;
    if (filePath == "")
      FilePath = DataManager.CreateFilePath(Id, Specifier);
    else
      FilePath = filePath;
    Edited = false;
  }

  public void SetValues(Train NewTrain) {
    Name = NewTrain.Name;
    Description = NewTrain.Description;
    MaxSpeed = NewTrain.MaxSpeed;
    Acceleration = NewTrain.Acceleration;
    Icon = NewTrain.Icon;
    FilePath = NewTrain.FilePath;
    Edited = true;
  }
}