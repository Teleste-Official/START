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

namespace SmartTrainApplication.Data
{
    /// <summary>
    /// Functions used for generating and simulating TrainRoutes
    /// </summary>
    internal class Simulation
    {
        public static SimulationData? LatestSimulation = null;

        /// <summary>
        /// Run Preprocess functions for TrainRoutes before simulating the TrainRoute
        /// </summary>
        class SimulatedTrainRoute : TrainRoute
        {
            public Dictionary<RouteCoordinate, bool> RouteTurnPoints;
            public Dictionary<RouteCoordinate, bool> RouteStops;

            public SimulatedTrainRoute(TrainRoute _route)
            {
                Name = _route.Name;
                Coords = _route.Coords;
                RouteTurnPoints = _route.Coords.ToDictionary(x => x, x => false);
            }
        }

        public static void PreprocessRoute()
        {
            // Preprocess the route to calculate the distance and add info (turns, speedlimitations) for simulation -Metso

            Dictionary<RouteCoordinate, bool> TurnPoints = new SimulatedTrainRoute(DataManager.CurrentTrainRoute).RouteTurnPoints;

            foreach (KeyValuePair<RouteCoordinate, bool> kvp in TurnPoints)
            {
                for (int i = 0; i < DataManager.CurrentTrainRoute.Coords.Count - 2; i++)
                {
                    RoutePoint point1 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i].Longitude, DataManager.CurrentTrainRoute.Coords[i].Latitude);
                    RoutePoint point2 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i + 1].Longitude, DataManager.CurrentTrainRoute.Coords[i + 1].Latitude);
                    RoutePoint point3 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i + 2].Longitude, DataManager.CurrentTrainRoute.Coords[i + 2].Latitude);

                    bool turn = TurnCalculation.CalculateTurn(point1, point2, point3);

                    TurnPoints[DataManager.CurrentTrainRoute.Coords[i]] =  turn;
                }

                Debug.WriteLine("Key: {0}, Value: {1}", kvp.Key, kvp.Value);
            }

            return;
        }

        /// <summary>
        /// Generate the TickData for use in simulation playback and simulation export data
        /// </summary>
        public static void RunSimulation() // See if async would be more preferrable for this -Metso
        {
            bool IsRunning = true;

            PreprocessRoute();

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
            bool isGpsFix = false;

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

                    if (route.Coords[pointIndex].Type == "TUNNEL_ENTRANCE")
                    {
                        isGpsFix = !isGpsFix;
                    }

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
                AllTickData.Add(new TickData(tickData.latitudeDD, tickData.longitudeDD, isGpsFix, tickData.speedKmh, false, tickData.distanceMeters, tickData.trackTimeSecs));

                // Test tick data
                //AllTickData.Add(new TickData(0, 0, false, 0, false, 0, 0));
                //AllTickData.Add(new TickData(0, 0, false, 0, false, 0, 0));
                
                if (IsRunning)
                {
                    pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
                    travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, interval, acceleration);

                    /*
                    if (RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration) > maxSpeed)
                    {
                        tickData.speedKmh = maxSpeed;
                        // tickData.speedKmh = SlowZone.CalculateSlowZone(pointDistance, tickData.speedKmh, acceleration, maxSpeed);
                    }
                    else
                    {
                        tickData.speedKmh = RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration);
                       // tickData.speedKmh = SlowZone.CalculateSlowZone(pointDistance, tickData.speedKmh, acceleration, maxSpeed);
                    }
                    */

                    // If the train isn't a stopping distance (distance needed to go from maxspeed to 0)
                    // away from the route end (plus some wiggle room), keep accelerating to train's max speed.
                    // Else start decelerating.
                    // With this the train coast at 7.2km/h for a few seconds at the end before stopping
                    if (tickData.distanceMeters < routeLengthMeters - 1.5 * RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration))
                    {
                        tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration), maxSpeed);
                    } else {
                        //7.2km/h is the speed from which the train can come to a stop in one second "tick" with the -2m/s^2 deceleration
                        tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, -acceleration), 7.2f);
                    }
                }
            }

            AllTickData.RemoveAt(AllTickData.Count - 1);
            //change last data to have 0 speed and open doors
            AllTickData[AllTickData.Count - 1].speedKmh = 0f;
            AllTickData[AllTickData.Count - 1].doorsOpen = true;
            SimulationData newSim = new SimulationData("Test", AllTickData);

            // Save the simulated run into a file. Name could be *TrainName*_*RouteName*_*DateTime*.json
            // SimulationRun file could also host the train and route data for playback in the future -Metso
            FileManager.SaveSimulationData(newSim);
            LatestSimulation = newSim;

            LayerManager.CreateAnimationLayer();

            // Possibly return the simulation data for playback
            return;
        }

        /// <summary>
        /// Runs the generated TickData / SimulationData in visual playback on the map
        /// </summary>
        /// <returns>Creates an async Task of Simulation animation</returns>
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
