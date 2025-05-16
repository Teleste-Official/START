#region

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Projections;
using NLog;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Data;

/// <summary>
/// Functions used for generating and simulating TrainRoutes
/// </summary>
internal class Simulation {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  
  public static SimulationData? LatestSimulation = null;

  // This is used by the animation layer... do something about this.
  public static float TickLength = 1.0f;

  /// <summary>
  /// Run Preprocess functions for TrainRoutes before simulating the TrainRoute
  /// </summary>
  private class SimulatedTrainRoute : TrainRoute {
    public Dictionary<RouteCoordinate, bool> RouteTurnPoints;
    public Dictionary<RouteCoordinate, bool> RouteStops;

    public SimulatedTrainRoute(TrainRoute route) {
      Name = route.Name;
      Coords = route.Coords;
      RouteTurnPoints = route.Coords.ToDictionary(x => x, x => false);
      RouteStops = route.Coords.ToDictionary(x => x, x => false);
    }
  }

  private static MPoint CreateMPointFromRouteCoordinate(RouteCoordinate coord) {
    double x = double.Parse(coord.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
    double y = double.Parse(coord.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);
    MPoint mPoint = SphericalMercator.ToLonLat(new MPoint(x, y));
    return mPoint;
  }

  private static List<MPoint> GenerateMPointListFromRouteCoordinates(List<RouteCoordinate> coords) {
    List<MPoint> mPoints = new List<MPoint>();

    foreach (RouteCoordinate? coord in coords) {
      //double x = double.Parse(coord.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
      //double y = double.Parse(coord.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);
      //MPoint? mPoint = SphericalMercator.ToLonLat(new MPoint(x, y));
      //Logger.Debug($"Latitude={coord.Latitude}, Longitude={coord.Longitude}, mPoint={mPoint}, x={x}, y={y}");
      mPoints.Add(CreateMPointFromRouteCoordinate(coord));
    }
    return mPoints;
  }

  public static SimulationData GenerateSimulationData2(Dictionary<RouteCoordinate, bool> stopsDictionary,
    Train trainToBeSimulated, TrainRoute routeToBeSimulated, float tickLength) {

    List<TickData> generatedSimulationTicks =  new List<TickData>();
    double routeLengthMeters = 0;

    List<MPoint> mPointsList = GenerateMPointListFromRouteCoordinates(routeToBeSimulated.Coords);

    foreach (MPoint mPoint in mPointsList) {
      TickData newTick = new TickData();

    }

    SimulationData newSim = new(generatedSimulationTicks, trainToBeSimulated, routeToBeSimulated);
    newSim.RouteLengthMeters = routeLengthMeters;
    LatestSimulation = newSim;
    Logger.Debug($"Simulation had tickData.Count={generatedSimulationTicks.Count}, mPointsList.Count={mPointsList.Count}");
    return newSim;
  }

  private static List<(double, bool)> stoppingDistances;

  private static bool ShouldDecelerateAtDistance(double distanceTravelled, float currentSpeed, float acceleration) {

    for (int i=0; i < stoppingDistances.Count; i++) {
      // Stop has been visited
      if (stoppingDistances[i].Item2) {
        continue;
      }

      double distanceToNextStop = stoppingDistances[i].Item1-distanceTravelled;
      double stoppingDistance = 1.75 * RouteGeneration.CalculateStoppingDistance(currentSpeed, 0f, acceleration);

      if (distanceToNextStop <= stoppingDistance) {
        //stoppingDistances[i] = (stoppingDistances[i].Item1, true);
        Logger.Debug($"should deaccelerate, travelled {distanceTravelled}, distance to next stop {distanceToNextStop}, speed {currentSpeed}");
        return true;
      }

      break;
    }
    return false;
  }

  private static double GetDistanceToNextStop(float distanceTravelled) {

    for (int i=0; i < stoppingDistances.Count; i++) {
      // Stop has been visited
      if (stoppingDistances[i].Item2) {
        continue;
      }

      double distanceToNextStop = stoppingDistances[i].Item1-distanceTravelled;
      return distanceToNextStop;
    }

    return 0;
  }

  // MPoints coming from this are in the actual correct format that will be in the simulator json.
  // TODO Make this make some sense
  // TODO start with distance 0
  public static SimulationData GenerateSimulationData(Dictionary<RouteCoordinate, bool> stopsDictionary, Train trainToBeSimulated, TrainRoute routeToBeSimulated, float tickLength) {

    // Hack stuff for now
    TickLength = tickLength;

    float acceleration = trainToBeSimulated.Acceleration;
    float maxSpeed = trainToBeSimulated.MaxSpeed;

    // Preprocess the route to calculate the distance and add info (turns, speedlimitations) for simulation -Metso
    Dictionary<RouteCoordinate, bool> turnPoints = new SimulatedTrainRoute(routeToBeSimulated).RouteTurnPoints;
    Dictionary<RouteCoordinate, bool> stopPoints = new SimulatedTrainRoute(routeToBeSimulated).RouteStops;

    int turns = 0;
    foreach (KeyValuePair<RouteCoordinate, bool> kvp in turnPoints) {
      for (int i = 0; i < routeToBeSimulated.Coords.Count - 2; i++) {
        RoutePoint? point1 = new(routeToBeSimulated.Coords[i].Longitude, routeToBeSimulated.Coords[i].Latitude);
        RoutePoint? point2 = new(routeToBeSimulated.Coords[i + 1].Longitude, routeToBeSimulated.Coords[i + 1].Latitude);
        RoutePoint? point3 = new(routeToBeSimulated.Coords[i + 2].Longitude, routeToBeSimulated.Coords[i + 2].Latitude);

        turnPoints[routeToBeSimulated.Coords[i + 1]] = TurnCalculation.CalculateTurn(point1, point2, point3);
      }
    }

    foreach (KeyValuePair<RouteCoordinate, bool> kvp in stopsDictionary) {
      stopPoints[kvp.Key] = kvp.Value;
    }

    Logger.Debug($"Simulation started with:" +
                 $"\ntrainToBeSimulated={trainToBeSimulated.Name}" +
                 $"\nrouteToBeSimulated={routeToBeSimulated.Name}" +
                 $"\nacceleration={acceleration}" +
                 $"\nmaxSpeed={maxSpeed}" +
                 $"\ntickLength={tickLength}");

    Logger.Debug($"stopPoints.Count: {stopPoints.Count}, stopsDictionary.Count: {stopsDictionary.Count}, turnPoints.Count: {turnPoints.Count}, turns:  {turns}");
    foreach (KeyValuePair<RouteCoordinate,bool> sp in stopsDictionary) {
      //Logger.Debug($"Stop:  {sp.Key.StopName} -> {sp.Value}");
    }


    List<TickData> generatedSimulationTicks = new();
    // MPonts that correspond to the RouteCoordinates in selected route.
    List<MPoint> mPointsList = GenerateMPointListFromRouteCoordinates(routeToBeSimulated.Coords);

    double routeLengthMeters = RouteGeneration.CalculateRouteLength(mPointsList);

    TickData? tickData = new(mPointsList[0].Y, mPointsList[0].X, false, 0, false, 0, 0);

    int pointIndex = 1;
    double nextLat = mPointsList[pointIndex].Y;
    double nextLon = mPointsList[pointIndex].X;
    bool isGpsFix = true;

    double travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, tickLength, acceleration);
    double pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);


    bool isRunning = true;



    Logger.Debug($"Calculating dist");

    double cumulativeDistance = 0;
    // key=coord, value distance travelled at that point
    Dictionary<RouteCoordinate, double> od = new();

    stoppingDistances = new();
    for (int i = 0; i < routeToBeSimulated.Coords.Count; i++) {
      if (i + 1 >= routeToBeSimulated.Coords.Count) {

        break;
      }

      RouteCoordinate coord1 = routeToBeSimulated.Coords[i];
      RouteCoordinate coord2 = routeToBeSimulated.Coords[i+1];

      if (stopPoints.Values.ElementAt(i)) {
        od.Add(coord1, cumulativeDistance);
        stoppingDistances.Add((cumulativeDistance, false));
      }

      //double lon1 = double.Parse(coord1.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
      //double lat1 = double.Parse(coord1.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);
      //double lon2 = double.Parse(coord2.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
      //double lat2 = double.Parse(coord2.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);

      MPoint mPoint1 = CreateMPointFromRouteCoordinate(coord1);
      MPoint mPoint2 = CreateMPointFromRouteCoordinate(coord2);

      double lon1 = mPoint1.X;
      double lon2 = mPoint2.X;
      double lat1 = mPoint1.Y;
      double lat2 = mPoint2.Y;

      cumulativeDistance += RouteGeneration.CalculatePointDistance(lon1, lon2, lat1, lat2);



      Logger.Debug($"dist: {cumulativeDistance}");

      //Logger.Debug($"lon1={lon1}, lon2={lon2}, lat1={lat1}, lat2={lat2}, pointDistance={RouteGeneration.CalculatePointDistance(lon1, lon2, lat1, lat2)}", lon1, lon2, lat1, lat2 );
    }

    foreach (KeyValuePair<RouteCoordinate, double> kvp in od) {
      Logger.Debug($"coord: {kvp.Key}={kvp.Value}");
    }
    Logger.Debug("Done Calculating dist");


    while (isRunning) {
      // Iterate through the route and save all data
      // In each iteration move the train based on time, velocity and acceleration
      // Thus new calculations for each of these need to be done first in every iteration
      // -Metso


      //Logger.Debug($"travel distance: {travelDistance}, point Distance: {pointDistance}");
      // Basically this increases the lat+lon and distanceMeters until the point distance equals travel distance.
      while (travelDistance > pointDistance) {
        // Stop loop in trying to go past last point
        if (pointIndex == mPointsList.Count - 1) {
          isRunning = false; // Remove this after functionality is added. -Metso
          travelDistance = pointDistance;
          break;
        }

        travelDistance -= pointDistance;
        tickData.distanceMeters += (float)pointDistance;
        tickData.latitudeDD = nextLat;
        tickData.longitudeDD = nextLon;

        if (routeToBeSimulated.Coords[pointIndex].Type == "TUNNEL_ENTRANCE" ||
            routeToBeSimulated.Coords[pointIndex].Type == "TUNNEL_ENTRANCE_STOP") {
          isGpsFix = !isGpsFix;
        }

        pointIndex++;
        nextLat = mPointsList[pointIndex].Y;
        nextLon = mPointsList[pointIndex].X;

        pointDistance =
          RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
        //Logger.Debug($"pointDistance: {pointDistance}");
      }

      //Logger.Debug($"AFTER travel distance: {travelDistance}, point Distance: {pointDistance}");

      (tickData.longitudeDD, tickData.latitudeDD) = RouteGeneration.CalculateNewTrainPoint(tickData.longitudeDD,
        tickData.latitudeDD, nextLon, nextLat, travelDistance, pointDistance);
      tickData.distanceMeters += (float)travelDistance;
      tickData.trackTimeSecs += tickLength;

      if (tickData.distanceMeters > routeLengthMeters)
        //Logger.Debug(tickData.distanceMeters);
        //Logger.Debug(routeLengthMeters);
        break;

      // Data to be saved in Ticks:
      // double _latitudeDD, double _longitudeDD, bool _isGpsFix, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs
      generatedSimulationTicks.Add(new TickData(tickData.latitudeDD, tickData.longitudeDD, isGpsFix, tickData.speedKmh, false,
        tickData.distanceMeters, tickData.trackTimeSecs));

      if (isRunning) {
        pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
        travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, tickLength, acceleration);
        double distanceToNextStop = GetDistanceToNextStop(tickData.distanceMeters);
        //Logger.Debug($"PointDistance: {pointDistance} TravelDistance: {travelDistance} Distance: {tickData.distanceMeters}");

        bool turn = turnPoints.Values.ElementAt(pointIndex);
        bool stop = ShouldDecelerateAtDistance(tickData.distanceMeters, tickData.speedKmh, -acceleration);//stopPoints.Values.ElementAt(pointIndex);
        //Logger.Debug($"travelDistance: {tickData.distanceMeters}, stopped: {stop}");
        // If the current/"next" RoutePoint is marked as stop
        // TODO this produces error if there are two stops back to back without any route point in between them.
        if (stop) {
          // If the distance to next RoutePoint is shorter than
          // 1.75 times the stopping distance (distance needed to decelerate from maxSpeed to 0),
          // decelerate to 7.2km/h and coast at that speed until turn's RoutePoint
          if (pointDistance < 1.75 * RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration)) {

            // TODO decelerate with tickLength=1 here also remember to fix the passed time somewhere else.
            //7.2km/h is the speed from which the train can come to a stop in one second "tick" with the -2m/s^2 deceleration
            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, tickLength, -acceleration),
              7.2f);

            // TODO calculate the actual distance to the stop, not point to point distance
            // If within 3 meters from the RoutePoint, stop for 10 seconds.
            //if (pointDistance < 3) {
            if (distanceToNextStop < 3) {

              int idx = 0;


              while (idx < stoppingDistances.Count) {
                if (stoppingDistances[idx].Item2) {
                  idx++;
                  continue;
                }

                double distanceTravelledAtStop = stoppingDistances[idx].Item1;


                if (tickData.distanceMeters <= distanceTravelledAtStop) {
                  stoppingDistances[idx] = (stoppingDistances[idx].Item1, true);
                  Logger.Debug($"STOP AT: {tickData.distanceMeters}");
                  for (int i = 1; i <= 10; i++) {
                    tickData.trackTimeSecs += tickLength;
                    generatedSimulationTicks.Add(new TickData(tickData.latitudeDD, tickData.longitudeDD, isGpsFix, 0,
                      true,
                      tickData.distanceMeters, tickData.trackTimeSecs));
                  }
                }

                break;
              }
            }


          }
          // Else accelerate normally.
          else {
            tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, tickLength, acceleration),
              maxSpeed);
          }
        }
        // If the current/"next" RoutePoint is marked as turn
        else if (turn) {
          // Calculate turn speed using turn's radius
          RoutePoint? point1 = new(turnPoints.Keys.ElementAt(pointIndex - 1).Longitude,
            turnPoints.Keys.ElementAt(pointIndex - 1).Latitude);
          RoutePoint? point2 = new(turnPoints.Keys.ElementAt(pointIndex).Longitude,
            turnPoints.Keys.ElementAt(pointIndex).Latitude);
          RoutePoint? point3 = new(turnPoints.Keys.ElementAt(pointIndex + 1).Longitude,
            turnPoints.Keys.ElementAt(pointIndex + 1).Latitude);
          float turnSpeed = TurnCalculation.CalculateTurnSpeedByRadius(point1, point2, point3, maxSpeed, 180);

          // If the distance to next RoutePoint is shorter than
          // double the stopping distance (distance needed to decelerate from maxSpeed to turnSpeed),
          // decelerate to turnSpeed and coast at that speed until turn's RoutePoint
          if (pointDistance < 2 * RouteGeneration.CalculateStoppingDistance(maxSpeed, turnSpeed, -acceleration)) {
            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, tickLength, -acceleration),
              turnSpeed);

          } else { // Else accelerate normally.
            tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, tickLength, acceleration),
              maxSpeed);
          }

        }
        else {
          // If the train isn't a stopping distance (distance needed to decelerate from maxSpeed to 0)
          // away from the route end (plus some wiggle room), keep accelerating to train's max speed.
          if (tickData.distanceMeters < routeLengthMeters -
              1.75 * RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration)) {
            tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, tickLength, acceleration), maxSpeed);
          }
          // Else start decelerating.
          // With this the train coast at 7.2km/h for a few seconds at the end before stopping
          else {
            //7.2km/h is the speed from which the train can come to a stop in one second "tick" with the -2m/s^2 deceleration
            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, tickLength, -acceleration),
              7.2f);
          }

        }
      }
    }

    generatedSimulationTicks.RemoveAt(generatedSimulationTicks.Count - 1);
    //change last data to have 0 speed and open doors
    // TBD does this make sense...
    generatedSimulationTicks[generatedSimulationTicks.Count - 1].speedKmh = 0f;
    generatedSimulationTicks[generatedSimulationTicks.Count - 1].doorsOpen = true;

    SimulationData newSim = new(generatedSimulationTicks, trainToBeSimulated, routeToBeSimulated);
    newSim.RouteLengthMeters = routeLengthMeters;
    LatestSimulation = newSim;
    Logger.Debug($"Simulation had tickData.Count={generatedSimulationTicks.Count}, mPoints.Count={mPointsList.Count}, route length meters={routeLengthMeters}");
    return newSim;

  }

  public static void StartAnimationPlayback() {
    // TODO do something not so horrible here...
    LayerManager.RemoveAnimationLayer();
    LayerManager.CreateAnimationLayer();
  }

  public static void StopAnimationPlayback() {
    // TODO do something not so horrible here...
    LayerManager.RemoveAnimationLayer();
  }

  /// <summary>
  /// Runs the generated TickData / SimulationData in visual playback on the map
  /// </summary>
  /// <returns>Creates an async Task of Simulation animation</returns>
  private static async Task StartSimulationPlayback() {
    // TODO use this or something like this...
    LayerManager.RemoveAnimationLayer();
    LayerManager.CreateAnimationLayer();
    // Read tickdata from simulation data in set intervals and move a bitmap on the map accordingly
    foreach (TickData? tick in LatestSimulation.TickData) {
    }

    return;
  }
}