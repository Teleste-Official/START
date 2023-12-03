using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    internal class Settings
    {
        public double Longitude {  get; set; }
        public double Latitude { get; set; }
        public List<string> RouteDirectories { get; set; }
        public List<string> TrainDirectories { get; set; }
        public string VersionNumber { get; set; }

        public Settings() { }

        public Settings(double longitude, double latitude, List<string> routeDirectories, List<string> trainDirectories)
        {
            Longitude = longitude;
            Latitude = latitude;
            RouteDirectories = routeDirectories;
            TrainDirectories = trainDirectories;
            VersionNumber = Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
    }
}
