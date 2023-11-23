using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class SimulationData
    {
        string Name { get; set; } // Datetime? Might not need this one, this information could be in the files name itself -Metso

        List<TickData> TickData { get; set; }

        Train Train { get; set; } // The train the simulation was performed with
        TrainRoute TrainRoute { get; set; } // The route the simulation was performed with

        public SimulationData() { }

        public SimulationData(string name, List<TickData> tickData)
        {
            Name = name;
            TickData = tickData;
        }
    }
}

