using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    internal class Settings
    {
        public string Longitude {  get; set; }
        public string Latitude { get; set; }
        public List<string> RouteDirectories { get; set; }
        public List<string> TrainDirectories { get; set; }
        public string VersionNumber { get; set; }

        public Settings() { }

        public Settings(string longitude, string latitude, List<string> routeDirectories, List<string> trainDirectories)
        {
            Longitude = longitude;
            Latitude = latitude;
            RouteDirectories = routeDirectories;
            TrainDirectories = trainDirectories;
        }
    }
}
