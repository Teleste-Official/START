using SmartTrainApplication.Models;
using System.Collections.Generic;
using System.IO;

namespace SmartTrainApplication.Data
{
    internal class SettingsManager
    {
        public static Settings CurrentSettings;

        /// <summary>
        /// Generate new default settings -file and set it as the current one.
        /// </summary>
        public static void GenerateNewSettings()
        {
            List<string> newRouteDirectories = new List<string>() {
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Routes")
            };
            List<string> newTrainDirectories = new List<string>() {
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains")
            };

            // Default coordinates point to Tampere
            Settings settings = new Settings(23.76227433384882, 61.49741016814548, newRouteDirectories, newTrainDirectories);

            // Save the settings
            FileManager.SaveSettings(settings);
            CurrentSettings = settings;
            return;
        }
    }
}
