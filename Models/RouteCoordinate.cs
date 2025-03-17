#region

using System.Text.Json.Serialization;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Models;

/// <summary>
///   The singular coordinate points that make up TrainRoutes
///   <list type="bullet">
///     <item>(string) Longitude</item>
///     <item>(string) Longitude</item>
///     <item>(string) Type</item>
///     <item>(string) StopName</item>
///   </list>
/// </summary>
public class RouteCoordinate {
  [JsonIgnore] public string Id { get; set; }
  public string Longitude { get; set; }
  public string Latitude { get; set; }

  public string Type { get; set; } // Ie. "NORMAL", "STOP" etc. -Metso
  
  //[JsonIgnore] public Geometry Geometry { get; set; }

  // Possible Types:
  // "NORMAL" - Normal route point on the surface
  // "TUNNEL_ENTRANCE" - Entrance for a tunnel
  // "TUNNEL" - Point is underground in a tunnel
  // "STOP" - Point is a possible stop
  // "TUNNEL_STOP" - Point is a possible stop that is in a tunnel
  // "TUNNEL_ENTRANCE_STOP" - Point is a possible stop that is in a tunnel entrance
  public string StopName { get; set; } // This is only for stops, and can remain empty otherwise

  public RouteCoordinate() {
    Id = DataManager.CreateId();
  }

  public RouteCoordinate(string x, string y) {
    Longitude = x;
    Latitude = y;
    Type = "NORMAL";
    StopName = "";
    Id = DataManager.CreateId();
  }

  /// <summary>
  ///   Changes the Type of the RouteCoordinate object to a given Type
  /// </summary>
  /// <param name="newType">(string) Type to set</param>
  public void SetType(string newType) {
    Type = newType;
  }

  public void SetName(string newName) {
    StopName = newName;
  }

  public string GetCoordinateString() {
    return $"POINT ({Longitude} {Latitude})";
  }
  

  public override string ToString() {
    return $"Id: {Id}, Longitude: {Longitude}, Latitude: {Latitude}, Type: {Type}, StopName: {StopName}";
  }

  public override bool Equals(object obj) {
    if (ReferenceEquals(this, obj)) {
      return true;
    }
    if (obj is null || GetType() != obj.GetType()) {
      return false;
    }

    RouteCoordinate other = (RouteCoordinate)obj;
    return string.Equals(Longitude, other.Longitude) &&
           string.Equals(Latitude, other.Latitude) &&
           string.Equals(Type, other.Type) &&
           string.Equals(StopName, other.StopName);
  }

  public static bool operator ==(RouteCoordinate left, RouteCoordinate right) {
    if (ReferenceEquals(left, null)) {
      return ReferenceEquals(right, null);
    }
      
    return left.Equals(right);
  }

  public static bool operator !=(RouteCoordinate left, RouteCoordinate right) {
    return !(left == right);
  }
}