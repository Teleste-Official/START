namespace SmartTrainApplication.Models;

public class RouteCoordinate
{
    public string Longitude { get; set; }
    public string Latitude { get; set; }
    public string Type { get; set; } // Ie. "NORMAL", "STOP" etc. Could also be int, let's see what fits the best -Metso

    public RouteCoordinate() { }

    public RouteCoordinate(string X, string Y)
    {
        Longitude = X;
        Latitude = Y;
        Type = "NORMAL";
    }
}