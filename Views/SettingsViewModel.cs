using SmartTrainApplication.Data;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SmartTrainApplication.Views
{
    internal class SettingsViewModel : ViewModelBase
    {
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string VersionNumber { get; set; }
        public string RouteDirectories { get; set; }
        public string TrainDirectories { get; set; }

        public SettingsViewModel()
        {
            VersionNumber = "Version " + Assembly.GetEntryAssembly().GetName().Version.ToString();
            Longitude = SettingsManager.CurrentSettings.Longitude.ToString();
            Latitude = SettingsManager.CurrentSettings.Latitude.ToString();
            // TODO: RouteDirectories, I see two options for this, either the user types or copy-pastes the directory
            // in the textbox or we implement a button that when clicked opens a file picker pop-up whatever it was called
            // and the user can select the directory there. Latter option would be better, because it eliminates user error.
            // -Metso
            List<string> ListOfRoutes = SettingsManager.CurrentSettings.RouteDirectories;
            foreach (string dir in SettingsManager.CurrentSettings.RouteDirectories)
            {
                RouteDirectories += dir + "\n";
            }
            foreach (string dir in SettingsManager.CurrentSettings.TrainDirectories)
            {
                TrainDirectories += dir + "\n";
            }
        }

        public void ResetButton()
        {
            SettingsManager.GenerateNewSettings();

            VersionNumber = "Version " + Assembly.GetEntryAssembly().GetName().Version.ToString();
            Longitude = SettingsManager.CurrentSettings.Longitude.ToString();
            Latitude = SettingsManager.CurrentSettings.Latitude.ToString();

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(VersionNumber));
            RaisePropertyChanged(nameof(Longitude));
            RaisePropertyChanged(nameof(Latitude));
        }

        public void SaveButton()
        {
            // TODO: Check that values are in correct format -Metso
            double x = Convert.ToDouble(Longitude);
            double y = Convert.ToDouble(Latitude);

            SettingsManager.CurrentSettings.Longitude = x;
            SettingsManager.CurrentSettings.Latitude = y;

            // TODO: The directories -Metso

            FileManager.SaveSettings(SettingsManager.CurrentSettings);
        }

        public void LogsButton()
        {
            return;
        }

        public async void AddRouteButton()
        {
            string NewPath = await FileManager.OpenFolder(MainWindow.TopLevel);
            if (!String.IsNullOrEmpty(NewPath))
            {
                SettingsManager.CurrentSettings.AddRouteDirectory(NewPath);
                FileManager.SaveSettings(SettingsManager.CurrentSettings);
            }           
            return;
        }
    }
}
