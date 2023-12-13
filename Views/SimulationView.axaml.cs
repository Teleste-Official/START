using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SmartTrainApplication.Data;
using SmartTrainApplication.Views;
using System.Diagnostics;

namespace SmartTrainApplication;

public partial class SimulationView : UserControl
{
    public SimulationView()
    {
        InitializeComponent();
    }

    public void TrackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle the selection changed event here
        if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
        {
            if (DataManager.TrainRoutes.Count == 0)
                return;

            if (DataContext is SimulationViewModel viewModel)
            {
                viewModel.Routes = DataManager.TrainRoutes;
                DataManager.CurrentTrainRoute = DataManager.TrainRoutes[comboBox.SelectedIndex];
                LayerManager.SwitchRoute();
                Debug.WriteLine("Current route " + DataManager.CurrentTrainRoute.Id);
            }
        }
    }

    public void TrainComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle the selection changed event here
        if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
        {
            if (DataManager.Trains.Count == 0)
                return;

            if (DataContext is SimulationViewModel viewModel)
            {
                DataManager.CurrentTrain = DataManager.Trains[comboBox.SelectedIndex];
                Debug.WriteLine("Current train " + DataManager.CurrentTrain.Id);
            }
        }
    }
}