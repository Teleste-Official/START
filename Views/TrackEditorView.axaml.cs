using Avalonia.Controls;
using SmartTrainApplication.Data;
using SmartTrainApplication.Views;

namespace SmartTrainApplication;

public partial class TrackEditorView : UserControl
{
    public TrackEditorView()
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

            if (DataContext is TrackEditorViewModel viewModel)
            {
                // If we are adding a new line, switch the combobox to it
                if (viewModel.AddingNew)
                    comboBox.SelectedIndex = DataManager.TrainRoutes.Count - 1;

                viewModel.Routes = DataManager.TrainRoutes;
                DataManager.CurrentTrainRoute = DataManager.TrainRoutes[comboBox.SelectedIndex];
                viewModel.SetValuesToUI();
                LayerManager.SwitchRoute();
            }
        }
    }
}