using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class SimulationData
    {
        string Name { get; set; } // Datetime?

        List<TickData> TickData { get; set; }

        public SimulationData() { }

        public SimulationData(string name, List<TickData> tickData)
        {
            Name = name;
            TickData = tickData;
        }
    }
}

