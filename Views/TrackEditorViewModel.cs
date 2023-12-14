using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SmartTrainApplication.Views
{
    public class TrackEditorViewModel : ViewModelBase
    {
        public string TrackName { get; set; }
        public List<TrainRoute> Routes { get; set; }
        public List<RouteCoordinate> Stops { get; set; }
        private string CurrentAction {  get; set; }
        public bool AddingNew { get; set; } // This is for switching the combobox on a newly created route

        public TrackEditorViewModel()
        {
            if (DataManager.TrainRoutes.Count == 0)
                LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);
            Routes = DataManager.TrainRoutes.ToList();
            TrackName = "Track Name...";
            CurrentAction = "None";
            AddingNew = false;

            Stops = DataManager.GetStops();
        }

        public void AddLineButton()
        {
            LayerManager.ClearFeatures();
            LayerManager.AddLine();
            CurrentAction = "AddLine";
        }

        public void AddTunnelButton()
        {
            LayerManager.ClearFeatures();
            LayerManager.AddTunnel();
            CurrentAction = "AddTunnel";
        }

        public void AddStopButton()
        {
            LayerManager.ClearFeatures();
            LayerManager.AddTunnel();
            CurrentAction = "AddStop";
        }

        public void ModifyButton()
        {
            if (CurrentAction != "ModifyTrack")
            {
                LayerManager.ClearFeatures();
                LayerManager.TurnImportToEdit();
                CurrentAction = "ModifyTrack";
            }
        }

        public void ConfirmButton()
        {
            if (CurrentAction == "ModifyTrack")
            {
                LayerManager.ApplyEditing(DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name, DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Id, DataManager.TrainRoutes[DataManager.CurrentTrainRoute].FilePath);
            }
            if (CurrentAction == "AddLine"){
                AddingNew = true;
                LayerManager.ClearAllLayers();
                LayerManager.ApplyEditing("Route " + (DataManager.TrainRoutes.Count + 1));
                Routes = DataManager.TrainRoutes.ToList();
                RaisePropertyChanged(nameof(Routes));
                AddingNew = false;
            }
            if (CurrentAction == "AddStop")
            {
                LayerManager.ConfirmStops();
                SetStopsToUI();
            }
            if (CurrentAction == "AddTunnel")
            {
                LayerManager.ConfirmTunnel();
            }
            if (CurrentAction == "None")
            {
                DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name = TrackName;
                DataManager.SetStopsNames(Stops);
                Routes = DataManager.TrainRoutes.ToList();
                SetStopsToUI();
                RaisePropertyChanged(nameof(Routes));
            }
            CurrentAction = "None";
        }

        public void CancelButton()
        {
            CurrentAction = "None";
            LayerManager.ClearFeatures();
            LayerManager.ClearAllLayers();
            LayerManager.ChangeCurrentRoute(-1);
        }

        public void SetValuesToUI()
        {
            TrackName = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(TrackName));
        }

        public void UpdateRoutesToUI()
        {
            TrackName = DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(TrackName));
            RaisePropertyChanged(nameof(Routes));
        }

        public void SetStopsToUI()
        {
            Stops = DataManager.GetStops();
            RaisePropertyChanged(nameof(Stops));
        }

        public void DrawFocusedStop(string _Id)
        {
            RouteCoordinate selectedStop = Stops.First(item => item.Id == _Id);
            LayerManager.AddFocusStop(selectedStop);
        }

        public void ChangeCurrentRouteIndex(int index)
        {
            LayerManager.ChangeCurrentRoute(index);
        }

        public void ImportButton()
        {
            LayerManager.ImportNewRoute(SettingsManager.CurrentSettings.RouteDirectories);
        }
    }
}
