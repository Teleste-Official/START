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
using SmartTrainApplication.Views;

#endregion

namespace SmartTrainApplication.Data;

/// <summary>
/// Functions used for generating and simulating TrainRoutes
/// TODO document better... split functions and clean everything up.
/// </summary>
internal class Simulation {
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


  // The simulation will be created with this, if tickLength is set to something else in GUI, extra ticks will be removed.
  private const int DefaultTimeInterval = 1;
  // How many ticks will be included with DefaultTimeInterval when TickLength is greater than 1.
  private const int TickBufferAroundStops = 10;

  // key = distance traveled (meters) at a stop that the train will stop at.
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

  private static double GetDistanceToNextStop(float distanceTravelled, float routeLength) {
    for (int i = 0; i < stoppingDistances.Count; i++) {
      // Stop has been visited
      if (stoppingDistances[i].Item2) {
        continue;
      }

      double distanceToNextStop = stoppingDistances[i].Item1 - distanceTravelled;
      return distanceToNextStop;
    }

    return routeLength-distanceTravelled;
  }

  private static double GetDistanceFromPreviousStop(float distanceTravelled) {

    if (stoppingDistances.Count == 1) {
      if (stoppingDistances[0].Item2) {
        return distanceTravelled - stoppingDistances[0].Item1;
      }
    } else if (stoppingDistances.Count == 2) {
      if (stoppingDistances[0].Item2 && !stoppingDistances[1].Item2) {
        return distanceTravelled - stoppingDistances[0].Item1;
      }
    } else if (stoppingDistances.Count > 2) {
      for (int i = 0; i < stoppingDistances.Count-1; i++) {
        // Stop has been visited
        if (stoppingDistances[i].Item2 && !stoppingDistances[i+1].Item2) {
          if (distanceTravelled - stoppingDistances[i].Item1 < 0) {
            return 0;
          }
          return distanceTravelled - stoppingDistances[i].Item1;
        }
      }

      return distanceTravelled - stoppingDistances[stoppingDistances.Count-1].Item1;
    }

    return distanceTravelled;

  }

  private static List<(double, bool)> CalculateStoppingDistances(TrainRoute routeToBeSimulated,
    Dictionary<RouteCoordinate, bool> stopPoints) {
    List<(double, bool)> _stoppingDistances = new();
    double cumulativeDistance = 0;

    for (int i = 0; i < routeToBeSimulated.Coords.Count; i++) {
      if (i + 1 >= routeToBeSimulated.Coords.Count) {
        if (stopPoints.Values.ElementAt(i)) {
          _stoppingDistances.Add((cumulativeDistance, false));
        }
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

  private static void VisitNextStop(bool visited=true) {
    int i = 0;
    while (i < stoppingDistances.Count) {
      if (stoppingDistances[i].Item2) {
        i++;
        continue;
      }

      stoppingDistances[i] = (stoppingDistances[i].Item1, visited);
      break;
    }
  }


  // MPoints coming from this are in the actual correct format that will be in the simulator json.
  public static SimulationData GenerateSimulationData(Dictionary<RouteCoordinate, bool> stopsDictionary,
    Train trainToBeSimulated, TrainRoute routeToBeSimulated, int tickLength, float stopApproachSpeed, double slowZoneLengthMeters, double stopArrivalThresholdMeters, double timeSpentAtStopSeconds, double doorsOpenThreshold) {
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
    //stopPoints[stopPoints.Keys.ElementAt(stopPoints.Count - 1)] = true;
    
    // MPonts that correspond to the RouteCoordinates in selected route.
    List<MPoint> mPointsList = GenerateMPointListFromRouteCoordinates(routeToBeSimulated.Coords);

    double routeLengthMeters = RouteGeneration.CalculateRouteLength(mPointsList);

    TickData? tickData = new(mPointsList[0].Y, mPointsList[0].X, false, 0, false, 0, 0, false);

    int pointIndex = 1;
    double nextLat = mPointsList[pointIndex].Y;
    double nextLon = mPointsList[pointIndex].X;
    bool isGpsFix = true;

    double travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, DefaultTimeInterval, acceleration);
    double pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitude, nextLon, tickData.latitude, nextLat);


    bool isRunning = true;

    stoppingDistances = CalculateStoppingDistances(routeToBeSimulated, stopPoints);


    List<TickData> generatedSimulationTicks = new();
    while (isRunning) {

      // Basically this increases the lat+lon and distanceMeters until the point distance equals travel distance.
      while (travelDistance > pointDistance) {
        // Stop loop in trying to go past last point
        if (pointIndex == mPointsList.Count - 1) {
          isRunning = false;
          travelDistance = pointDistance;
          break;
        }

        travelDistance -= pointDistance;
        tickData.distance += (float)pointDistance;
        tickData.latitude = nextLat;
        tickData.longitude = nextLon;

        if (routeToBeSimulated.Coords[pointIndex].Type == "TUNNEL_ENTRANCE" ||
            routeToBeSimulated.Coords[pointIndex].Type == "TUNNEL_ENTRANCE_STOP") {
          isGpsFix = !isGpsFix;
        }

        pointIndex++;
        nextLat = mPointsList[pointIndex].Y;
        nextLon = mPointsList[pointIndex].X;

        pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitude, nextLon, tickData.latitude, nextLat);
      }


      (tickData.longitude, tickData.latitude) = RouteGeneration.CalculateNewTrainPoint(tickData.longitude,
        tickData.latitude, nextLon, nextLat, travelDistance, pointDistance);
      tickData.distance += (float)travelDistance;
      tickData.timeSecs += DefaultTimeInterval;

      if (tickData.distance >= routeLengthMeters) {
        break;
      }

      generatedSimulationTicks.Add(new TickData(
        tickData.latitude,
        tickData.longitude,
        isGpsFix,
        tickData.speedKmh,
        false,
        tickData.distance,
        tickData.timeSecs,
        tickData.isDoorsOpen));

      if (isRunning) {
        pointDistance = RouteGeneration.CalculatePointDistance(tickData.longitude, nextLon, tickData.latitude, nextLat);
        // How much the train moves next tick.
        travelDistance = RouteGeneration.CalculateTrainMovement(tickData.speedKmh, DefaultTimeInterval, acceleration);

        double distanceFromPreviousStop = GetDistanceFromPreviousStop(tickData.distance);
        double distanceToNextStopOrEnd = GetDistanceToNextStop(tickData.distance, (float)routeLengthMeters);
        // TODO maybe add a bit more distance to this.
        double distanceFromSlowToZero = RouteGeneration.CalculateStoppingDistance(stopApproachSpeed, 0f, -acceleration);
        double distanceFromMaxToSlow = RouteGeneration.CalculateStoppingDistance(maxSpeed, stopApproachSpeed, -acceleration);


        tickData.isDoorsOpen = distanceToNextStopOrEnd <= doorsOpenThreshold;

        /*
        double distanceToRouteEnd = routeLengthMeters - tickData.distanceMeters;
        double distanceFromCurrentToZero = RouteGeneration.CalculateStoppingDistance(tickData.speedKmh, 0f, -acceleration);
        double distanceFromCurrentToSlow = RouteGeneration.CalculateStoppingDistance(tickData.speedKmh, SlowSpeedKmh, -acceleration);
        double distanceFromMaxToZero = RouteGeneration.CalculateStoppingDistance(maxSpeed, 0f, -acceleration);
        double distanceFromSlowToMax = RouteGeneration.CalculateStoppingDistance(SlowSpeedKmh, maxSpeed, -acceleration);
        */
        //bool turn = turnPoints.Values.ElementAt(pointIndex);
        //bool stop = ShouldDecelerateAtDistance(tickData.distanceMeters, tickData.speedKmh, -acceleration);
        float targetSpeed;


        if (distanceToNextStopOrEnd <= distanceFromSlowToZero) {
          // Need to start decelerating to 0 to stop at the next stop
          targetSpeed = 0.0f;
        }
        else if (distanceToNextStopOrEnd <= (distanceFromMaxToSlow + slowZoneLengthMeters)) {
          // Need to start decelerating to SlowSpeed for approach to next stop
          targetSpeed = stopApproachSpeed;

        } else if (distanceFromPreviousStop < slowZoneLengthMeters) {
          // Maintain SlowSpeed until we pass the SlowSpeedThreshold from previous stop
          targetSpeed = stopApproachSpeed;
        } else {
          // Cruise at max speed when clear of all constraints
          targetSpeed = maxSpeed;
        }

        //Logger.Debug($"fromPrevious={distanceFromPreviousStop} toNext={distanceToNextStopOrEnd} travelled={tickData.distanceMeters} pointDistance={pointDistance} travelDistance={travelDistance} speed={tickData.speedKmh} targetSpeed={targetSpeed} NEEDED:{Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, -acceleration), 0.0f)}");

        if (distanceToNextStopOrEnd <= stopArrivalThresholdMeters) {
          VisitNextStop();
          for (int i = 1; i <= timeSpentAtStopSeconds; i++) {
            tickData.timeSecs += DefaultTimeInterval;
            generatedSimulationTicks.Add(new TickData(tickData.latitude, tickData.longitude, isGpsFix, 0.0f,
              true,
              tickData.distance, tickData.timeSecs, tickData.isDoorsOpen));
          }
        }
        // TODO implement correctly
        /*else if (turn) {
          Logger.Debug("TURNING");
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
            Logger.Debug("CALCING TURN SPEED");
            tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, -acceleration), turnSpeed);
          }
          else {
            // Else accelerate normally.
            tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, acceleration), maxSpeed);
          }
        }
        else {
          Logger.Debug("NOT TURNING");
        }*/


        if (tickData.speedKmh < targetSpeed) {
          tickData.speedKmh = Math.Min(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, acceleration), targetSpeed);
        } else {
          tickData.speedKmh = Math.Max(RouteGeneration.CalculateNewSpeed(tickData.speedKmh, DefaultTimeInterval, -acceleration), targetSpeed);
        }

      }

    }


    //change last data to have 0 speed and set isAtStop=true
    generatedSimulationTicks.RemoveAt(generatedSimulationTicks.Count - 1);
    generatedSimulationTicks[generatedSimulationTicks.Count - 1].speedKmh = 0.0f;
    generatedSimulationTicks[generatedSimulationTicks.Count - 1].isAtStop = true;

    List<TickData> res;
    
    if (!tickLength.Equals(DefaultTimeInterval)) {
      res = DownsampleTickData(generatedSimulationTicks, (int)tickLength);
    } else {
      res = generatedSimulationTicks;
    }
    
    SimulationData newSim = new(res, trainToBeSimulated, routeToBeSimulated);
    LatestSimulation = newSim;
    Logger.Debug($"Simulation had tickData.Count={generatedSimulationTicks.Count}, res.Count={res.Count} mPoints.Count={mPointsList.Count}, route length meters={routeLengthMeters}");
    return newSim;
  }

  private static void LogLatestSimulation() {

    foreach (TickData tick in LatestSimulation.TickData) {
      LogTick(tick);
    }
  }

  private static void LogTick(TickData tick) {
    const string tpl = "latitude={0,10:F6} longitude={1,10:F6} " +
                       "isGpsFix={2} isAtStop={3} isDoorsOpen={4} " +
                       "speedKmh={5,8:F3} distance={6,10:F3} timeSecs={7,4}";

    string line = string.Format(
      tpl,
      tick.latitude,                  // {0} - double with 6 decimal places
      tick.longitude,                 // {1} - double with 6 decimal places
      tick.isGpsFix.ToString().ToLower(),  // {2} - lowercase boolean
      tick.isAtStop.ToString().ToLower(),  // {3} - lowercase boolean
      tick.isDoorsOpen.ToString().ToLower(), // {4} - lowercase boolean
      tick.speedKmh,                   // {5} - float with 3 decimals
      tick.distance,                   // {6} - float with 3 decimals
      tick.timeSecs);                  // {7} - right-aligned in 4 chars

    Logger.Debug(line);
  }

  private static List<TickData> DownsampleTickData(List<TickData> ticks, int X) {
    int n = ticks.Count;
    var intervals = new List<(int Start, int End)>();

    // 1) Find runs of isAtStop==true and build ±10 windows
    for (int i = 0; i < n; i++) {
      if (ticks[i].isAtStop) {
        int runStart = i;
        while (i + 1 < n && ticks[i + 1].isAtStop)
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
    LogLatestSimulation();
    LayerManager.RemoveAnimationLayer();
    LayerManager.CreateAnimationLayer();
  }

  public static void StopAnimationPlayback() {
    LayerManager.RemoveAnimationLayer();
  }

}