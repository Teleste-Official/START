using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    /// <summary>
    /// The railway route that the Train will follow
    /// <list type="bullet">
    /// <item>(string) Name</item>
    /// <item>(List of RouteCoordinate) Coords</item>
    /// </list>
    /// </summary>
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
}
