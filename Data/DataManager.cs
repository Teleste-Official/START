using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DynamicData;
using Mapsui;
using NetTopologySuite.Geometries;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    /// <summary>
    /// Functions used for adding or updating tunnels, stops, etc. features to new or existing TrainRoutes
    /// </summary>
    internal class DataManager
    {
        public static List<TrainRoute> TrainRoutes = new List<TrainRoute>();
        public static TrainRoute CurrentTrainRoute;

        public static List<Train> Trains = new List<Train>();
        public static Train CurrentTrain;

        /// <summary>
        /// Creates a new TrainRoute with a list of RouteCoordinates from a given GeometryString
        /// </summary>
        /// <param name="GeometryString">(String) TrainRoute's GeometryString to be parsed</param>
        /// <returns>(TrainRoute) TrainRoute with name & list of RouteCoordinates</returns>
        public static TrainRoute CreateNewRoute(String GeometryString)
        {

            List<RouteCoordinate> Geometry = ParseGeometryString(GeometryString);
            TrainRoute NewTrainRoute = new TrainRoute("TestRoute", Geometry);

            return NewTrainRoute;
        }

        /// <summary>
        /// Adds the given TrainRoute to the list of TrainRoutes
        /// <br/>
        /// Or If TrainRoute is already in the list, updates it's tunnels and stops
        /// </summary>
        /// <param name="NewRoute">(TrainRoute) The TrainRoute to be added (or updated) to the list</param>
        public static void AddToRoutes(TrainRoute NewRoute)
        {
            TrainRoute? oldRoute = null;
            // Check if the route is new or edited existing one
            foreach (var Route in TrainRoutes)
            {
                if (Route.Name == NewRoute.Name)
                {
                    oldRoute = Route;
                }
            }

            if(oldRoute == null)
            {
                TrainRoutes.Add(NewRoute);
                CurrentTrainRoute = NewRoute;

            }
            else
            {
                // Fix tunnels and stops into the modified route
                List<string> tunnelPoints = GetTunnelPoints();
                List<string> stopsPoints = GetStopStrings();

                CurrentTrainRoute = NewRoute;
                List<string> tunnelStrings = AddTunnels(tunnelPoints);
                List<string> stopsStrings = AddStops(stopsPoints);

                LayerManager.RedrawTunnelsToMap(tunnelStrings);
                LayerManager.RedrawStopsToMap(stopsStrings);
            }

            return;
        }

        /// <summary>
        /// Parses the given GeometryString to a list of RouteCoordinates
        /// </summary>
        /// <param name="GeometryString">(String) TrainRoute's GeometryString to be parsed </param>
        /// <returns>(List of RoudeCoordinate(string X, string Y)) TrainRoute's coordinates</returns>
        private static List<RouteCoordinate> ParseGeometryString(String GeometryString)
        {
            // Parse the line string into individual values
            string[] ParsedGeometry = GeometryString.Split("(");
            string Geometry = ParsedGeometry[1].Remove(ParsedGeometry[1].Length - 1);
            string[] IndividualCoords = Geometry.Split(",");

            // Create a list of coordinate values from the parsed string
            List<RouteCoordinate> Coordinates = new List<RouteCoordinate>();
            for (int i = 0; i < IndividualCoords.Length; i++)
            {
                string[] xy = IndividualCoords[i].Split(" ");
                if (i == 0)
                {
                    Coordinates.Add(new RouteCoordinate(xy[0], xy[1]));
                }
                else
                {
                    Coordinates.Add(new RouteCoordinate(xy[1], xy[2]));
                }
            }
            return Coordinates;
        }        

        /// <summary>
        /// Adds tunnel data types to the given list of TunnelPoints
        /// </summary>
        /// <param name="TunnelPoints">(List of string) Points that contain Tunnels</param>
        /// <returns>(List of string) List of TunnelPoints with added tunnel data types</returns>
        public static List<string> AddTunnels(List<string> TunnelPoints)
        {
            List<string> tunnelStrings = new List<string>();
            if (CurrentTrainRoute == null) return tunnelStrings;

            foreach (var PointString in TunnelPoints)
            {
                // Parse the line string into individual values
                string[] ParsedGeometry = PointString.Split("(");
                string Geometry = ParsedGeometry[1].Remove(ParsedGeometry[1].Length - 1);
                string[] xy = Geometry.Split(" ");
                RoutePoint routePoint = new RoutePoint(xy[0], xy[1]);

                double closestDiff = 1000000;
                int pointStatusToBeChanged = -1;
                List<double> diffpoints = new List<double>();

                for (int i = 0; i < CurrentTrainRoute.Coords.Count; i++)
                {
                    double diff = CalculateDistance(new RoutePoint(CurrentTrainRoute.Coords[i].Longitude, CurrentTrainRoute.Coords[i].Latitude), routePoint);
                    diffpoints.Add(diff);
                    if (i == 0)
                    {
                        closestDiff = diff;
                        pointStatusToBeChanged = i;
                    }
                    else
                    {
                        if (diff < closestDiff)
                        {
                            closestDiff = diff;
                            pointStatusToBeChanged = i;
                        }
                    }
                }

                if(pointStatusToBeChanged != -1)
                    CurrentTrainRoute.Coords[pointStatusToBeChanged].SetType("TUNNEL_ENTRANCE");
            }

            FixTunnelTypes();

            tunnelStrings = GetTunnelStrings();

            return tunnelStrings;
        }

        /// <summary>
        /// Gets a list of Tunnel Entrance Points from the CurrentTrainRoute's coordinates
        /// </summary>
        /// <returns>(List of string) List of Tunnel Entrance Points gotten from CurrentTrainRoute.Coords</returns>
        static List<string> GetTunnelPoints()
        {
            List<string> points = new List<string>();

            foreach (var coord in CurrentTrainRoute.Coords)
            {
                if (coord.Type == "TUNNEL_ENTRANCE")
                    points.Add("POINT (" + coord.Longitude + " " + coord.Latitude + ")");
            }

            return points;
        }

        public static string GetCurrentLinestring()
        {
            string GeometryString = "LINESTRING (";

            foreach (var coord in CurrentTrainRoute.Coords)
            {
                GeometryString += coord.Longitude + " " + coord.Latitude + ",";
            }
            GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
            
            return GeometryString;
        }

        /// <summary>
        /// Gets a list of TunnelStrings from the CurrentTrainRoute's coordinates
        /// </summary>
        /// <returns>(List of string) List of tunnelStrings (points that contain tunnels) gotten from CurrentTrainRoute.Coords</returns>
        public static List<string> GetTunnelStrings()
        {
            List<string> tunnelStrings = new List<string>();

            int EntranceCount = 0;
            string GeometryString = "LINESTRING (";
            foreach (var coord in CurrentTrainRoute.Coords)
            {
                if (coord.Type == "TUNNEL" || coord.Type == "TUNNEL_ENTRANCE")
                    GeometryString += coord.Longitude + " " + coord.Latitude + ",";

                if (coord.Type == "TUNNEL_ENTRANCE")
                    EntranceCount++;

                if (EntranceCount == 2)
                {
                    GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
                    tunnelStrings.Add(GeometryString);
                    EntranceCount = 0;
                    GeometryString = "LINESTRING (";
                }
            }
            //GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
            //tunnelStrings.Add(GeometryString);

            return tunnelStrings;
        }

        /// <summary>
        /// Gets a list of StopPoints from the CurrentTrainRoute's coordinates
        /// </summary>
        /// <returns>(List of string) List of stopStrings (points that contain stops) gotten from CurrenTrainRoute.Coords</returns>
        public static List<string> GetStopStrings()
        {
            List<string> stopStrings = new List<string>();

            foreach (var coord in CurrentTrainRoute.Coords)
            {
                if (coord.Type == "STOP")
                {
                    stopStrings.Add("POINT (" + coord.Longitude + " " + coord.Latitude + ")");
                }
            }

            return stopStrings;
        }

        /// <summary>
        /// Calculates a distance between 2 given RoutePoints
        /// <br/>
        /// Used to determine the closest RoutePoint to which to add the tunnel entrance when creating tunnels
        /// </summary>
        /// <param name="point1">(RoutePoint) RoutePoint from which to calculate the distance</param>
        /// <param name="point2">(RoutePoint) RoutePoint to which to calculate the distance</param>
        /// <returns>(double) Distance between the 2 given RoutePoints</returns>
        public static double CalculateDistance(RoutePoint point1, RoutePoint point2)
        {
            double deltaX = double.Parse(point2.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture) - double.Parse(point1.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
            double deltaY = double.Parse(point2.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture) - double.Parse(point1.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// Updates the data types of points in-between Tunnel Entrances to Tunnels in CurrentTrainRoute.Coords
        /// </summary>
        private static void FixTunnelTypes()
        {
            bool tunnelEntrance = false;
            foreach (var coords in CurrentTrainRoute.Coords)
            {
                if(coords.Type == "TUNNEL_ENTRANCE")
                {
                    if(!tunnelEntrance)
                        tunnelEntrance = true;
                    else
                        tunnelEntrance = false;
                }
                else if (coords.Type == "NORMAL" && tunnelEntrance)
                {
                    coords.SetType("TUNNEL");
                }
            }
        }

        /// <summary>
        /// Adds stop data types to the given list of StopsPoints
        /// </summary>
        /// <param name="StopsPoints">(List of string) Points that contain Stops</param>
        /// <returns>(List of string) List of stopStrings with added stop data types</returns>
        public static List<string> AddStops(List<string> StopsPoints)
        {
            List<string> stopStrings = new List<string>();
            if (CurrentTrainRoute == null) return stopStrings;

            foreach (var PointString in StopsPoints)
            {
                // Parse the line string into individual values
                string[] ParsedGeometry = PointString.Split("(");
                string Geometry = ParsedGeometry[1].Remove(ParsedGeometry[1].Length - 1);
                string[] xy = Geometry.Split(" ");
                RoutePoint routePoint = new RoutePoint(xy[0], xy[1]);

                double closestDiff = 1000000;
                int pointStatusToBeChanged = -1;
                List<double> diffpoints = new List<double>();

                for (int i = 0; i < CurrentTrainRoute.Coords.Count; i++)
                {
                    double diff = CalculateDistance(new RoutePoint(CurrentTrainRoute.Coords[i].Longitude, CurrentTrainRoute.Coords[i].Latitude), routePoint);
                    diffpoints.Add(diff);
                    if (i == 0)
                    {
                        closestDiff = diff;
                        pointStatusToBeChanged = i;
                    }
                    else
                    {
                        if (diff < closestDiff)
                        {
                            closestDiff = diff;
                            pointStatusToBeChanged = i;
                        }
                    }
                }

                if (pointStatusToBeChanged != -1)
                    CurrentTrainRoute.Coords[pointStatusToBeChanged].SetType("STOP");
            }


            stopStrings = GetStopStrings();

            return stopStrings;
        }

    }
}
