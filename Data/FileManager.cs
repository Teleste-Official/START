using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    internal class FileManager
    {
        public static FilePickerFileType JSON { get; } = new("json")
        {
            Patterns = new[] { "*.json" },
            AppleUniformTypeIdentifiers = new[] { "public.json" },
            MimeTypes = new[] { "application/json" }
        };

        /// <summary>
        /// Export the created lines into a file.
        /// </summary>
        /// <param name="GeometryString">This takes a mapsui feature geometry string. Example: "LINESTRING ( x y, x y, x y ...)</param>
        public static async void Export(String GeometryString, TopLevel topLevel)
        {
            if (GeometryString == "")
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export JSON",
                FileTypeChoices = new[] { JSON },
                SuggestedFileName = "export"
            });

            TrainRoute NewTrainRoute = DataManager.CreateNewRoute(GeometryString);

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

        public static void Save()
        {
            if (DataManager.CurrentTrainRoute == null)
                return;

            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");

            // Save the current train route
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(DataManager.CurrentTrainRoute, Json_options));
        }

        // Imports all JSON-files from directories saved by user on app startup
        // Currently just gets all JSON-files in runtime directory. Rest will be implemented later - Timo
        public static List<string> StartupFolderImport()
        {
            List<string> Files = new List<string>();
            List<string> SavedPaths = new List<string>(); //For later

            //In case these are needed later
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isMacOs = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Routes");
            Debug.WriteLine(Path);
            try
            {
                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }
                else
                {
                    var FilesInFolder = Directory.EnumerateFiles(Path, "*.json");

                    foreach (var file in FilesInFolder)
                    {
                        var FileAsString = "";
                        using (StreamReader sr = File.OpenText(file))
                        {
                            string S;
                            while ((S = sr.ReadLine()) != null)
                            {
                                FileAsString += S;
                            }
                        }
                        if (FileAsString.Contains("Coords"))
                        {
                            Files.Add(FileAsString);
                        }

                    }
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

            // Deserialise the JSON strings into objects and add to list
            var Json_options = new JsonSerializerOptions { IncludeFields = true };
            List<TrainRoute> ImportedTrainRoutes = new List<TrainRoute>();
            foreach (var file in Files)
            {
                TrainRoute ImportedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(file, Json_options);
                DataManager.TrainRoutes.Add(ImportedTrainRoute);
                ImportedTrainRoutes.Add(ImportedTrainRoute);
            }

            // Set the first imported train route as the currently selected one
            DataManager.CurrentTrainRoute = ImportedTrainRoutes[0];

            // Turn the coordinates back to a geometry string
            List<string> routesAsStrings = new List<string>();
            string GeometryString = "LINESTRING (";
            foreach (var route in ImportedTrainRoutes)
            {
                foreach (var coord in route.Coords)
                {
                    GeometryString += coord.Longitude + " " + coord.Latitude + ",";
                }
                GeometryString = GeometryString.Remove(GeometryString.Length - 1) + ")";
                routesAsStrings.Add(GeometryString);
            }

            return routesAsStrings;
        }


        public static void SaveSimulationData(SimulationData sim)
        {
            string SimulationsDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Simulations");
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Simulations", "simulation.json");
            try
            {
                if (!Directory.Exists(SimulationsDirectory))
                {
                    Directory.CreateDirectory(SimulationsDirectory);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

            // Save the simulation
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(sim, Json_options));
        }

        public static void LoadTrains()
        {
            string TrainsDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains");
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains", "train.json");
            string FileAsString = "";

            try
            {
                if (!Directory.Exists(TrainsDirectory))
                {
                    Directory.CreateDirectory(TrainsDirectory);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

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
            Train LoadedTrain = JsonSerializer.Deserialize<Train>(FileAsString, Json_options);

            // Set the imported train as the currently selected one
            DataManager.Trains.Add(LoadedTrain);
            DataManager.CurrentTrain = LoadedTrain;

            return;
        }

        public static void SaveTrain()
        {
            string TrainsDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains");
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains", string.Concat(DataManager.CurrentTrain.Name.Replace(" ", "_").Split(System.IO.Path.GetInvalidFileNameChars())) + ".json");
            try
            {
                if (!Directory.Exists(TrainsDirectory))
                {
                    Directory.CreateDirectory(TrainsDirectory);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

            // Save the train
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(DataManager.CurrentTrain, Json_options));
        }

    }
}
