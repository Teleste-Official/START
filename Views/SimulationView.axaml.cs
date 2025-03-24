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
  public SimulationView() {
    InitializeComponent();
  }

  public void TrackComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      if (DataManager.TrainRoutes.Count == 0)
        return;

      if (DataContext is SimulationViewModel viewModel) {
        viewModel.Routes = DataManager.TrainRoutes;
        DataManager.CurrentTrainRoute = comboBox.SelectedIndex;
        LayerManager.SwitchRoute();
        viewModel.SetStopsToUI();
        Logger.Debug($"Selected {DataManager.GetCurrentRoute()?.Name} for simulation");
      }
    }
  }

  public void TrainComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      if (DataManager.Trains.Count == 0)
        return;

      DataManager.CurrentTrain = comboBox.SelectedIndex;
      //Logger.Debug("Current train " + DataManager.Trains[DataManager.CurrentTrain].Id);
    }
  }

  private void ClickStop(object? sender, PointerPressedEventArgs e) {
    TextBlock? textBlock = sender as TextBlock;
    //Logger.Debug("Clicked! " + textBlock.Text.ToString());
  }

  private void EnterHoverStop(object? sender, PointerEventArgs e) {
    TextBlock? textBlock = sender as TextBlock;
    if (DataContext is SimulationViewModel viewModel) viewModel.DrawFocusedStop(textBlock.Name);
    //Logger.Debug("Hover over: " + textBlock.Name);
  }

  private void ExitHoverStop(object? sender, PointerEventArgs e) {
    TextBlock? textBlock = sender as TextBlock;
    LayerManager.RemoveFocusStop();
    //.Debug("Lost focus: " + textBlock.Name);
  }

  private void CheckStop(object? sender, RoutedEventArgs e) {
    CheckBox? checkBox = sender as CheckBox;
    //Logger.Debug("Checked! " + checkBox.Name);

    if (DataContext is SimulationViewModel viewModel) viewModel.SetBool(checkBox.Name, true);
  }

  private void UncheckStop(object? sender, RoutedEventArgs e) {
    CheckBox? checkBox = sender as CheckBox;
    //Logger.Debug("Unchecked! " + checkBox.Name);

    if (DataContext is SimulationViewModel viewModel) viewModel.SetBool(checkBox.Name, false);
  }
}