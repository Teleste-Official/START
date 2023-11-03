using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class RoutePoint
    {
        public string Longitude { get; set; }
        public string Latitude { get; set; }

        public RoutePoint() { }

        public RoutePoint(string X, string Y)
        {
            Longitude = X;
            Latitude = Y;
        }
    }
}
