using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    internal class RouteGeneration
    {
        public static double CalculateRouteLength(List<RoutePoint> points)
        {
            double length = 0;
            for (int i = 0; i < points.Count-1; i++)
            {
                RoutePoint currentPoint = points[i];
                RoutePoint nextPoint = points[i+1];

                length += CalculatePointDistance(currentPoint, nextPoint);
            }

            return length;
        }

        public static RoutePoint CalculateNewTrainPoint(RoutePoint point1, RoutePoint point2, double trainMovement, double pointDistance)
        {
            double X1 = double.Parse(point1.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
            double Y1 = double.Parse(point1.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);
            double X2 = double.Parse(point2.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
            double Y2 = double.Parse(point2.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);

            double newX = X1 + (trainMovement / pointDistance) * (X2 - X1);
            double newY = Y1 + (trainMovement / pointDistance) * (Y2 - Y1);

            return new RoutePoint(newX.ToString(), newY.ToString());
        }

        public static double CalculatePointDistance(RoutePoint point1, RoutePoint point2)
        {
            double deltaX = double.Parse(point2.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture) - double.Parse(point1.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
            double deltaY = double.Parse(point2.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture) - double.Parse(point1.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public static double CalculateTrainMovement(float currentSpeed, float timeInterval, float acceleration)
        {
            return currentSpeed * timeInterval + 0.5 * acceleration * timeInterval * timeInterval;
        }

        public static float CalculateNewSpeed(float currentSpeed, float timeInterval, float acceleration)
        {
            return currentSpeed + timeInterval * acceleration;
        }
    }
}
