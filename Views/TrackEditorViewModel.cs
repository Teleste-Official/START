using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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

namespace SmartTrainApplication.Views
{
    public class TrackEditorViewModel : ViewModelBase
    {

        public List<TrainRoute> ImportedRoutes { get; set; }

        public TrackEditorViewModel()
        {
            ImportedRoutes = FileManager.ImportedRoutes;
        }

        public void AddLineButton()
        {
            LayerManager.AddLine();
        }

        public void AddTunnelButton()
        {
            LayerManager.AddTunnel();
        }

        public void ModifyButton()
        {
            LayerManager.TurnImportToEdit();
        }

        public void ChangeCurrentRouteIndex(int index)
        {
            LayerManager.ChangeCurrentRoute(index);
        }

    }
}
