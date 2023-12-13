namespace SmartTrainApplication.Models;

/// <summary>
/// The singular coordinate points that make up TrainRoutes
/// <list type="bullet">
/// <item>(string) Longitude</item>
/// <item>(string) Longitude</item>
/// <item>(string) Type</item>
/// <item>(string) StopName</item>
/// </list>
/// </summary>
public class RouteCoordinate
{
    public string Longitude { get; set; }
    public string Latitude { get; set; }
    public string Type { get; set; } // Ie. "NORMAL", "STOP" etc. -Metso
    // Possible Types:
    // "NORMAL" - Normal route point on the surface
    // "TUNNEL_ENTRANCE" - Entrance for a tunnel
    // "TUNNEL" - Point is underground in a tunnel
    // "STOP" - Point is a possible stop
    // "TUNNEL_STOP" - Point is a possible stop that is in a tunnel
    // "TUNNEL_ENTRANCE_STOP" - Point is a possible stop that is in a tunnel entrance
    public string StopName { get; set; } // This is only for stops, and can remain empty otherwise

    public RouteCoordinate() { }

    public RouteCoordinate(string X, string Y)
    {
        Longitude = X;
        Latitude = Y; 
        Type = "NORMAL";
        StopName = "";
    }

    /// <summary>
    /// Changes the Type of the RouteCoordinate object to a given Type
    /// </summary>
    /// <param name="Type">(string) Type to set</param>
    public void SetType(string Type)
    {
        this.Type = Type;
    }

}