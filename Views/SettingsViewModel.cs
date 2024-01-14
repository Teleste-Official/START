using SmartTrainApplication.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            SetDirectoriesToUI();

            // Switch view in file manager
            FileManager.CurrentView = "Settings";
            Debug.WriteLine(FileManager.CurrentView);
        }

        public void ResetButton()
        {
            SettingsManager.GenerateNewSettings();

            VersionNumber = "Version " + Assembly.GetEntryAssembly().GetName().Version.ToString();
            Longitude = SettingsManager.CurrentSettings.Longitude.ToString();
            Latitude = SettingsManager.CurrentSettings.Latitude.ToString();

            SetDirectoriesToUI();

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
                SettingsManager.CurrentSettings.AddRouteDirectory(System.IO.Path.GetFullPath(NewPath));
                SetDirectoriesToUI();
            }           
        }

        public async void AddTrainButton()
        {
            string NewPath = await FileManager.OpenFolder(MainWindow.TopLevel);
            if (!String.IsNullOrEmpty(NewPath))
            {
                SettingsManager.CurrentSettings.AddTrainDirectory(System.IO.Path.GetFullPath(NewPath));
                SetDirectoriesToUI();
            }
        }

        private void SetDirectoriesToUI()
        {
            RouteDirectories = "";
            TrainDirectories = "";
            foreach (string dir in SettingsManager.CurrentSettings.RouteDirectories)
            {
                RouteDirectories += dir + "\n";
            }
            foreach (string dir in SettingsManager.CurrentSettings.TrainDirectories)
            {
                TrainDirectories += dir + "\n";
            }
            RaisePropertyChanged(nameof(RouteDirectories));
            RaisePropertyChanged(nameof(TrainDirectories));
        }
    }
}
