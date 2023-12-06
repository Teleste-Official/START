using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
                viewModel.Routes = DataManager.TrainRoutes;
                DataManager.CurrentTrainRoute = DataManager.TrainRoutes[comboBox.SelectedIndex];
                viewModel.SetValuesToUI();
                LayerManager.SwitchRoute();
            }
        }
    }
}