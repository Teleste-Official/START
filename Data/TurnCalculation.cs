#region

using System;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.Data;

/// <summary>
/// Functions used for determining turns from Routes
/// <br/>
/// For Simulation Preprocessing
/// </summary>
public class TurnCalculation {
  /// <summary>
  /// Calculates if a gap between 3 given points is a turn
  /// </summary>
  /// <param name="point1">(Coor(double X, double Y)) Startig point</param>
  /// <param name="point2">(Coor(double X, double Y)) Middle point</param>
  /// <param name="point3">(Coor(double X, double Y)) End point</param>
  /// <returns>(bool) Is gap between given points a turn</returns>
  public static bool CalculateTurn(RoutePoint point1, RoutePoint point2, RoutePoint point3) {
    bool turn;

    // Convert string values to doubles
    var point1Longitude = double.Parse(point1.Longitude.Replace(".", ","));
    var point1Latitude = double.Parse(point1.Latitude.Replace(".", ","));

    var point2Longitude = double.Parse(point2.Longitude.Replace(".", ","));
    var point2Latitude = double.Parse(point2.Latitude.Replace(".", ","));

    var point3Longitude = double.Parse(point3.Longitude.Replace(".", ","));
    var point3Latitude = double.Parse(point3.Latitude.Replace(".", ","));

    // Calculate vectors v1 and v2
    var v1X = point2Longitude - point1Longitude;
    var v1Y = point2Latitude - point1Latitude;

    var v2X = point3Longitude - point2Longitude;
    var v2Y = point3Latitude - point2Latitude;

    // Calculate the cross product of v1 and v2
    var crossProduct = v1X * v2Y - v2X * v1Y;

    // Determine direction based on the cross product
    if (crossProduct > 100000)
      // Left turn
      turn = true;
    else if (crossProduct < -100000)
      // Right turn
      turn = true;
    else
      // Forward
      turn = false;

    return turn;
  }

  /// <summary>
  /// Calculates a radius of turn
  /// </summary>
  /// <param name="point1">(Coor(double X, double Y)) Startig point</param>
  /// <param name="point2">(Coor(double X, double Y)) Middle point</param>
  /// <param name="point3">(Coor(double X, double Y)) End point</param>
  /// <returns>(double) calculated curve radius</returns>
  public static float CalculateRadius(RoutePoint point1, RoutePoint point2, RoutePoint point3) {
    // Convert string values to doubles
    var point1Longitude = double.Parse(point1.Longitude.Replace(".", ","));
    var point1Latitude = double.Parse(point1.Latitude.Replace(".", ","));

    var point2Longitude = double.Parse(point2.Longitude.Replace(".", ","));
    var point2Latitude = double.Parse(point2.Latitude.Replace(".", ","));

    var point3Longitude = double.Parse(point3.Longitude.Replace(".", ","));
    var point3Latitude = double.Parse(point3.Latitude.Replace(".", ","));

    // Calculate the lengths of the sides of the triangle formed by the three points
    var a = Math.Sqrt(Math.Pow(point2Longitude - point1Longitude, 2) + Math.Pow(point2Latitude - point1Latitude, 2));
    var b = Math.Sqrt(Math.Pow(point3Longitude - point2Longitude, 2) + Math.Pow(point3Latitude - point2Latitude, 2));
    var c = Math.Sqrt(Math.Pow(point3Longitude - point1Longitude, 2) + Math.Pow(point3Latitude - point1Latitude, 2));

    // Calculate the semi-perimeter of the triangle
    var s = (a + b + c) / 2;

    // Calculate the radius
    var radius = a * b * c / (4 * Math.Sqrt(s * (s - a) * (s - b) * (s - c)));

    return (float)radius;
  }

  /// <summary>
  /// Calculates new speed based on curve radius
  /// </summary>
  /// <param name="point1">(Coor(double X, double Y)) Startig point</param>
  /// <param name="point2">(Coor(double X, double Y)) Middle point</param>
  /// <param name="point3">(Coor(double X, double Y)) End point</param>
  /// <param name="maxSpeed">(float) Train's maximum speed</param>
  /// <param name="maxRadius">(float) Maximum radius</param>
  /// <returns>(double) new turn speed based on curve radius</returns>
  public static float CalculateTurnSpeedByRadius(RoutePoint point1, RoutePoint point2, RoutePoint point3,
    float maxSpeed, float maxRadius) {
    var turn = CalculateTurn(point1, point2, point3);

    var curveRadius = CalculateRadius(point1, point2, point3);
    /*float deceleration = 2;

    if (curveRadius > maxRadius)
    {
        // Reduce speed proportionally to the severity of the turn
        deceleration = maxRadius / curveRadius;
    }

    float newSpeed = currentSpeed * deceleration;

    // if turn, reduce speed further
    if (turn)
    {
        newSpeed *= 0.5f;
    }

    return newSpeed;*/

    var turnSpeed = maxSpeed;

    if (curveRadius > maxRadius)
      // Reduce speed proportionally to the severity of the turn
      // The scale/multiplier maybe needs to be adjusted -Sami
      turnSpeed -= maxRadius / curveRadius * turnSpeed * 1.25f;

    return turnSpeed;
  }
}