using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartTrainApplication.Models;

namespace SmartTrainApplication.Data
{
    /// <summary>
    /// Functions used for determining turns from Routes
    /// <br/>
    /// For Simulation Preprocessing
    /// </summary>
    public class TurnCalculation
    {
        /// <summary>
        /// Calculates if a gap between 3 given points is a turn
        /// </summary>
        /// <param name="point1">(Coor(double X, double Y)) Startig point</param>
        /// <param name="point2">(Coor(double X, double Y)) Middle point</param>
        /// <param name="point3">(Coor(double X, double Y)) End point</param>
        /// <returns>(bool) Is gap between given points a turn</returns>
        public static bool CalculateTurn(RoutePoint point1, RoutePoint point2, RoutePoint point3)
        {
            bool turn;

            // Convert string values to doubles
            double point1Longitude = double.Parse(point1.Longitude.Replace(".", ","));
            double point1Latitude = double.Parse(point1.Latitude.Replace(".", ","));

            double point2Longitude = double.Parse(point2.Longitude.Replace(".", ","));
            double point2Latitude = double.Parse(point2.Latitude.Replace(".", ","));

            double point3Longitude = double.Parse(point3.Longitude.Replace(".", ","));
            double point3Latitude = double.Parse(point3.Latitude.Replace(".", ","));

            // Calculate vectors v1 and v2
            double v1x = point2Longitude - point1Longitude;
            double v1y = point2Latitude - point1Latitude;

            double v2x = point3Longitude - point2Longitude;
            double v2y = point3Latitude - point2Latitude;

            // Calculate the cross product of v1 and v2
            double crossProduct = v1x * v2y - v2x * v1y;

            // Determine direction based on the cross product
            if (crossProduct > 100000)
            {
                // Left turn
                turn = true;
            }
            else if (crossProduct < -100000)
            {
                // Right turn
                turn = true;
            }
            else
            {
                // Forward
                turn = false;
            }

            return turn;
        }
        /// <summary>
        /// Calculates a radius of turn
        /// </summary>
        /// <param name="point1">(Coor(double X, double Y)) Startig point</param>
        /// <param name="point2">(Coor(double X, double Y)) Middle point</param>
        /// <param name="point3">(Coor(double X, double Y)) End point</param>
        /// <returns>(double) calculated curve radius</returns>
        public static float CalculateRadius(RoutePoint point1, RoutePoint point2, RoutePoint point3)
        {
            // Convert string values to doubles
            double point1Longitude = double.Parse(point1.Longitude.Replace(".", ","));
            double point1Latitude = double.Parse(point1.Latitude.Replace(".", ","));

            double point2Longitude = double.Parse(point2.Longitude.Replace(".", ","));
            double point2Latitude = double.Parse(point2.Latitude.Replace(".", ","));

            double point3Longitude = double.Parse(point3.Longitude.Replace(".", ","));
            double point3Latitude = double.Parse(point3.Latitude.Replace(".", ","));

            // Calculate the lengths of the sides of the triangle formed by the three points
            double a = Math.Sqrt(Math.Pow(point2Longitude - point1Longitude, 2) + Math.Pow(point2Latitude - point1Latitude, 2));
            double b = Math.Sqrt(Math.Pow(point3Longitude - point2Longitude, 2) + Math.Pow(point3Latitude - point2Latitude, 2));
            double c = Math.Sqrt(Math.Pow(point3Longitude - point1Longitude, 2) + Math.Pow(point3Latitude - point1Latitude, 2));

            // Calculate the semi-perimeter of the triangle
            double s = (a + b + c) / 2;

            // Calculate the radius
            double radius = (a * b * c) / (4 * Math.Sqrt(s * (s - a) * (s - b) * (s - c)));

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
        public static float CalculateTurnSpeedByRadius(RoutePoint point1, RoutePoint point2, RoutePoint point3, float maxSpeed, float maxRadius)
        {
            bool turn = CalculateTurn(point1, point2, point3);

            float curveRadius = CalculateRadius(point1, point2, point3);
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

            float turnSpeed = maxSpeed;

            if (curveRadius > maxRadius)
            {
                // Reduce speed proportionally to the severity of the turn
                // The scale/multiplier maybe needs to be adjusted -Sami
                turnSpeed -= (maxRadius / curveRadius) * turnSpeed * 1.25f;
            }

            return turnSpeed;
        }
    }
}
