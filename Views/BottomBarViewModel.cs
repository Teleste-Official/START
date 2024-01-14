using SmartTrainApplication.Data;

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
            switch (FileManager.CurrentView)
            {
                case "Route":
                    FileManager.Export(MainWindow.TopLevel, "Route");
                    break;
                
                case "Train":
                    FileManager.Export(MainWindow.TopLevel, "Train");
                    break;
                
                case "Simulation":
                    FileManager.Export(MainWindow.TopLevel, "Simulation");
                    break;

                case "Settings":
                    FileManager.Export(MainWindow.TopLevel, "Settings");
                    break;
                
                default:
                    break;
            }
        }
    }
}
