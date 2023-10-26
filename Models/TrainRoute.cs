using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class TrainRoute
    {
        public string Name { get; set; }
        public List<RouteCoordinate> Coords { get; set; }

        public TrainRoute() { }

        public TrainRoute(string name, List<RouteCoordinate> coords)
        {
            Name = name;
            Coords = coords;
        }
    }

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
}
