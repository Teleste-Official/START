#region

using System.Collections.Generic;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Models;

/// <summary>
/// The TickData, TrainRoute and Train data generated from TrainRoutes in Simulation
/// <list type="bullet">
/// <item>(List of TickData) TickData</item>
/// <item>(Train) Train</item>
/// <item>(TrainRoute) TrainRoute</item>
/// </list>
/// </summary>
public class SimulationData {
  public List<TickData> TickData { get; set; }

  public Train Train { get; set; } // The train the simulation was performed with
  public TrainRoute TrainRoute { get; set; } // The route the simulation was performed with

  public SimulationData() {
  }

  public SimulationData(List<TickData> tickData, Train simulatedTrain, TrainRoute simulatedTrainRoute) {
    TickData = tickData;
    //Train = DataManager.CurrentTrain;
    Train = simulatedTrain;
    TrainRoute = simulatedTrainRoute;
  }
}