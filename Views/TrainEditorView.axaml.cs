#region

using Avalonia.Controls;
using SmartTrainApplication.Data;
using SmartTrainApplication.Views;

#endregion

namespace SmartTrainApplication;

public partial class TrainEditorView : UserControl {
  public TrainEditorView() {
    InitializeComponent();
  }

  public void TrainComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
    // Handle the selection changed event here
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null) {
      if (DataManager.Trains.Count == 0)
        return;

      if (DataContext is TrainEditorViewModel viewModel) {
        DataManager.CurrentTrain = comboBox.SelectedIndex;
        viewModel.SetValuesToUI();
      }
    }
  }
}