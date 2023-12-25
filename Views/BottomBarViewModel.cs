using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SmartTrainApplication.Views
{
    public class BottomBarViewModel : ViewModelBase
    {

        public BottomBarViewModel()
        {
            
        }

        public void SaveButton()
        {
            for (int i = 0; i < DataManager.TrainRoutes.Count; i++)
            {
                if (DataManager.TrainRoutes[i].Edited)
                {
                    FileManager.SaveSpecific(DataManager.TrainRoutes[i]);
                    DataManager.TrainRoutes[i].Edited = false;
                }
            }

            for (int i = 0; i < DataManager.Trains.Count; i++)
            {
                if (DataManager.Trains[i].Edited)
                {
                    FileManager.SaveTrain(DataManager.Trains[i]);
                    DataManager.Trains[i].Edited = false;
                }
            }
        }

        public void SaveAsButton()
        {
            LayerManager.ExportNewRoute(MainWindow.TopLevel, DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name, DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Id);        }
    }
}
