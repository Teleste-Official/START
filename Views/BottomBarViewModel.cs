#region

using NLog;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Views;

public class BottomBarViewModel : ViewModelBase {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  public BottomBarViewModel() {
  }

  public void SaveButton() {
    Logger.Debug("SaveButton pressed");

    foreach (TrainRoute route in DataManager.TrainRoutes) {
      FileManager.SaveSpecific(route);
    }

    foreach (Train train in DataManager.Trains) {
      FileManager.SaveTrain(train);
    }
  }

  public void SaveAsButton() {
    switch (FileManager.CurrentView) {
      case "Route":
        FileManager.Export(MainWindow.TopLevel, "Route");
        break;

      case "Train":
        FileManager.Export(MainWindow.TopLevel, "Train");
        break;

      case "Simulation":
        FileManager.Export(MainWindow.TopLevel, "Simulation");
        break;

      case "Settings":
        FileManager.Export(MainWindow.TopLevel, "Settings");
        break;

      default:
        break;
    }
  }
}