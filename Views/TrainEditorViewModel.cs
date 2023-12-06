using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BruTile.Wms;
using BruTile.Wmts;
using DynamicData;
using Mapsui.UI.Avalonia;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;

namespace SmartTrainApplication.Views
{
    public class TrainEditorViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public object Speed { get; set; }
        public object Acceleration { get; set; }
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
            Icons = new List<Bitmap>
            {
                new Bitmap(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_train1.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_train2.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://SmartTrainApplication/Assets/start_ui_icon_tram.png")))
            };

            // TESTING 

            // TESTING
            if (DataManager.Trains.Count == 0)
            {
                DataManager.Trains.Add(new Train("Add new train", "Train description", 0, 0, 0));
                DataManager.Trains.Add(new Train("Train 1", "Train 1 Test", 500, 20, 0));
                DataManager.Trains.Add(new Train("Train 2", "Train 2 Test", 1000, 50, 2));
                DataManager.Trains.Add(new Train("Train 3", "Train 3 Test", 100, 5, 1));
            }
            Trains = new List<ListedTrain>();
            foreach (var Train in DataManager.Trains)
            {
                Trains.Add(new ListedTrain(Train, Icons[Train.Icon]));
            }
        }

        public void SaveTrainButton()
        {
            if (Title == null || Description == null || Speed == null || Acceleration == null)
            {
                return;
            }
            Train newTrain = new Train(Title, Description, Decimal.ToSingle((decimal)Speed), Decimal.ToSingle((decimal)Acceleration), IconIndex);
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
            Speed = DataManager.CurrentTrain.MaxSpeed;
            Acceleration = DataManager.CurrentTrain.Acceleration;
            IconIndex = DataManager.CurrentTrain.Icon;

            // Notify the UI about the property changes
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Speed));
            RaisePropertyChanged(nameof(Acceleration));
            RaisePropertyChanged(nameof(IconIndex));
        }
    }
}
