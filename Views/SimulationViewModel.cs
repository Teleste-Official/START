using SmartTrainApplication.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Views
{
    public class SimulationViewModel : ViewModelBase
    {

        public SimulationViewModel()
        {

        }

        public void RunSimulationButton()
        {
            Simulation.RunSimulation();
            return;
        }
    }
}