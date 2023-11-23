using SmartTrainApplication.Data;
using System;


namespace SmartTrainApplication.Views
{
    public class TrainEditorViewModel : ViewModelBase
    {
        public string Title { get; set; }

        public TrainEditorViewModel()
        {
            Title = "Add Train"; // This only for testing now here -Metso
        }

        public void SaveTrainButton()
        {
            DataManager.CurrentTrain = new Models.Train("Test", "Testing train", 0); // For testing, remove later -Metso
            DataManager.SaveTrain();
        }
    }
}
