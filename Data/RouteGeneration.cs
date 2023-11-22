using SmartTrainApplication.Models;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Mapsui;
using Mapsui.Projections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    internal class RouteGeneration
    {
        // Used in testing, triggered from 'Save' button. -Sami
        public static void GenerateRoute()
        {
            TrainRoute route = DataManager.CurrentTrainRoute;
            List<MPoint> points = new List<MPoint>();

            foreach (var coord in route.Coords)
            {
                double X = double.Parse(coord.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
                double Y = double.Parse(coord.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);
                var point = SphericalMercator.ToLonLat(new MPoint(X, Y));
                points.Add(point);
            }


            Debug.WriteLine($"Route Length: {CalculateRouteLength(points)}");

            // Used for testing functionality
            //Debug.WriteLine(points.Count);
            //foreach (var point in points)
            //{
            //    Debug.WriteLine($"{point.X}, {point.Y}");
            //}
        }

        public static double CalculateRouteLength(List<MPoint> points)
        {
            double length = 0;
            for (int i = 0; i < points.Count-1; i++)
            {
                MPoint currentPoint = points[i];
                MPoint nextPoint = points[i+1];

                length += CalculatePointDistance(currentPoint.X, nextPoint.X, currentPoint.Y, nextPoint.Y);

                // For EPSG:3857, major projection errors except at Equator. -Sami
                //length += CalculatePointDistance(currentPoint, nextPoint);

                // Used for testing functionality
                //Debug.WriteLine(length);
            }

            return length;
        }

        static double ConvertToRadians(double degrees)
        {
            return (degrees * Math.PI) / 180;
        }

        static double CalculatePointDistance(double lon1, double lon2, double lat1, double lat2)
        {
            const double R = 6371;
            var φ1 = ConvertToRadians(lat1);
            var φ2 = ConvertToRadians(lat2);
            var Δφ = ConvertToRadians(lat2 - lat1);
            var Δλ = ConvertToRadians(lon2 - lon1);

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var d = R * c;
            return d;
        }
        
        // For EPSG:3857, major projection errors except at Equator. -Sami
        //public static double CalculatePointDistance(MPoint point1, MPoint point2)
        //{
        //    double deltaX = point2.X - point1.X;
        //    double deltaY = point2.Y - point1.Y;

        //    return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        //}

        public static MPoint CalculateNewTrainPoint(MPoint point1, MPoint point2, double trainMovement, double pointDistance)
        {
            double X1 = point1.X;
            double Y1 = point1.Y;
            double X2 = point2.X;
            double Y2 = point2.Y;

            double newX = X1 + (trainMovement / pointDistance) * (X2 - X1);
            double newY = Y1 + (trainMovement / pointDistance) * (Y2 - Y1);

            return new MPoint(x: newX, y: newY);
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
