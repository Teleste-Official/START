using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    internal class Simulation
    {
        public static SimulationData? LatestSimulation = null;

        public static void PreprocessRoute()
        {
            // Preprocess the route to calculate the distance and add info (turns, speedlimitations) for simulation -Metso

            return;
        }

        public static void RunSimulation() // See if async would be more preferrable for this -Metso
        {
            bool IsRunning = true;
            List<TickData> AllTickData = new List<TickData>();

            while (IsRunning)
            {
                // Iterate through the route and save all data
                // In each iteration move the train based on time, velocity and acceleration
                // Thus new calculations for each of these need to be done first in every iteration
                // -Metso

                // Data tobe saved in Ticks:
                // double _latitudeDD, double _longitudeDD, bool _isGpsFix, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs 
                // AllTickData.Add(new TickData())

                // Test tick data
                AllTickData.Add(new TickData(0, 0, false, 0, false, 0, 0));
                AllTickData.Add(new TickData(0, 0, false, 0, false, 0, 0));

                // Stop the loop when on the last point
                IsRunning = false; // Remove this after functionality is added. -Metso
            }

            SimulationData newSim = new SimulationData("Test", AllTickData);

            // Save the simulated run into a file. Name could be *TrainName*_*RouteName*_*DateTime*.json
            // SimulationRun file could also host the train and route data for playback in the future -Metso
            FileManager.SaveSimulationData(newSim);

            // Possibly return the simulation data for playback
            return;
        }

        public static void StartSimulationPlayback()
        {
            // Read tickdata from simulation data in set intervals and move a bitmap on the map accordingly
            return;
        }
    }
}
