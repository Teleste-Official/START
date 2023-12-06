using Avalonia;
using Mapsui.Nts.Editing;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static SmartTrainApplication.Views.TrainEditorViewModel;

namespace SmartTrainApplication.Views
{
    public class TrackEditorViewModel : ViewModelBase
    {
        public string TrackName { get; set; }
        public List<TrainRoute> Routes { get; set; }
        private string CurrentAction {  get; set; }

        public TrackEditorViewModel()
        {
            if (DataManager.TrainRoutes.Count == 0)
                LayerManager.ImportNewRoute(MainWindow.TopLevel);
            Routes = DataManager.TrainRoutes.ToList();
            TrackName = "Track Name...";
            CurrentAction = "None";
        }

        public void AddLineButton()
        {
            LayerManager.AddLine();
            CurrentAction = "AddLine";
        }

        public void AddTunnelButton()
        {
            LayerManager.AddTunnel();
            CurrentAction = "AddTunnel";
        }

        public void AddStopButton()
        {
            LayerManager.AddTunnel();
            CurrentAction = "AddStop";
        }

        public void ModifyButton()
        {
            LayerManager.TurnImportToEdit();
            CurrentAction = "ModifyTrack";
        }

        public void ConfirmButton()
        {
            if (CurrentAction == "ModifyTrack")
            {
                LayerManager.ApplyEditing();
            }
            if (CurrentAction == "AddLine"){
                LayerManager.ExportNewRoute(MainWindow.TopLevel);
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
            }
            CurrentAction = "None";
            return;
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
    }
}
