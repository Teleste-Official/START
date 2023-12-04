using Mapsui.Projections;
using Mapsui;
using SmartTrainApplication.Models;
using SmartTrainApplication.Data;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Projections;
using NetTopologySuite.Operation.Distance;

namespace SmartTrainApplication.Data
{
    internal class Simulation
    {
        public static SimulationData? LatestSimulation = null;

        public static void PreprocessRoute()
        {
            // Preprocess the route to calculate the distance and add info (turns, speedlimitations) for simulation -Metso

  

             List<string> turnPoints = DataManager.GetTurnStrings();
             List<string> turnStrings = DataManager.AddTurns(turnPoints);



            return;
        }

        public static void RunSimulation() // See if async would be more preferrable for this -Metso
        {
            bool IsRunning = true;

            // Const test variables for train & simulation info
            const float acceleration = 2;
            const float maxSpeed = 50;
            const float interval = 1;

            List<TickData> AllTickData = new List<TickData>();

            TrainRoute route = DataManager.CurrentTrainRoute;
            List<MPoint> points = new List<MPoint>();

            foreach (var coord in route.Coords)
            {
                double X = double.Parse(coord.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
                double Y = double.Parse(coord.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);
                var point = SphericalMercator.ToLonLat(new MPoint(X, Y));
                points.Add(point);
            }

            double routeLengthMeters = RouteGeneration.CalculateRouteLength(points);
            TickData tickData = new TickData(points[0].Y, points[0].X, false, 0, false, 0, 0);
            int pointIndex = 1;
            double nextLat = points[pointIndex].Y;
            double nextLon = points[pointIndex].X;

            double travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, interval, acceleration);
            double pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);

            while (IsRunning)
            {
                // Iterate through the route and save all data
                // In each iteration move the train based on time, velocity and acceleration
                // Thus new calculations for each of these need to be done first in every iteration
                // -Metso
                while (travelDistance > pointDistance)
                {
                    // Stop loop in trying to go past last point
                    if (pointIndex ==  points.Count-1)
                    {
                        IsRunning = false; // Remove this after functionality is added. -Metso
                        travelDistance = pointDistance;
                        break;
                    }

                    travelDistance -= pointDistance;
                    tickData.distanceMeters += (float)pointDistance;
                    tickData.latitudeDD = nextLat;
                    tickData.longitudeDD = nextLon;
                    pointIndex++;
                    nextLat = points[pointIndex].Y;
                    nextLon = points[pointIndex].X;

                    pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
                }

                (tickData.longitudeDD, tickData.latitudeDD) = RouteGeneration.CalculateNewTrainPoint(tickData.longitudeDD, tickData.latitudeDD, nextLon, nextLat, travelDistance, pointDistance);
                tickData.distanceMeters += (float)travelDistance;
                tickData.trackTimeSecs += interval;

                if(tickData.distanceMeters > routeLengthMeters)
                {
                    //Debug.WriteLine(tickData.distanceMeters);
                    //Debug.WriteLine(routeLengthMeters);
                    break;
                }

                // Data to be saved in Ticks:
                // double _latitudeDD, double _longitudeDD, bool _isGpsFix, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs 
                AllTickData.Add(new TickData(tickData.latitudeDD, tickData.longitudeDD, false, tickData.speedKmh, false, tickData.distanceMeters, tickData.trackTimeSecs));

                // Test tick data
                //AllTickData.Add(new TickData(0, 0, false, 0, false, 0, 0));
                //AllTickData.Add(new TickData(0, 0, false, 0, false, 0, 0));
                
                if (IsRunning)
                {
                    pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
                    travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, interval, acceleration);

                    if ((tickData.speedKmh + RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration)) > maxSpeed)
                    {
                        tickData.speedKmh = maxSpeed;
                        // tickData.speedKmh = SlowZone.CalculateSlowZone(pointDistance, tickData.speedKmh, acceleration, maxSpeed);
                    }
                    else
                    {
                        tickData.speedKmh += RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration);
                       // tickData.speedKmh = SlowZone.CalculateSlowZone(pointDistance, tickData.speedKmh, acceleration, maxSpeed);
                    }
                }
            }

            AllTickData.RemoveAt(AllTickData.Count - 1);
            SimulationData newSim = new SimulationData("Test", AllTickData);

            // Save the simulated run into a file. Name could be *TrainName*_*RouteName*_*DateTime*.json
            // SimulationRun file could also host the train and route data for playback in the future -Metso
            FileManager.SaveSimulationData(newSim);
            LatestSimulation = newSim;

            LayerManager.CreateAnimationLayer();

            // Possibly return the simulation data for playback
            return;
        }

        public static async Task StartSimulationPlayback()
        {
            LayerManager.CreateAnimationLayer();
            // Read tickdata from simulation data in set intervals and move a bitmap on the map accordingly
            foreach (TickData tick in LatestSimulation.TickData)
            {
                
            }
            return;
        }
    }
}
