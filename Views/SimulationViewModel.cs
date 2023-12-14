using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System.Collections.Generic;
using System.Linq;
using static SmartTrainApplication.Views.TrainEditorViewModel;

namespace SmartTrainApplication.Views
{
    public class SimulationViewModel : ViewModelBase
    {

        public List<TrainRoute> Routes { get; set; }
        public List<ListedTrain> Trains { get; set; }
        public string Interval { get; set; }
        public Dictionary<RouteCoordinate, bool> StopsDictionary { get; set; }
        public List<bool> StopsBooleans { get; set; }
        public List<RouteCoordinate> Stops { get; set; }

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

            Interval = Simulation.intervalTime.ToString();

            Stops = DataManager.GetStops();
            StopsDictionary = Stops.ToDictionary(x => x, x => false);
            StopsBooleans = new List<bool>();
        }

        public void RunSimulationButton()
        {
            Simulation.intervalTime = (int)float.Parse(Interval);
            Simulation.RunSimulation(StopsDictionary);
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

        public void SetStopsToUI()
        {
            Stops = DataManager.GetStops();
            StopsDictionary = Stops.ToDictionary(x => x, x => false);
            StopsBooleans = new List<bool>();
            RaisePropertyChanged(nameof(Stops));
        }

        public void SetBool(string _Id, bool value)
        {
            RouteCoordinate selectedStop = Stops.First(item => item.Id == _Id);
            StopsDictionary[selectedStop] = value;
        }

        public void DrawFocusedStop(string _Id)
        {
            RouteCoordinate selectedStop = Stops.First(item => item.Id == _Id);
            LayerManager.AddFocusStop(selectedStop);
        }
    }
}