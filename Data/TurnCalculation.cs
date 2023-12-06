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
    }

}
