using Mapsui;
using NetTopologySuite.Geometries;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    internal class DataManager
    {
        public static List<TrainRoute> TrainRoutes = new List<TrainRoute>();
        public static TrainRoute CurrentTrainRoute;

        /// <summary>
        /// Export the created lines into a file.
        /// </summary>
        /// <param name="GeometryString">This takes a mapsui feature geometry string. Example: "LINESTRING ( x y, x y, x y ...)</param>
        public static void Export(String GeometryString) {
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");

            List<RouteCoordinate> Geometry = ParseGeometryString(GeometryString);
            TrainRoute NewTrainRoute = new TrainRoute("TestRoute", Geometry);

            // Create a file and write empty the new route to it
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(NewTrainRoute, Json_options));

            Import();
        }

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

        public static string Import()
        {
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");
            string FileAsString = "";

            // Open the file to read from
            using (StreamReader Sr = File.OpenText(Path))
            {

                // Read the lines on the file and gather a list from them
                string S;
                while ((S = Sr.ReadLine()) != null)
                {
                    FileAsString += S;
                }
            }

            // Deserialise the JSON string into a object
            var Json_options = new JsonSerializerOptions { IncludeFields = true };
            TrainRoute ImportedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(FileAsString, Json_options);

            // Set the imported train route as the currently selected one
            CurrentTrainRoute = ImportedTrainRoute;

            // Turn the coordinates back to a geometry string
            string GeometryString = "LINESTRING (";
            foreach (var coord in ImportedTrainRoute.Coords)
            {
                GeometryString += coord.Longitude + " " + coord.Latitude + ",";
            }
            GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";

            return GeometryString;
        }

        public static List<string> AddTunnels(List<string> TunnelPoints)
        {
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

        static double CalculateDistance(RoutePoint point1, RoutePoint point2)
        {
            double deltaX = double.Parse(point2.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture) - double.Parse(point1.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture);
            double deltaY = double.Parse(point2.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture) - double.Parse(point1.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture);

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

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
    }
}
