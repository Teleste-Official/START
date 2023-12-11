using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System.Collections.Generic;
using System.Linq;

namespace SmartTrainApplication.Views
{
    public class TrackEditorViewModel : ViewModelBase
    {
        public string TrackName { get; set; }
        public List<TrainRoute> Routes { get; set; }
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
            LayerManager.ClearFeatures();
            LayerManager.TurnImportToEdit();
            CurrentAction = "ModifyTrack";
        }

        public void ConfirmButton()
        {
            if (CurrentAction == "ModifyTrack")
            {
                LayerManager.ApplyEditing(DataManager.CurrentTrainRoute.Name, DataManager.CurrentTrainRoute.Id);
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
            }
            if (CurrentAction == "AddTunnel")
            {
                LayerManager.ConfirmTunnel();
            }
            if (CurrentAction == "None")
            {
                DataManager.CurrentTrainRoute.Name = TrackName;
                Routes = DataManager.TrainRoutes.ToList();
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
            TrackName = DataManager.CurrentTrainRoute.Name;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(TrackName));
        }

        public void UpdateRoutesToUI()
        {
            TrackName = DataManager.CurrentTrainRoute.Name;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(TrackName));
            RaisePropertyChanged(nameof(Routes));
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
