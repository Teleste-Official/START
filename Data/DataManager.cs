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
    internal class DataManager
    {
        public static List<TrainRoute> TrainRoutes = new List<TrainRoute>();
        public static TrainRoute CurrentTrainRoute;

        public static FilePickerFileType JSON { get; } = new("json")
        {
            Patterns = new[] { "*.json" },
            AppleUniformTypeIdentifiers = new[] { "public.json" },
            MimeTypes = new[] { "application/json" }
        };

        public static TrainRoute CreateNewRoute(String GeometryString)
        {
            List<RouteCoordinate> Geometry = ParseGeometryString(GeometryString);
            TrainRoute NewTrainRoute = new TrainRoute("TestRoute", Geometry);

            return NewTrainRoute;
        }

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
        /// Export the created lines into a file.
        /// </summary>
        /// <param name="GeometryString">This takes a mapsui feature geometry string. Example: "LINESTRING ( x y, x y, x y ...)</param>
        public static async void Export(String GeometryString, TopLevel topLevel) {
            if (GeometryString == "")
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export JSON",
                FileTypeChoices = new[] { JSON },
                SuggestedFileName = "export"
            });
            
            TrainRoute NewTrainRoute = CreateNewRoute(GeometryString);

            // Create a file and write empty the new route to it
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            var output = JsonSerializer.Serialize(NewTrainRoute, Json_options);
            if (file is not null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var streamWriter = new StreamWriter(stream);
                await streamWriter.WriteLineAsync(output);
            }

            //Import();
        }

        public static void Save() {
            if (CurrentTrainRoute == null)
                return;

            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");

            // Save the current train route
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(CurrentTrainRoute, Json_options));
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

        public static List<string> ImportFolder(TopLevel topLevel) {
            //Open folder & parse JSON-files into a list
            Debug.WriteLine("ImportFolder");
            Task<List<string>> result = HandleFolderImport(topLevel);
            List<string> openedFiles = result.GetAwaiter().GetResult();

            // Deserialise the JSON strings into objects and add to list
            var Json_options = new JsonSerializerOptions { IncludeFields = true };
            List<TrainRoute> ImportedTrainRoutes = new List<TrainRoute>();
            foreach (var file in openedFiles)
            {
                TrainRoute ImportedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(file, Json_options);
                TrainRoutes.Add(ImportedTrainRoute);
                ImportedTrainRoutes.Add(ImportedTrainRoute);
            }
            

            // Set the first imported train route as the currently selected one
            
            CurrentTrainRoute = ImportedTrainRoutes[0];

            // Turn the coordinates back to a geometry string
            List<string> routesAsStrings = new List<string>();
            string GeometryString = "LINESTRING (";
            foreach (var route in ImportedTrainRoutes) { 
                foreach (var coord in route.Coords)
                {
                    GeometryString += coord.Longitude + " " + coord.Latitude + ",";
                }
                GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
                routesAsStrings.Add(GeometryString);
            }

            return routesAsStrings;
        }

        public static async Task<List<string>> HandleFolderImport(TopLevel topLevel)
        {
            //Open the folder as IReadOnlyList<IStorageFolder>
            Debug.WriteLine("HandleFolder");
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Open project folder",
                AllowMultiple = false,

            });
            Debug.WriteLine("After Opening Folder");
            //Get .json-files from the folder as IStorageItems into their own list
            List<IStorageItem> filesInFolder = new List<IStorageItem>();
            if (folder is not null)
            {
                foreach (var file in folder)
                {
                    var current = file.GetItemsAsync();
                    await foreach (var item in current)
                    {
                        var itemType = item.GetType();
                        if (itemType.ToString() is ".json") {
                            filesInFolder.Add(item);
                        }
                    }
                }
            }

            //Cast IStorageItems into IStorageFiles and read them into strings
            //And add the final strings to their own list
            List<string> FinalStrings = new List<string>(); 
            foreach (var file in filesInFolder)
            {
                string FileAsString = "";
                using var cast = file as IStorageFile;
                await using var stream = await cast.OpenReadAsync();
                using var st = new StreamReader(stream);
                string S;
                while ((S = st.ReadLine()) != null)
                {
                    FileAsString += S;
                }
                FinalStrings.Add(FileAsString);
            }
            

            return FinalStrings;
        }

        //NOTE: FILE IMPORT HANGS SOFTWARE.
        //Also i haven't made sure the rest of it works after picking the file. -Timo
        public static string Import(TopLevel topLevel)
        {
            //Open the file
            Task<string> task = HandleImport(topLevel);
            string FileAsString = task.GetAwaiter().GetResult();

            // Deserialise the JSON string into a object
            var Json_options = new JsonSerializerOptions { IncludeFields = true };
            TrainRoute ImportedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(FileAsString, Json_options);

            // Set the imported train route as the currently selected one
            TrainRoutes.Add(ImportedTrainRoute);
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

        public static async Task<string> HandleImport(TopLevel topLevel)
        {
            string FileAsString = "";
            //Code hangs here, no idea why
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open JSON",
                AllowMultiple = false,
                FileTypeFilter = new[] { JSON }
            });
            
            if (files is not null)
            {
                await using var stream = await files[0].OpenReadAsync();
                using var st = new StreamReader(stream);
                string S;
                while ((S = st.ReadLine()) != null)
                {
                    FileAsString += S;
                }
            }

            return FileAsString;
        }

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
