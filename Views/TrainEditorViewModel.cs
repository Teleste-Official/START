using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartTrainApplication.Views
{
    public class TrainEditorViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Speed { get; set; }
        public string Acceleration { get; set; }
        public int IconIndex { get; set; }

        public List<Bitmap> Icons { get; set; }

        public List<ListedTrain> Trains { get; set; }

        public class ListedTrain : Train
        {
            public Bitmap Image { get; set; }

            public ListedTrain(Train train, Bitmap image)
            {
                Name = train.Name;
                Description = train.Description;
                MaxSpeed = train.MaxSpeed;
                Acceleration = train.Acceleration;
                Icon = train.Icon;
                Image = image;
            }
        }

        public TrainEditorViewModel()
        {
            if (DataManager.Trains.Count == 0)
                DataManager.Trains = FileManager.StartupTrainFolderImport(SettingsManager.CurrentSettings.TrainDirectories);

            Icons = new List<Bitmap>
            {
                new Bitmap(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_train1.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_train2.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_tram.png")))
            };

            Trains = new List<ListedTrain>();
            SetTrainsToUI();
        }

        public void UpdateTrainButton()
        {
            if (Title == null || Description == null || Speed == null || Acceleration == null)
            {
                return;
            }
            Train newTrain = new Train(Title, Description, float.Parse(Speed), float.Parse(Acceleration), IconIndex, DataManager.CurrentTrain.Id, DataManager.CurrentTrain.FilePath);
            DataManager.UpdateTrain(newTrain);
            Trains[DataManager.Trains.FindIndex(a => a.Id.Contains(DataManager.CurrentTrain.Id))].Name = Title;
            Trains = Trains;
            RaisePropertyChanged(nameof(Trains));
        }

        public void SaveTrainButton()
        {
            if (Title == null || Description == null || Speed == null || Acceleration == null)
            {
                return;
            }
            Train newTrain = new Train(Title, Description, float.Parse(Speed), float.Parse(Acceleration), IconIndex, DataManager.CurrentTrain.Id, DataManager.CurrentTrain.FilePath);
            
            DataManager.CurrentTrain = newTrain;
            DataManager.Trains.Add(newTrain);
            Trains.Add(new ListedTrain(newTrain, Icons[newTrain.Icon]));
            RaisePropertyChanged(nameof(Trains));
            SetValuesToUI();
            FileManager.SaveTrain();
        }

        public void SetValuesToUI()
        {
            Title = DataManager.CurrentTrain.Name;
            Description = DataManager.CurrentTrain.Description;
            Speed = DataManager.CurrentTrain.MaxSpeed.ToString();
            Acceleration = DataManager.CurrentTrain.Acceleration.ToString();
            IconIndex = DataManager.CurrentTrain.Icon;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Speed));
            RaisePropertyChanged(nameof(Acceleration));
            RaisePropertyChanged(nameof(IconIndex));
        }

        public void ClearValues()
        {
            Title = null;
            Description = null;
            Speed = null;
            Acceleration = null;
            IconIndex = 0;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Speed));
            RaisePropertyChanged(nameof(Acceleration));
            RaisePropertyChanged(nameof(IconIndex));
        }

        public void SetTrainsToUI()
        {
            Trains.Clear();
            foreach (var Train in DataManager.Trains)
            {
                Trains.Add(new ListedTrain(Train, Icons[Train.Icon]));
            }
            Trains = Trains.ToList(); // This needs to be here for the UI to update on its own -Metso
        }
    }
}
