#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Models;

/// <summary>
/// The railway route that the Train will follow
/// <list type="bullet">
/// <item>(string) Name</item>
/// <item>(List of RouteCoordinate) Coords</item>
/// </list>
/// </summary>
public class TrainRoute {
  [JsonIgnore] public string Id { get; set; }
  [JsonIgnore] public string FilePath { get; set; }
  [JsonIgnore] public bool Edited { get; set; }
  public string Name { get; set; }
  public List<RouteCoordinate> Coords { get; set; }
  [JsonIgnore] public string Specifier = "Route";

  public TrainRoute() {
  }

  public TrainRoute(string name, List<RouteCoordinate> coords, string ID = "", string filePath = "") {
    Name = name;
    Coords = coords;
    
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

  public List<RouteCoordinate> GetStopCoordinates() {
    List<RouteCoordinate> stopsCoordinates = new();
    
    foreach (RouteCoordinate coord in Coords) {
      if (coord.Type == "STOP" || coord.Type == "TUNNEL_STOP" || coord.Type == "TUNNEL_ENTRANCE_STOP") {
        stopsCoordinates.Add(coord);
      }
    }
    return stopsCoordinates;
  }

  public string GetGeometry() {
    IEnumerable<string> coordStrings = Coords.Select(coord => $"{coord.Longitude} {coord.Latitude}");
    string geometryString = "LINESTRING (" + string.Join(", ", coordStrings) + ")";
    return geometryString;
  }

  public override string ToString() {
    StringBuilder sb = new();
    sb.AppendLine("{");
    sb.AppendLine($"  \"Id\": \"{Id}\",");
    sb.AppendLine($"  \"FilePath\": \"{FilePath}\",");
    sb.AppendLine($"  \"Edited\": {Edited.ToString().ToLower()},");
    sb.AppendLine($"  \"Name\": \"{Name}\",");
    sb.AppendLine($"  \"Specifier\": \"{Specifier}\",");
    sb.AppendLine("  \"Coords\": [");

    for (int i = 0; i < Coords.Count; i++) {
      RouteCoordinate coord = Coords[i];
      sb.Append("    { ");
      sb.Append($"\"Latitude\": {coord.Latitude}, \"Longitude\": {coord.Longitude}, \"Type\": \"{coord.Type}\", \"StopName\": \"{coord.StopName}\" ");
      sb.Append("}");
      if (i < Coords.Count - 1)
        sb.AppendLine(",");
      else
        sb.AppendLine();
    }

    sb.AppendLine("  ]");
    sb.Append("}");
    return sb.ToString();
  }
  
  public override bool Equals(object obj) {
    if (ReferenceEquals(this, obj)) {
      return true;
    }
    if (obj is null || GetType() != obj.GetType()) {
      return false;
    }

    TrainRoute other = (TrainRoute)obj;
        
    // Compare Name first.
    if (!string.Equals(Name, other.Name)) {
      return false;
    }

    // Both Coords null or both non-null with same count.
    if (Coords == null && other.Coords == null) {
      return true;
    }
    if (Coords == null || other.Coords == null || Coords.Count != other.Coords.Count) {
      return false;
    }

    // Compare each coordinate in sequence (assuming RouteCoordinate.Equals is overridden)
    return Coords.SequenceEqual(other.Coords);
  }

  public override int GetHashCode() {
    unchecked { // Overflow is fine
      int hash = 17;
      hash = hash * 23 + (Name != null ? Name.GetHashCode() : 0);
      if (Coords != null) {
        foreach (RouteCoordinate coord in Coords) {
          hash = hash * 23 + (coord != null ? coord.GetHashCode() : 0);
        }
      }
      return hash;
    }
  }

  public static bool operator ==(TrainRoute left, TrainRoute right) {
    if (ReferenceEquals(left, null)) {
      return ReferenceEquals(right, null);
    }
    return left.Equals(right);
  }

  public static bool operator !=(TrainRoute left, TrainRoute right) {
    return !(left == right);
  }
  
}