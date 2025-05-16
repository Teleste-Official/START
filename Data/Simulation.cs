#region

using System;
using System.Collections.Generic;
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
/// TODO document better... split functions and clean everything up.
/// </summary>
internal class Simulation {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  // The simulation will be created with this, if tickLength is set to something else in GUI, extra ticks will be removed.
  private readonly static float DefaultTimeInterval = 1.0f;

  // Used to calculate when the train should start accelerating/decelerating when approaching a stop.
  private readonly static double StoppingCoefficient = 1.75;

  // How close is considered "at stop"
  private readonly static double StopArrivalThreshold = 3;

  // How long the train will stop at a given platform/stop.
  private readonly static int StoppingTimeSeconds = 10;

  // How many ticks will be included with DefaultTimeInterval when TickLength is greater than 1.
  private readonly static int TickBufferAroundStops = 10;

  // key = distance travelled (meters) at a stop that the train will stop at.
  // value = given stop has already been visited.
  private static List<(double, bool)> stoppingDistances;

  public static SimulationData? LatestSimulation = null;


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
      mPoints.Add(CreateMPointFromRouteCoordinate(coord));
    }

    return mPoints;
  }


  private static bool ShouldDecelerateAtDistance(double distanceTravelled, float currentSpeed, float acceleration) {
    for (int i = 0; i < stoppingDistances.Count; i++) {
      
      // Stop has been visited
      if (stoppingDistances[i].Item2) {
        continue;
      }

      double distanceToNextStop = stoppingDistances[i].Item1 - distanceTravelled;
      double stoppingDistance =
        StoppingCoefficient * RouteGeneration.CalculateStoppingDistance(currentSpeed, 0f, acceleration);

      if (distanceToNextStop <= stoppingDistance) {
        //Logger.Debug($"travelled:{distanceTravelled} shoudStopAt:{stoppingDistances[i].Item1} distanceToNextStop:{distanceToNextStop} speed:{currentSpeed} stoppingDistance:{stoppingDistance}");
        return true;
      }

      break;
    }

    return false;
  }

  private static double GetDistanceToNextStop(float distanceTravelled) {
    for (int i = 0; i < stoppingDistances.Count; i++) {
      // Stop has been visited
      if (stoppingDistances[i].Item2) {
        continue;
      }

      double distanceToNextStop = stoppingDistances[i].Item1 - distanceTravelled;
      return distanceToNextStop;
    }

    return 0;
  }

  private static List<(double, bool)> CalculateStoppingDistances(TrainRoute routeToBeSimulated,
    Dictionary<RouteCoordinate, bool> stopPoints) {
    List<(double, bool)> _stoppingDistances = new();
    double cumulativeDistance = 0;

    for (int i = 0; i < routeToBeSimulated.Coords.Count; i++) {
      if (i + 1 >= routeToBeSimulated.Coords.Count) {
        break;
      }

      RouteCoordinate coord1 = routeToBeSimulated.Coords[i];
      RouteCoordinate coord2 = routeToBeSimulated.Coords[i + 1];

      if (stopPoints.Values.ElementAt(i)) {
        _stoppingDistances.Add((cumulativeDistance, false));
      }

      MPoint mPoint1 = CreateMPointFromRouteCoordinate(coord1);
      MPoint mPoint2 = CreateMPointFromRouteCoordinate(coord2);

      double lon1 = mPoint1.X;
      double lon2 = mPoint2.X;
      double lat1 = mPoint1.Y;
      double lat2 = mPoint2.Y;

      cumulativeDistance += RouteGeneration.CalculatePointDistance(lon1, lon2, lat1, lat2);
    }

    return _stoppingDistances;
  }

  // MPoints coming from this are in the actual correct format that will be in the simulator json.
  public static SimulationData GenerateSimulationData(Dictionary<RouteCoordinate, bool> stopsDictionary,
    Train trainToBeSimulated, TrainRoute routeToBeSimulated, float tickLength) {
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
    
    // MPonts that correspond to the RouteCoordinates in selected route.
    List<MPoint> mPointsList = GenerateMPointListFromRouteCoordinates(routeToBeSimulated.Coords);

    double routeLengthMeters = RouteGeneration.CalculateRouteLength(mPointsList);

    TickData? tickData = new(mPointsList[0].Y, mPointsList[0].X, false, 0, false, 0, 0);

    int pointIndex = 1;
    double nextLat = mPointsList[pointIndex].Y;
    double nextLon = mPointsList[pointIndex].X;
    bool isGpsFix = true;

    double travelDistance =
      RouteGeneration.CalculateTrainMovement(tickData.speedKmh, DefaultTimeInterval, acceleration);
    double pointDistance =
      RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);


    bool isRunning = true;

    stoppingDistances = CalculateStoppingDistances(routeToBeSimulated, stopPoints);


    List<TickData> generatedSimulationTicks = new();
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
      }


      (tickData.longitudeDD, tickData.latitudeDD) = RouteGeneration.CalculateNewTrainPoint(tickData.longitudeDD,
        tickData.latitudeDD, nextLon, nextLat, travelDistance, pointDistance);
      tickData.distanceMeters += (float)travelDistance;
      tickData.trackTimeSecs += DefaultTimeInterval;

      if (tickData.distanceMeters >= routeLengthMeters) {
        break;
      }


      // Data to be saved in Ticks:
      // double _latitudeDD, double _longitudeDD, bool _isGpsFix, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs
      generatedSimulationTicks.Add(new TickData(
        tickData.latitudeDD,
        tickData.longitudeDD,
        isGpsFix,
        tickData.speedKmh,
        false,
        tickData.distanceMeters,
        tickData.trackTimeSecs));

      if (isRunning) {
        pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitudeDD, nextLon, tickData.latitudeDD, nextLat);
        travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, DefaultTimeInterval, acceleration);
        double distanceToNextStop = GetDistanceToNextStop(tickData.distanceMeters);

        bool turn = turnPoints.Values.ElementAt(pointIndex);
        bool stop = ShouldDecelerateAtDistance(tickData.distanceMeters, tickData.speedKmh, -acceleration);

        
        // If the current/"next" RoutePoint is marked as stop
        if (stop) {
          // If the distance to next RoutePoint is shorter than
          // 1.75 times the stopping distance (distance needed to decelerate from maxSpeed to 0),
          // decelerate to 7.2km/h and coast at that speed until turn's RoutePoint
          if (distanceToNextStop < StoppingCoefficient * RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration)) {
            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, -acceleration), (float)3.6 * acceleration);
            
            // If within StopArrivalThreshold meters from next stop, stop for StoppingTimeSeconds seconds.
            if (distanceToNextStop <= StopArrivalThreshold) {
              int idx = 0;

              while (idx < stoppingDistances.Count) {
                if (stoppingDistances[idx].Item2) {
                  idx++;
                  continue;
                }

                stoppingDistances[idx] = (stoppingDistances[idx].Item1, true);

                for (int i = 1; i <= StoppingTimeSeconds; i++) {
                  tickData.trackTimeSecs += DefaultTimeInterval;
                  generatedSimulationTicks.Add(new TickData(tickData.latitudeDD, tickData.longitudeDD, isGpsFix, 0,
                    true,
                    tickData.distanceMeters, tickData.trackTimeSecs));
                }

                break;
              }
            }
          }
          // Else accelerate normally.
          else {
            tickData.speedKmh = Math.Min(
              RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, acceleration),
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
            tickData.speedKmh = Math.Max(
              RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, -acceleration),
              turnSpeed);
          }
          else {
            // Else accelerate normally.
            tickData.speedKmh = Math.Min(
              RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, acceleration),
              maxSpeed);
          }
        }
        else {
          // If the train isn't a stopping distance (distance needed to decelerate from maxSpeed to 0)
          // away from the route end (plus some wiggle room), keep accelerating to train's max speed.
          if (tickData.distanceMeters < routeLengthMeters -
              StoppingCoefficient * RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration)) {
            tickData.speedKmh =
              Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, acceleration),
                maxSpeed);
          }
          // Else start decelerating.
          // With this the train coast at 7.2km/h for a few seconds at the end before stopping
          else {
            //7.2km/h is the speed from which the train can come to a stop in one second "tick" with the -2m/s^2 deceleration
            tickData.speedKmh = Math.Max(
              RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, -acceleration),
              (float)3.6 * acceleration);
          }
        }
      }
    }

    generatedSimulationTicks.RemoveAt(generatedSimulationTicks.Count - 1);
    //change last data to have 0 speed and open doors
    // TBD does this make sense...
    generatedSimulationTicks[generatedSimulationTicks.Count - 1].speedKmh = 0f;
    generatedSimulationTicks[generatedSimulationTicks.Count - 1].doorsOpen = true;

    List<TickData> res;
    
    if (!tickLength.Equals(DefaultTimeInterval)) {
      res = DownsampleTickData(generatedSimulationTicks, (int)tickLength);
    } else {
      res = generatedSimulationTicks;
    }
    
    SimulationData newSim = new(res, trainToBeSimulated, routeToBeSimulated);
    LatestSimulation = newSim;

    Logger.Debug(
      $"Simulation had tickData.Count={generatedSimulationTicks.Count}, res.Count={res.Count} mPoints.Count={mPointsList.Count}, route length meters={routeLengthMeters}");
    return newSim;
  }

  private static List<TickData> DownsampleTickData(List<TickData> ticks, int X) {
    int n = ticks.Count;
    var intervals = new List<(int Start, int End)>();

    // 1) Find runs of doorsOpen==true and build ±10 windows
    for (int i = 0; i < n; i++) {
      if (ticks[i].doorsOpen) {
        int runStart = i;
        while (i + 1 < n && ticks[i + 1].doorsOpen)
          i++;
        int runEnd = i;

        int wStart = Math.Max(0, runStart - TickBufferAroundStops);
        int wEnd = Math.Min(n - 1, runEnd + TickBufferAroundStops);
        intervals.Add((wStart, wEnd));
      }
    }

    // Merge overlapping/adjacent intervals
    var merged = intervals
      .OrderBy(iv => iv.Start)
      .Aggregate(new List<(int Start, int End)>(), (list, iv) => {
        if (list.Count == 0 || iv.Start > list.Last().End + 1)
          list.Add(iv);
        else {
          var last = list[list.Count - 1];
          list[list.Count - 1] = (last.Start, Math.Max(last.End, iv.End));
        }

        return list;
      });

    // 2) Walk once, preserving windows and sampling outside
    var result = new List<TickData>(n);
    int currentWindow = 0;
    int sinceLastKept = X; // Force the very first tick to be kept

    for (int i = 0; i < n; i++) {
      // Check if i is inside the current window
      bool inWindow = false;
      while (currentWindow < merged.Count && merged[currentWindow].End < i)
        currentWindow++;
      if (currentWindow < merged.Count &&
          merged[currentWindow].Start <= i && i <= merged[currentWindow].End) {
        inWindow = true;
      }

      if (inWindow) {
        // Always keep, but do NOT reset outside counter
        result.Add(ticks[i]);
      }
      else {
        // Outside any window: sample at most once per X
        sinceLastKept++;
        if (sinceLastKept >= X) {
          result.Add(ticks[i]);
          sinceLastKept = 0;
        }
      }
    }

    return result;
  }


  public static void StartAnimationPlayback() {
    LayerManager.RemoveAnimationLayer();
    LayerManager.CreateAnimationLayer();
  }

  public static void StopAnimationPlayback() {
    LayerManager.RemoveAnimationLayer();
  }

}