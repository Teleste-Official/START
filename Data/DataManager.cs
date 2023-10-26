using Mapsui;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
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

            // Turn the coordinates back to a geometry string
            string GeometryString = "LINESTRING (";
            foreach (var coord in ImportedTrainRoute.Coords)
            {
                GeometryString += coord.Longitude + " " + coord.Latitude + ",";
            }
            GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";

            return GeometryString;
        }
    }
}
