#region

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using SmartTrainApplication.Views;

#endregion

namespace SmartTrainApplication;

public partial class TrackEditorView : UserControl {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  
  private static bool _firstSelectionChange = true;

  public TrackEditorView() {
    _firstSelectionChange = true;
    InitializeComponent();
    //RouteComboBox.SelectedIndex = DataManager.CurrentTrainRoute;
  }

  public void TrackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    //Logger.Debug($"Sender: {sender.GetType()} args: {e}, selected: {DataManager.CurrentTrainRoute}");
    // Could be better I know
    if (_firstSelectionChange) {
      RouteComboBox.SelectedIndex = DataManager.CurrentTrainRoute;
      _firstSelectionChange = false;
      return;
    }
    
    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      Logger.Debug($"Selected Track: {((TrainRoute)comboBox.SelectedItem).Name}");
      if (DataManager.TrainRoutes.Count == 0) {
        return;
      }
      
      if (DataContext is TrackEditorViewModel viewModel) {
        // If we are adding a new line, switch the combobox to it
        if (viewModel.AddingNew) comboBox.SelectedIndex = DataManager.TrainRoutes.Count - 1;
        

        viewModel.Routes = DataManager.TrainRoutes;
        DataManager.CurrentTrainRoute = comboBox.SelectedIndex;
        viewModel.SetValuesToUI();
        LayerManager.SwitchRoute();
        viewModel.SetStopsToUI();
      }
      RouteComboBox.SelectedIndex = DataManager.CurrentTrainRoute;
    }
  }

  public void StopGotFocus(object sender, GotFocusEventArgs e) {
    TextBox? textBox = sender as TextBox;
    if (DataContext is TrackEditorViewModel viewModel) viewModel.DrawFocusedStop(textBox.Name);
  }

  public void StopLostFocus(object sender, RoutedEventArgs e) {
    TextBox? textBox = sender as TextBox;
    LayerManager.RemoveFocusStop();
  }
}