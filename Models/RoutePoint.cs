namespace SmartTrainApplication.Models;

/// <summary>
/// A point that can be used to store coordinate data
/// <list type="bullet">
/// <item>(string) Longitude</item>
/// <item>(string) Latitude</item>
/// </list>
/// </summary>
public class RoutePoint {
  public string Longitude { get; set; }
  public string Latitude { get; set; }

  public RoutePoint() {
  }

  public RoutePoint(string X, string Y) {
    Longitude = X;
    Latitude = Y;
  }
}