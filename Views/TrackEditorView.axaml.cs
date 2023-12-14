using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapsui.Widgets;
using SmartTrainApplication.Data;
using SmartTrainApplication.Views;
using System.Diagnostics;

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
                DataManager.CurrentTrainRoute = comboBox.SelectedIndex;
                viewModel.SetValuesToUI();
                LayerManager.SwitchRoute();
                viewModel.SetStopsToUI();
            }
        }
    }

    public void StopGotFocus(object sender, GotFocusEventArgs e)
    {
        Avalonia.Controls.TextBox textBox = sender as Avalonia.Controls.TextBox;
        if (DataContext is TrackEditorViewModel viewModel)
        {
            viewModel.DrawFocusedStop(textBox.Name);
        }
        Debug.WriteLine("Focused: " + textBox.Name);
    }

    public void StopLostFocus(object sender, RoutedEventArgs e)
    {
        Avalonia.Controls.TextBox textBox = sender as Avalonia.Controls.TextBox;
        LayerManager.RemoveFocusStop();
        Debug.WriteLine("Lost focus: " + textBox.Name);
    }
}