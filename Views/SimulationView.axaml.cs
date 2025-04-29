#region

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Views;

#endregion

namespace SmartTrainApplication;

public partial class SimulationView : UserControl {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  private static bool _firstRouteSelectionChange = true;
  private static bool _firstTrainSelectionChange = true;

  public SimulationView() {
    _firstRouteSelectionChange = true;
    _firstTrainSelectionChange = true;
    InitializeComponent();
  }

  public void RouteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    if (_firstRouteSelectionChange) {
      RouteComboBox.SelectedIndex = DataManager.CurrentTrainRoute;
      MapViewControl.MoveMapToCoords(DataManager.GetCurrentRoute().Coords[0]);
      _firstRouteSelectionChange = false;
      return;
    }

    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      if (DataManager.TrainRoutes.Count == 0)
        return;

      if (DataContext is SimulationViewModel viewModel) {
        viewModel.Routes = DataManager.TrainRoutes;
        DataManager.CurrentTrainRoute = comboBox.SelectedIndex;
        MapViewControl.MoveMapToCoords(DataManager.GetCurrentRoute().Coords[0]);
        LayerManager.SwitchRoute();
        viewModel.SetStopsToUI();
        Logger.Debug($"Selected {DataManager.GetCurrentRoute()?.Name} for simulation");
      }
      RouteComboBox.SelectedIndex = DataManager.CurrentTrainRoute;
    }
  }

  public void TrainComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    if (_firstTrainSelectionChange) {
      TrainComboBox.SelectedIndex = DataManager.CurrentTrain;
      _firstTrainSelectionChange = false;
      return;
    }

    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      if (DataManager.Trains.Count == 0) {
        return;
      }

      DataManager.CurrentTrain = comboBox.SelectedIndex;
      //TrainComboBox.SelectedIndex = DataManager.CurrentTrain;
      //Logger.Debug("Current train " + DataManager.Trains[DataManager.CurrentTrain].Id);
    }
  }

  private void ClickStop(object? sender, PointerPressedEventArgs e) {
    TextBlock? textBlock = sender as TextBlock;
    //Logger.Debug("Clicked! " + textBlock.Text.ToString());
  }

  private void CheckStop(object? sender, RoutedEventArgs e) {
    CheckBox? checkBox = sender as CheckBox;
    //Logger.Debug("Checked! " + checkBox.Name);

    if (DataContext is SimulationViewModel viewModel) {
      viewModel.SetBool(checkBox.Name, true);
      viewModel.DrawFocusedStop(checkBox.Name);
    }

  }

  private void UncheckStop(object? sender, RoutedEventArgs e) {
    CheckBox? checkBox = sender as CheckBox;
    //Logger.Debug("Unchecked! " + checkBox.Name);

    if (DataContext is SimulationViewModel viewModel) {
      viewModel.SetBool(checkBox.Name, false);
      viewModel.UnDrawFocusedStop(checkBox.Name);
    }
  }
}