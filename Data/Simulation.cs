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
            Dictionary<RouteCoordinate, bool> TurnPoints = new SimulatedTrainRoute(DataManager.CurrentTrainRoute).RouteTurnPoints;

            foreach (KeyValuePair<RouteCoordinate, bool> kvp in TurnPoints)
            {
                for (int i = 0; i < DataManager.CurrentTrainRoute.Coords.Count - 2; i++)
                {
                    RoutePoint point1 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i].Longitude, DataManager.CurrentTrainRoute.Coords[i].Latitude);
                    RoutePoint point2 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i + 1].Longitude, DataManager.CurrentTrainRoute.Coords[i + 1].Latitude);
                    RoutePoint point3 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i + 2].Longitude, DataManager.CurrentTrainRoute.Coords[i + 2].Latitude);

                    bool turn = TurnCalculation.CalculateTurn(point1, point2, point3);

                    TurnPoints[DataManager.CurrentTrainRoute.Coords[i + 1]] = turn;
                }
            }
            RunSimulation(TurnPoints);
            return;
        }

        /// <summary>
        /// Generate the TickData for use in simulation playback and simulation export data
        /// </summary>
        public static void RunSimulation(Dictionary<RouteCoordinate, bool> TurnPoints) // See if async would be more preferrable for this -Metso
        {
            bool IsRunning = true;

            // Const test variables for train & simulation info
            const float acceleration = 2;
            const float maxSpeed = 50;
            const float interval = 1;
            /*double distance;
            double slowZoneDistance = 100000000;
            double kvpKeyLongitude = 0.0;
            double kvpKeyLatitude = 0.0;
            double point1Longitude = 0.0;
            double point1Latitude = 0.0;
            RoutePoint point1 = new RoutePoint();
            RoutePoint point2 = new RoutePoint();
            RoutePoint point3 = new RoutePoint();*/
            bool turn = false;

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
            bool isGpsFix = true;

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
                    if (pointIndex == points.Count - 1)
                    {
                        IsRunning = false; // Remove this after functionality is added. -Metso
                        travelDistance = pointDistance;
                        break;
                    }

                    travelDistance -= pointDistance;
                    tickData.distanceMeters += (float)pointDistance;
                    tickData.latitudeDD = nextLat;
                    tickData.longitudeDD = nextLon;

                    if (route.Coords[pointIndex].Type == "TUNNEL_ENTRANCE" || route.Coords[pointIndex].Type == "TUNNEL_ENTRANCE_STOP")
                    {
                        isGpsFix = !isGpsFix;
                    }

                    turn = false;

                    pointIndex++;
                    nextLat = points[pointIndex].Y;
                    nextLon = points[pointIndex].X;

                    pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
                }

                (tickData.longitudeDD, tickData.latitudeDD) = RouteGeneration.CalculateNewTrainPoint(tickData.longitudeDD, tickData.latitudeDD, nextLon, nextLat, travelDistance, pointDistance);
                tickData.distanceMeters += (float)travelDistance;
                tickData.trackTimeSecs += interval;
       
                if (tickData.distanceMeters > routeLengthMeters)
                {
                    //Debug.WriteLine(tickData.distanceMeters);
                    //Debug.WriteLine(routeLengthMeters);
                    break;
                }

                // Data to be saved in Ticks:
                // double _latitudeDD, double _longitudeDD, bool _isGpsFix, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs 
                AllTickData.Add(new TickData(tickData.latitudeDD, tickData.longitudeDD, isGpsFix, tickData.speedKmh, false, tickData.distanceMeters, tickData.trackTimeSecs));

                // Loop trough TurnPoints dictionary to get turn points for slow zone
                /*foreach (KeyValuePair<RouteCoordinate, bool> kvp in TurnPoints)
                {
                    for (int i = 0; i < DataManager.CurrentTrainRoute.Coords.Count - 2; i++)
                    {
                        kvpKeyLongitude = double.Parse(kvp.Key.Longitude.Replace(".", ","));
                        kvpKeyLatitude = double.Parse(kvp.Key.Latitude.Replace(".", ","));
                        point1Longitude = double.Parse(DataManager.CurrentTrainRoute.Coords[i].Longitude.Replace(".", ","));
                        point1Latitude = double.Parse(DataManager.CurrentTrainRoute.Coords[i].Latitude.Replace(".", ","));

                        point1 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i].Longitude, DataManager.CurrentTrainRoute.Coords[i].Latitude);
                        point2 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i + 1].Longitude, DataManager.CurrentTrainRoute.Coords[i + 1].Latitude);
                        point3 = new RoutePoint(DataManager.CurrentTrainRoute.Coords[i + 2].Longitude, DataManager.CurrentTrainRoute.Coords[i + 2].Latitude);

                        turn = kvp.Value;

                        if (IsRunning)
                        {
                            pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
                            travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, interval, acceleration);

                            distance = RouteGeneration.CalculatePointDistance(point1Longitude, kvpKeyLongitude, point1Latitude, kvpKeyLatitude);

                            if (turn)
                            {

                                slowZoneDistance = distance + 100;
                                // Calculate slow zone speed with current distance and slow zone distance
                                tickData.speedKmh = SlowZone.CalculateSlowZone(distance, slowZoneDistance, tickData.speedKmh, acceleration, maxSpeed);

                                float maxRadius = 180;

                                // New speed based on curve radius
                                tickData.speedKmh = TurnCalculation.CalculateSpeedByRadius(point1, point2, point3, tickData.speedKmh, maxRadius);

                            }
                            else
                            {
                                slowZoneDistance = 100000000;
                            }

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
                  //  Debug.WriteLine(tickData.speedKmh);

                }*/
                if (IsRunning)
                {
                    pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
                    travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, interval, acceleration);

                    turn = TurnPoints.Values.ElementAt(pointIndex);

                    // If the current/"next" RoutePoint is marked as turn
                    if (turn)
                    {
                        // If the distance to next RoutePoint is shorter than
                        // double the stopping distance (distance needed to decelerate from maxSpeed to turnSpeed),
                        // decelerate to turnSpeed (currently 20km/h) and coast at that speed until turn's RoutePoint
                        if (pointDistance < 2 * RouteGeneration.CalculateStoppingDistance(maxSpeed, 20f, -acceleration))
                        {
                            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, -acceleration), 20f);
                        }
                        // Else accelerate normally.
                        else
                        {
                            tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration), maxSpeed);
                        }
                    }
                    else
                    {
                        // If the train isn't a stopping distance (distance needed to decelerate from maxSpeed to 0)
                        // away from the route end (plus some wiggle room), keep accelerating to train's max speed.
                        if (tickData.distanceMeters < routeLengthMeters - 1.5 * RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration))
                        {
                            tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, acceleration), maxSpeed);
                        }
                        // Else start decelerating.
                        // With this the train coast at 7.2km/h for a few seconds at the end before stopping
                        else
                        {
                            //7.2km/h is the speed from which the train can come to a stop in one second "tick" with the -2m/s^2 deceleration
                            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, interval, -acceleration), 7.2f);
                        }
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
