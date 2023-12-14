using SmartTrainApplication.Data;
using System.Collections.Generic;

namespace SmartTrainApplication.Models
{
    /// <summary>
    /// The TickData, TrainRoute and Train data generated from TrainRoutes in Simulation
    /// <list type="bullet">
    /// <item>(string) Name</item>
    /// <item>(List of TickData) TickData</item>
    /// <item>(Train) Train</item>
    /// <item>(TrainRoute) TrainRoute</item>
    /// </list>
    /// </summary>
    public class SimulationData
    {
        public string Name { get; set; } // Datetime? Might not need this one, this information could be in the files name itself -Metso

        public List<TickData> TickData { get; set; }

        public Train Train { get; set; } // The train the simulation was performed with
        public TrainRoute TrainRoute { get; set; } // The route the simulation was performed with

        public SimulationData() { }

        public SimulationData(string name, List<TickData> tickData)
        {
            Name = name;
            TickData = tickData;
            //Train = DataManager.CurrentTrain;
            Train = new Train("Test", "Testing Train", 0, 0, 0); // For testing only -Metso
            TrainRoute = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
        }
    }
}

