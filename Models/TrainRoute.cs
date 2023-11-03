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
        public int Tunnels { get; set; }
        public List<RouteCoordinate> Coords { get; set; }

        public TrainRoute() { }

        public TrainRoute(string name, List<RouteCoordinate> coords)
        {
            Name = name;
            Coords = coords;
        }
    }
}
