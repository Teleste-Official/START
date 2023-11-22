using System;
using System.Drawing;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SmartTrainApplication.Models;

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
    // "TUNNELSTOP" - Point is a possible stop that is in a tunnel, not implemented yet -Metso
    public string StopName { get; set; } // This is only for stops, and can remain empty otherwise

    public RouteCoordinate() { }

    public RouteCoordinate(string X, string Y)
    {
        Longitude = X;
        Latitude = Y; 
        Type = "NORMAL";
        StopName = "";
    }

    public void SetType(string Type)
    {
        this.Type = Type;
    }

}