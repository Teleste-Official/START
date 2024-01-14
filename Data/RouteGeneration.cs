using SmartTrainApplication.Models;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Mapsui;
using Mapsui.Projections;

namespace SmartTrainApplication.Data
{
    /// <summary>
    /// Functions used for calculation Route/Train data during simulation loop
    /// </summary>
    internal class RouteGeneration
    {
        /// <summary>
        /// Used in initial testing to calculate route's length, now implemented in <c>Simulation.RunSimulation()</c>. 
        /// </summary>
        public static void GenerateRoute()
        {
            TrainRoute route = DataManager.TrainRoutes[DataManager.CurrentTrainRoute];
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

        /// <summary>
        /// Calculates given route's length using <c>CalculatePointDistance()</c>
        /// </summary>
        /// <param name="points">(List of MPoint(double X, double Y)) Route's points</param>
        /// <returns>(double) Route's length in meters</returns>
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

        /// <summary>
        /// Converts given degrees to radians
        /// </summary>
        /// <param name="degrees">(double) Degrees</param>
        /// <returns>(double) Radians</returns>
        static double ConvertToRadians(double degrees)
        {
            return (degrees * Math.PI) / 180;
        }

        /// <summary>
        /// Calculates a distance between 2 points based on given EPSG:4326 coordinates.
        /// <br/>
        /// The Haversine formula is used in the calculation.
        /// </summary>
        /// <param name="lon1">(double) First point's longitude in degrees</param>
        /// <param name="lon2">(double) Second point's longitude in degrees</param>
        /// <param name="lat1">(double) First point's latitude in degrees</param>
        /// <param name="lat2">(double) Second point's latitude in degrees</param>
        /// <returns>(double) Distance between given points in meters</returns>
        public static double CalculatePointDistance(double lon1, double lon2, double lat1, double lat2)
        {
            // Earth's radius in kilometers
            const double R = 6371;

            // φ is latitude, Δ is longitude, converted to radians
            var φ1 = ConvertToRadians(lat1);
            var φ2 = ConvertToRadians(lat2);
            var Δφ = ConvertToRadians(lat2 - lat1);
            var Δλ = ConvertToRadians(lon2 - lon1);

            //Haversine formula
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // d is the distance in kilometers
            var d = R * c;
            return d * 1000;
        }

        /// <summary>
        /// Calculates a distance between 2 points based on given EPSG:3857 coordinates.
        /// <br/>
        /// Not in use, major projection errors except at Equator. -Sami
        /// </summary>
        /// <param name="point1">(MPoint(double X, double Y)) First point in EPSG:3857</param>
        /// <param name="point2">(MPoint(double X, double Y)) Second point in EPSG:3857</param>
        /// <returns>(double) Distance between given points in meters</returns>
        //public static double CalculatePointDistance(MPoint point1, MPoint point2)
        //{
        //    double deltaX = point2.X - point1.X;
        //    double deltaY = point2.Y - point1.Y;

        //    return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        //}

        /// <summary>
        /// Calculates the train's new coordinates based on given EPSG:4326 coordinates and distances
        /// </summary>
        /// <param name="currentX">(double) Train's current longitude in degrees</param>
        /// <param name="currentY">(double) Train's current latitude in degrees</param>
        /// <param name="nextX">(double) Next route point's longitude in degrees</param>
        /// <param name="nextY">(double) Next route point's latitude in degrees</param>
        /// <param name="trainMovement">(double) Distance that the train moves in meters</param>
        /// <param name="pointDistance">(double) Train's distance to the next route point in meters</param>
        /// <returns>(double, double) Train's new coordinates in degrees</returns>
        public static (double, double) CalculateNewTrainPoint(double currentX, double currentY, double nextX, double nextY, double trainMovement, double pointDistance)
        {
            double newX = currentX + (trainMovement / pointDistance) * (nextX - currentX);
            double newY = currentY + (trainMovement / pointDistance) * (nextY - currentY);

            return (newX, newY);
        }

        /// <summary>
        /// Calculates train's movement distance in given interval using time, current speed and acceleration
        /// </summary>
        /// <param name="currentSpeedKmh">(float) Train's current speed in km/h</param>
        /// <param name="timeInterval">(float) Time interval("tick's" length) in seconds</param>
        /// <param name="acceleration">(float) Train's acceleration in m/s^2</param>
        /// <returns>(double) Train's movement distance in meters</returns>
        public static double CalculateTrainMovement(float currentSpeedKmh, float timeInterval, float acceleration)
        {
            return (currentSpeedKmh / 3.6) * timeInterval + 0.5 * acceleration * timeInterval * timeInterval;
        }

        /// <summary>
        /// Calculates train's new speed(after moving/"tick") given time, current speed and acceleration
        /// </summary>
        /// <param name="currentSpeedKmh">(float) Train's current speed in km/h</param>
        /// <param name="timeInterval">(float) Time interval("tick's" length) in seconds</param>
        /// <param name="acceleration">(float) Train's acceleration in m/s^2</param>
        /// <returns>(float) Train's new speed in km/h</returns>
        public static float CalculateNewSpeed(float currentSpeedKmh, float timeInterval, float acceleration)
        {
            return ((currentSpeedKmh / 3.6f) + timeInterval * acceleration) * 3.6f;
        }

        /// <summary>
        /// Calculates the (theoretical) distance needed to slow speed to target speed with given deceleration
        /// </summary>
        /// <param name="currentSpeedKmh">(float) Train's current speed in km/h</param>
        /// <param name="targetSpeedKmh">(float) Train's target speed in km/h</param>
        /// <param name="deceleration">(float) Train's deceleration (negative value) in m/s^2</param>
        /// <returns>(double) The needed slowing distance in meters</returns>
        public static double CalculateStoppingDistance(float currentSpeedKmh, float targetSpeedKmh, float deceleration)
        {
            float stoppingTime = ((targetSpeedKmh / 3.6f) - (currentSpeedKmh / 3.6f)) / deceleration;

            return CalculateTrainMovement(currentSpeedKmh, stoppingTime, deceleration);
        }
    }
}
