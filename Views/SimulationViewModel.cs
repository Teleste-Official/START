using Avalonia.Platform;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static SmartTrainApplication.Views.TrainEditorViewModel;

namespace SmartTrainApplication.Views
{
    public class SimulationViewModel : ViewModelBase
    {

        public List<TrainRoute> Routes { get; set; }
        public List<ListedTrain> Trains { get; set; }

        public SimulationViewModel()
        {
            // Get routes
            if (DataManager.TrainRoutes.Count == 0)
                LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);
            Routes = DataManager.TrainRoutes.ToList();

            // Get trains
            if (DataManager.Trains.Count == 0)
                DataManager.Trains = FileManager.StartupTrainFolderImport(SettingsManager.CurrentSettings.TrainDirectories);

            if (Icons == null)
                SetIcons();

            Trains = new List<ListedTrain>();
            SetTrainsToUI();
        }

        public void RunSimulationButton()
        {
            Simulation.PreprocessRoute();
            return;
        }

        public void SetTrainsToUI()
        {
            Trains.Clear();
            foreach (var Train in DataManager.Trains)
            {
                Trains.Add(new ListedTrain(Train, Icons[Train.Icon]));
            }
            Trains = Trains.ToList(); // This needs to be here for the UI to update on its own -Metso
        }
    }
}