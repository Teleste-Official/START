using Avalonia.Controls;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Layers.AnimatedLayers;
using Mapsui.Nts;
using Mapsui.Nts.Editing;
using Mapsui.Styles;
using NetTopologySuite.IO;
using SmartTrainApplication.Data;
using SmartTrainApplication.MapComponents;
using SmartTrainApplication.Models;
using SmartTrainApplication.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SmartTrainApplication
{
    internal class LayerManager
    {
        private static WritableLayer? _targetLayer = MapViewControl.map.Layers.FirstOrDefault(f => f.Name == "Layer 3") as WritableLayer;

        /// <summary>
        /// Prepares the EditMode and features for adding lines/TrainRoutes
        /// </summary>
        public static void AddLine()
        {
            var features = _targetLayer.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            MapViewControl._tempFeatures = new List<IFeature>(features);

            MapViewControl._editManager.EditMode = EditMode.AddLine;
        }

        /// <summary>
        /// Clears the target layer and EditMode
        /// </summary>
        public static void ClearFeatures()
        {
            if (_targetLayer != null && MapViewControl._tempFeatures != null)
            {
                _targetLayer.Clear();
                _targetLayer.AddRange(MapViewControl._tempFeatures.Copy());
                MapViewControl._mapControl?.RefreshGraphics();
            }

            MapViewControl._editManager.Layer?.Clear();

            MapViewControl._mapControl?.RefreshGraphics();

            MapViewControl._editManager.EditMode = EditMode.None;

            MapViewControl._tempFeatures = null;
        }

        public static void ClearAllLayers()
        {
            var importLayer = CreateImportLayer();
            var tunnelLayer = CreateTunnelLayer();
            var tunnelstringLayer = CreateTunnelStringLayer();
            var stopsLayer = CreateStopsLayer();
            //var animationLayer = CreateAnimationLayer();

            importLayer.Clear();
            tunnelLayer.Clear();
            tunnelstringLayer.Clear();
            stopsLayer.Clear();
            //animationLayer.ClearCache();

            MapViewControl._mapControl?.RefreshGraphics();

            MapViewControl._editManager.EditMode = EditMode.None;

            MapViewControl._tempFeatures = null;
        }

        /// <summary>
        /// Exports the Route as a JSON using <c>FileManager.Export()</c>
        /// </summary>
        /// <param name="_editManager">(EditManager) Edit manager</param>
        /// <param name="topLevel">(TopLevel) Top level</param>
        public static void ExportNewRoute(TopLevel topLevel)
        {
            FileManager.Export(GetRouteAsString(), topLevel);
        }

        /// <summary>
        /// Adds new Routes from imports to an import layer and redraws the map
        /// </summary>
        /// <param name="SavedPaths">Saved Paths</param>
        public static void ImportNewRoute(List<string> SavedPaths)
        {
            List<string> ImportedRoutes = FileManager.StartupFolderImport(SavedPaths);
            try
            {
                string GeometryData = ImportedRoutes[0];

                var importLayer = LayerManager.CreateImportLayer();
                List<string> tunnelStrings = DataManager.GetTunnelStrings();
                List<string> stopsStrings = DataManager.GetStopStrings();

                LayerManager.TurnImportToFeature(GeometryData, importLayer);
                LayerManager.RedrawTunnelsToMap(tunnelStrings);
                LayerManager.RedrawStopsToMap(stopsStrings);
            }
            catch (Exception Ex)
            {

                Debug.WriteLine(Ex);
            }
            
        }

        public static void ChangeCurrentRoute(int RouteIndex)
        {
            string GeometryData = FileManager.ChangeCurrentRoute(RouteIndex);

            var importLayer = LayerManager.CreateImportLayer();
            List<string> tunnelStrings = DataManager.GetTunnelStrings();
            List<string> stopsStrings = DataManager.GetStopStrings();

            LayerManager.TurnImportToFeature(GeometryData, importLayer);
            LayerManager.RedrawTunnelsToMap(tunnelStrings);
            LayerManager.RedrawStopsToMap(stopsStrings);
        }

        /// <summary>
        /// Adds the new Route to data, turns it to a feature and redraws the map
        /// </summary>
        public static void ConfirmNewRoute(string Name = "Route", string ID = "")
        {
            string RouteString = GetRouteAsString();

            if (RouteString == "")
                return;
            
            TrainRoute newRoute = DataManager.CreateNewRoute(RouteString, Name, ID);
            DataManager.AddToRoutes(newRoute);

            WritableLayer _importLayer = CreateImportLayer();
            TurnImportToFeature(RouteString, _importLayer);

            List<string> tunnelStrings = DataManager.GetTunnelStrings();
            RedrawTunnelsToMap(tunnelStrings);

            List<string> stopStrings = DataManager.GetStopStrings();
            RedrawStopsToMap(stopStrings);
        }

        /// <summary>
        /// Gets the selected features/TrainRoutes as a string
        /// </summary>
        /// <returns>(string) Selected features</returns>
        static string GetRouteAsString()
        {
            string RouteString = "";
            var selectedFeatures = MapViewControl._editManager.Layer?.GetFeatures();
            if (selectedFeatures.Any())
            {
                foreach (var selectedFeature in selectedFeatures)
                {
                    GeometryFeature testFeature = selectedFeature as GeometryFeature;

                    // If there is multiple feature this overrides all others and only gets the frist one
                    // Fix when routes can be named -Metso
                    RouteString = testFeature.Geometry.ToString();

                    // Currently this deletes all features, from the editlayer -Metso
                    MapViewControl._editManager.Layer?.TryRemove(selectedFeature);
                }
            }
            return RouteString;
        }

        /// <summary>
        /// Prepares the EditMode and features for adding tunnels
        /// </summary>
        public static void AddTunnel()
        {
            var features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            MapViewControl._tempFeatures = new List<IFeature>(features);

            MapViewControl._editManager.EditMode = EditMode.AddPoint;
        }

        /// <summary>
        /// Creates a new, if doesn't already exist, layer for imports
        /// </summary>
        /// <returns>(WritableLayer) Import layer</returns>
        public static WritableLayer CreateImportLayer()
        {
            var importLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Import");
            if (importLayer == null)
            {
                // Import layer doesnt exist yet, create the import layer
                MapViewControl.map.Layers.Add(MapViewControl.CreateImportLayer());
                importLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Import");
            }

            return importLayer;
        }

        /// <summary>
        /// Creates a new, if doesn't already exist, layer for animations
        /// </summary>
        /// <returns>(AnimatedPointLayer) Animation layer</returns>
        public static AnimatedPointLayer CreateAnimationLayer()
        {
            var animationLayer = (AnimatedPointLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Playback");
            if (animationLayer == null)
            {
                // Animation layer doesnt exist yet, create the import layer
                MapViewControl.map.Layers.Add(new AnimatedPointLayer(new TrainPointProvider())
                {
                    Name = "Trains",
                    Style = new LabelStyle
                    {
                        BackColor = new Brush(Color.Black),
                        ForeColor = Color.White,
                        Text = "Train",
                    }
                });
                animationLayer = (AnimatedPointLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Playback");
            }

            return animationLayer;
        }

        /// <summary>
        /// Copies the import layer feature, without tunnels or stops, to edit layer for editing
        /// </summary>
        /// <returns>(WritableLayer) Import layer</returns>
        public static WritableLayer TurnImportToEdit()
        {
            var importLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Import");

            // Clear the tunnels and stops out of the way
            var tunnelstringLayer = CreateTunnelStringLayer();
            tunnelstringLayer.Clear();
            var stopsLayer = CreateStopsLayer();
            stopsLayer.Clear();

            if (importLayer != null)
            {
                // Throw the imported feature into edit layer for editing
                MapViewControl._editManager.Layer.AddRange(importLayer.GetFeatures().Copy());
                importLayer.Clear();
            }

            MapViewControl._editManager.EditMode = EditMode.Modify;
            MapViewControl._mapControl?.RefreshGraphics();

            return importLayer;
        }

        /// <summary>
        /// Applies the edits to the TrainRoute and clears the edit layer
        /// </summary>
        public static void ApplyEditing(string Name = "Route", string ID = "")
        {
            ConfirmNewRoute(Name, ID);

            MapViewControl._editManager.Layer.Clear();
            MapViewControl._editManager.EditMode = EditMode.None;
            MapViewControl._mapControl?.RefreshGraphics();
        }

        /// <summary>
        /// Makes a new GeometryFeature from the given GeometryData and adds it to the given importLayer
        /// </summary>
        /// <param name="GeometryData">(string) Imported GeometryData</param>
        /// <param name="importLayer">(WritableLayer) The importLayer on which to add the import</param>
        public static void TurnImportToFeature(string GeometryData, WritableLayer importLayer)
        {
            var lineString = new WKTReader().Read(GeometryData);
            IFeature feature = new GeometryFeature { Geometry = lineString };
            importLayer.Add(feature);
        }

        /// <summary>
        /// Creates a new, if doesn't already exist, layer for tunnels 
        /// </summary>
        /// <returns>(WritableLayer) Tunnel layer</returns>
        public static WritableLayer CreateTunnelLayer()
        {
            var tunnelLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnel");
            if (tunnelLayer == null)
            {
                // Tunnel layer doesnt exist yet, create the import layer
                MapViewControl.map.Layers.Add(MapViewControl.CreateTunnelLayer());
                tunnelLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnel");
            }
            return tunnelLayer;
        }

        /// <summary>
        /// Creates a new, if doesn't already exist, layer for tunnel strings
        /// </summary>
        /// <returns>(WritableLayer) Tunnel string layer</returns>
        public static WritableLayer CreateTunnelStringLayer()
        {
            var tunnelstringLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnelstring");
            if (tunnelstringLayer == null)
            {
                // TunnelString layer doesnt exist yet, create the import layer
                MapViewControl.map.Layers.Add(MapViewControl.CreateTunnelstringLayer());
                tunnelstringLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Tunnelstring");
            }
            return tunnelstringLayer;
        }

        /// <summary>
        /// Creates a new, if doesn't already exist, layer for stops
        /// </summary>
        /// <returns>(WritableLayer) Stops layer</returns>
        public static WritableLayer CreateStopsLayer()
        {
            var stopsLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Stops");
            if (stopsLayer == null)
            {
                // TunnelString layer doesnt exist yet, create the import layer
                MapViewControl.map.Layers.Add(MapViewControl.CreateStopsLayer());
                stopsLayer = (WritableLayer)MapViewControl.map.Layers.FirstOrDefault(l => l.Name == "Stops");
            }
            return stopsLayer;
        }

        /// <summary>
        /// Takes the inputted tunnel points, lists them, adds them to data, (re)draws tunnels to map and clears the edit layer
        /// </summary>
        public static void ConfirmTunnel()
        {
            var tunnelLayer = CreateTunnelLayer();
            var tunnelstringLayer = CreateTunnelStringLayer();

            // Take created tunnel points
            tunnelLayer.AddRange(MapViewControl._editManager.Layer.GetFeatures().Copy());
            // Clear the editlayer
            MapViewControl._editManager.Layer?.Clear();

            // List of the tunnel points added
            List<string> tunnelPoints = new List<string>();

            var features = tunnelLayer?.GetFeatures().Copy();

            foreach (var feature in features)
            {
                GeometryFeature pointFeature = feature as GeometryFeature;

                string point = pointFeature.Geometry.ToString();
                tunnelPoints.Add(point);
            }

            if (tunnelPoints.Count == 0)
                return;

            // Add tunnels to data
            List<string> tunnelStrings = DataManager.AddTunnels(tunnelPoints);

            RedrawTunnelsToMap(tunnelStrings);

            tunnelLayer.Clear();

            MapViewControl._mapControl?.RefreshGraphics();

            MapViewControl._editManager.EditMode = EditMode.None;

            MapViewControl._tempFeatures = null;
        }

        /// <summary>
        /// Takes the inputted stop points, lists them, adds them to data, (re)draws stops to map and clears the edit layer
        /// </summary>
        public static void ConfirmStops()
        {
            var stopsLayer = CreateStopsLayer();

            // Take created tunnel points
            stopsLayer.AddRange(MapViewControl._editManager.Layer.GetFeatures().Copy());
            // Clear the editlayer
            MapViewControl._editManager.Layer?.Clear();

            // List of the tunnel points added
            List<string> stopsPoints = new List<string>();

            var features = stopsLayer?.GetFeatures().Copy();

            foreach (var feature in features)
            {
                GeometryFeature testFeature = feature as GeometryFeature;

                string point = testFeature.Geometry.ToString();
                stopsPoints.Add(point);
            }

            if (stopsPoints.Count == 0)
                return;

            // Add stops to data
            List<string> stopsStrings = DataManager.AddStops(stopsPoints);

            RedrawStopsToMap(stopsStrings);

            MapViewControl._mapControl?.RefreshGraphics();

            MapViewControl._editManager.EditMode = EditMode.None;

            MapViewControl._tempFeatures = null;
        }

        public static void SwitchRoute()
        {
            ClearAllLayers();
            string GeometryString = DataManager.GetCurrentLinestring();
            var importLayer = CreateImportLayer();
            TurnImportToFeature(GeometryString, importLayer);

            List<string> tunnelStrings = DataManager.GetTunnelStrings();
            RedrawTunnelsToMap(tunnelStrings);

            List<string> stopStrings = DataManager.GetStopStrings();
            RedrawStopsToMap(stopStrings);
        }

        /// <summary>
        /// (Re)draws the given list of tunnels to the map
        /// </summary>
        /// <param name="tunnelStrings">(List of string) The tunnel strings</param>
        public static void RedrawTunnelsToMap(List<string> tunnelStrings)
        {
            var tunnelstringLayer = CreateTunnelStringLayer();
            tunnelstringLayer.Clear();
            foreach (var tunnelString in tunnelStrings)
            {
                var lineString = new WKTReader().Read(tunnelString);
                IFeature feature = new GeometryFeature { Geometry = lineString };
                tunnelstringLayer.Add(feature);
            }
        }

        /// <summary>
        /// (Re)draws the given list of tunnels to the map
        /// </summary>
        /// <param name="stopsStrings">(List of string) The stop strings</param>
        public static void RedrawStopsToMap(List<string> stopsStrings)
        {
            var stopsLayer = CreateStopsLayer();
            stopsLayer.Clear();
            if (stopsStrings.Count > 0)
            {
                foreach (var stopString in stopsStrings)
                {
                    var pointString = new WKTReader().Read(stopString);
                    IFeature feature = new GeometryFeature { Geometry = pointString };
                    stopsLayer.Add(feature);
                }
            }
        }
    }
}
