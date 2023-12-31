﻿using Mapsui.Nts.Editing;
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
        public TrackEditorViewModel()
        {

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
    }
}
