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
            LayerManager.SaveEdits();
        }

        public void SaveAsButton()
        {
            LayerManager.ExportNewRoute(MainWindow.TopLevel, DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Name, DataManager.TrainRoutes[DataManager.CurrentTrainRoute].Id);
            
        }
    }
}
