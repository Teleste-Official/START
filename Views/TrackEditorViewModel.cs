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
            Routes = DataManager.TrainRoutes;
            //Routes = DataManager.TrainRoutes.ToList();
            TrackName = "Track Name...";
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
            CurrentAction = "None";
            return;
        }
    }
}
