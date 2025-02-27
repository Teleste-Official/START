#region

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Views;

#endregion

namespace SmartTrainApplication;

public partial class TrackEditorView : UserControl {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  public TrackEditorView() {
    InitializeComponent();
    RouteComboBox.SelectedIndex = DataManager.CurrentTrainRoute;
  }

  public void TrackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      if (DataManager.TrainRoutes.Count == 0)
        return;

      if (DataContext is TrackEditorViewModel viewModel) {
        // If we are adding a new line, switch the combobox to it
        if (viewModel.AddingNew) comboBox.SelectedIndex = DataManager.TrainRoutes.Count - 1;


        viewModel.Routes = DataManager.TrainRoutes;
        DataManager.CurrentTrainRoute = comboBox.SelectedIndex;
        viewModel.SetValuesToUI();
        LayerManager.SwitchRoute();
        viewModel.SetStopsToUI();
      }
    }
  }

  public void StopGotFocus(object sender, GotFocusEventArgs e) {
    var textBox = sender as TextBox;
    if (DataContext is TrackEditorViewModel viewModel) viewModel.DrawFocusedStop(textBox.Name);
    Logger.Debug("Focused: " + textBox.Name);
  }

  public void StopLostFocus(object sender, RoutedEventArgs e) {
    var textBox = sender as TextBox;
    LayerManager.RemoveFocusStop();
    Logger.Debug("Lost focus: " + textBox.Name);
  }
}