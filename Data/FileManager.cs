using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SmartTrainApplication.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SmartTrainApplication.Data
{
    /// <summary>
    /// Functions used for saving and loading data to and from files
    /// </summary>
    internal class FileManager
    {
        public static FilePickerFileType JSON { get; } = new("json")
        {
            Patterns = new[] { "*.json" },
            AppleUniformTypeIdentifiers = new[] { "public.json" },
            MimeTypes = new[] { "application/json" }
        };

        public static List<string>? ImportedRoutesAsStrings { get; set; }
        public static string DefaultRouteFolderPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Routes");
        public static string DefaultTrainFolderPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains");

        // Currently active view
        public static string CurrentView = "";

        /// <summary>
        /// Export route into a file using filepicker.
        /// </summary>
        [Obsolete]
        public static async void ExportRoute(TopLevel topLevel)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export JSON",
                FileTypeChoices = new[] { JSON },
                SuggestedFileName = "export"
            });

            if (file == null) return;

            // Create a file and write empty the new route to it
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            var output = JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options);
            if (file is not null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var streamWriter = new StreamWriter(stream);
                await streamWriter.WriteLineAsync(output);
            }
        }

        /// <summary>
        /// Export any of the JSON files with filepicker
        /// </summary>
        /// <param name="topLevel"></param>
        /// <param name="type">Route, Train & Simulation</param>
        public static async void Export(TopLevel topLevel, string type)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export JSON",
                FileTypeChoices = new[] { JSON },
                SuggestedFileName = "export"
            });

            if (file == null) return;

            await using var stream = await file.OpenWriteAsync();
            using var streamWriter = new StreamWriter(stream);

            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            switch (type){
                case "Route":
                    if (DataManager.TrainRoutes.Any())
                    {
                        var RouteOutput = JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options);
                        await streamWriter.WriteLineAsync(RouteOutput);
                    }
                    break;

                case "Train":
                    if (DataManager.Trains.Any())
                    {
                        var TrainOutput = JsonSerializer.Serialize(DataManager.Trains[DataManager.CurrentTrain], Json_options);
                        await streamWriter.WriteLineAsync(TrainOutput);
                    }
                    break;

                case "Simulation":
                    if (Simulation.LatestSimulation != null)
                    {
                        var SimulationOutput = JsonSerializer.Serialize(Simulation.LatestSimulation, Json_options);
                        await streamWriter.WriteLineAsync(SimulationOutput);
                    }
                    break;

                case "Settings":
                    var SettingsOutput = JsonSerializer.Serialize(SettingsManager.CurrentSettings, Json_options);
                    await streamWriter.WriteLineAsync(SettingsOutput);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Imports the specified JSON as a string
        /// </summary>
        /// <param name="Path">Location of the file</param>
        /// <returns>JSON object as a string</returns>
        public static string ImportAsString(string Path)
        {
            var FileAsString = "";
            using (StreamReader sr = File.OpenText(Path))
            {
                string S;
                while ((S = sr.ReadLine()) != null)
                {
                    FileAsString += S;
                }
            }
            return FileAsString;
        }

        /// <summary>
        /// Saves DataManager.CurrentTrainRoute to "export.json" file
        /// </summary>
        public static void Save()
        {
            if (DataManager.TrainRoutes[DataManager.CurrentTrainRoute] == null)
                return;

            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "export.json");

            // Save the current train route
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(DataManager.TrainRoutes[DataManager.CurrentTrainRoute], Json_options));
        }

        public static void SaveSpecific(TrainRoute route)
        {
            if (route == null) return;

            string Path = route.FilePath;
            Debug.WriteLine("Save: " + Path);
            // Save the current train route
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(route, Json_options));
        }

        /// <summary>
        /// Imports all Json-files from folders defined by user in settings view. Also sets current train route.
        /// </summary>
        /// <param name="SavedPaths">Takes the list of saved paths from settings</param>
        /// <returns>Returns available routes a list of strings</returns>
        public static List<string> StartupFolderImport(List<string> SavedPaths)
        {
            List<string> Files = new List<string>();
            List<string> Paths = new List<string>();
            List<string> routesAsStrings = new List<string>();

            //Create default folder if it doesn't exist
            try
            {
                if (!Directory.Exists(DefaultRouteFolderPath))
                {
                    Directory.CreateDirectory(DefaultRouteFolderPath);
                }
            }
            catch (Exception Ex)
            {

                Debug.WriteLine(Ex.Message);
            }


            try {
                foreach (var Path in SavedPaths)
                {
                    Debug.WriteLine(Path);
                    if (Directory.Exists(Path))
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
                                Paths.Add(file);
                            }

                        }
                    }
                    else
                    {
                       //Do nothing, because folder doesn't exist
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
            for (int i = 0;i < Files.Count; i++)
            {
                TrainRoute ImportedTrainRoute = JsonSerializer.Deserialize<TrainRoute>(Files[i], Json_options);
                ImportedTrainRoute.FilePath = Paths[i];
                ImportedTrainRoute.Id = DataManager.CreateID();
                DataManager.TrainRoutes.Add(ImportedTrainRoute);
                ImportedTrainRoutes.Add(ImportedTrainRoute);
            }

            // Set the first imported train route as the currently selected one
            if (ImportedTrainRoutes.Any())
            {
                DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = ImportedTrainRoutes[0];
            }

            // Turn the coordinates back to geometry strings

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

            //Update lists
            ImportedRoutesAsStrings = routesAsStrings;
            DataManager.TrainRoutes = ImportedTrainRoutes;

            return routesAsStrings;
        }

        /// <summary>
        /// Changes currently active train route
        /// </summary>
        /// <param name="RouteIndex">Index number of wanted route</param>
        /// <returns>New active route</returns>
        public static string ChangeCurrentRoute(int RouteIndex)
        {
            if (RouteIndex == -1)
            {
                return DataManager.GetCurrentLinestring();
            }

            if (DataManager.TrainRoutes[RouteIndex] == null)
            {
                string FirstRoute = ImportedRoutesAsStrings[0];
                DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = DataManager.TrainRoutes[0];
                return FirstRoute;
            }
                
            string NewCurrentRoute = ImportedRoutesAsStrings[RouteIndex];
            DataManager.TrainRoutes[DataManager.CurrentTrainRoute] = DataManager.TrainRoutes[RouteIndex];
            return NewCurrentRoute;
        }

        /// <summary>
        /// Saves route's simulation data to "simulation.json" file
        /// </summary>
        /// <param name="sim">(SimulationData) Route's simulation data</param>
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

        /// <summary>
        /// Imports all train Json-files from folders defined by user in settings view.
        /// </summary>
        /// <param name="SavedPaths">Takes the list of saved paths from settings</param>
        /// <returns>Returns imported trains</returns>
        public static List<Train> StartupTrainFolderImport(List<string> SavedPaths)
        {
            List<Train> Trains = new List<Train>();
            List<string> Paths = new List<string>();

            if (SavedPaths == null) return Trains;

            var Json_options = new JsonSerializerOptions { IncludeFields = true };

            try
            {
                foreach (var Path in SavedPaths)
                {
                    Debug.WriteLine(Path);
                    if (Directory.Exists(Path))
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
                            if (FileAsString.Contains("MaxSpeed"))
                            {
                                Train LoadedTrain = JsonSerializer.Deserialize<Train>(FileAsString, Json_options);
                                LoadedTrain.FilePath = file;
                                LoadedTrain.Id = DataManager.CreateID();
                                Trains.Add(LoadedTrain);
                            }

                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(Ex.Message);
            }

            return Trains;
        }

        /// <summary>
        /// Load saved trains from folder to Datamanager.Trains and Datamanager.CurrentTrain
        /// </summary>
        [Obsolete]
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
            DataManager.CurrentTrain = DataManager.Trains.Count - 1;
        }

        /// <summary>
        /// Saves DataManager.CurrentTrain to folder
        /// </summary>
        public static void SaveTrain(Train train)
        {
            string TrainsDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Trains");
            string Path = System.IO.Path.Combine(train.FilePath);

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
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(train, Json_options));
        }

        public static void SaveSettings(Settings settings)
        {
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");

            // Save the settings
            var Json_options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(settings, Json_options));
        }

        public static void LoadSettings()
        {
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");
            string FileAsString = "";

            // Check if the file exists
            if (!File.Exists(Path))
            {
                SettingsManager.GenerateNewSettings();
                return;
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
            Settings LoadedSettings = JsonSerializer.Deserialize<Settings>(FileAsString, Json_options);

            // Set the settings
            SettingsManager.CurrentSettings = LoadedSettings;
        }

        internal static void OpenGuide()
        {
            string Path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "START_Guide.pdf");

            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(Path)
                {
                    UseShellExecute = true
                };
                p.Start();
            }
            catch (Exception ex) { }
            {
                Debug.WriteLine("No guide");
            }
        }
        
        public static async Task<string> OpenFolder(TopLevel topLevel)
        {
            string Path = "";
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose folder"
            });

            if (folder.Count > 0)
            {
                Path = folder[0].Path.AbsolutePath;
            }
            return Path;
        }
    }
}
