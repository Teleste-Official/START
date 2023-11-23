using System;


namespace SmartTrainApplication.Views
{
    public class TrainEditorViewModel : ViewModelBase
    {
        public string Title { get; set; }

        public TrainEditorViewModel()
        {
            Title = "Add Train Test"; // This only for testing now here -Metso
        }
    }
}
