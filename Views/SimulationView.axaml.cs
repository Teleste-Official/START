using Avalonia.Controls;
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
                viewModel.SetStopsToUI();
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

            DataManager.CurrentTrain = DataManager.Trains[comboBox.SelectedIndex];
            Debug.WriteLine("Current train " + DataManager.CurrentTrain.Id);
        }
    }

    private void ClickStop(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        TextBlock textBlock = sender as TextBlock;
        Debug.WriteLine("Clicked! " + textBlock.Text.ToString());
    }

    private void CheckStop(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CheckBox checkBox = sender as CheckBox;
        Debug.WriteLine("Checked! " + checkBox.Name);

        if (DataContext is SimulationViewModel viewModel)
        {
            viewModel.SetBool(checkBox.Name, true);
        }
    }

    private void UncheckStop(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CheckBox checkBox = sender as CheckBox;
        Debug.WriteLine("Unchecked! " + checkBox.Name);

        if (DataContext is SimulationViewModel viewModel)
        {
            viewModel.SetBool(checkBox.Name, false);
        }
    }
}