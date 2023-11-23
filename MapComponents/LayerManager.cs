using Avalonia.Controls;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Editing;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using NetTopologySuite.IO;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;
using SmartTrainApplication.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication
{
    internal class LayerManager
    {
        private static WritableLayer? _targetLayer = MapViewControl.map.Layers.FirstOrDefault(f => f.Name == "Layer 3") as WritableLayer;

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
            
            return;
        }

        public static void ExportNewRoute(EditManager _editManager, TopLevel topLevel)
        {
            // TODO: Add naming and multible feature saving with it
            FileManager.Export(GetRouteAsString(), topLevel);

            return;
        }

        public static void ImportNewRoute(TopLevel topLevel)
        {
            List<string> importedRoutes = FileManager.StartupFolderImport();
            string GeometryData = importedRoutes[0];
            //string GeometryData = DataManager.Import(topLevel);
            var importLayer = LayerManager.CreateImportLayer();
            List<string> tunnelStrings = DataManager.GetTunnelStrings();
            List<string> stopsStrings = DataManager.GetStopStrings();

            LayerManager.TurnImportToFeature(GeometryData, importLayer);
            LayerManager.RedrawTunnelsToMap(tunnelStrings);
            LayerManager.RedrawStopsToMap(stopsStrings);
        }

        public static void ConfirmNewRoute()
        {
            string RouteString = GetRouteAsString();

            if (RouteString == "")
                return;
            
            TrainRoute newRoute = DataManager.CreateNewRoute(RouteString);
            DataManager.AddToRoutes(newRoute);

            WritableLayer _importLayer = CreateImportLayer();
            TurnImportToFeature(RouteString, _importLayer);

            List<string> tunnelStrings = DataManager.GetTunnelStrings();
            RedrawTunnelsToMap(tunnelStrings);

            List<string> stopStrings = DataManager.GetStopStrings();
            RedrawStopsToMap(stopStrings);

            return;
        }

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

        public static void AddTunnel()
        {
            var features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            MapViewControl._tempFeatures = new List<IFeature>(features);

            MapViewControl._editManager.EditMode = EditMode.AddPoint;

            return;
        }

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

        public static void ApplyEditing()
        {
            ConfirmNewRoute();

            MapViewControl._editManager.Layer.Clear();

            MapViewControl._editManager.EditMode = EditMode.None;

            return;
        }

        public static void TurnImportToFeature(string GeometryData, WritableLayer importLayer)
        {
            var lineString = new WKTReader().Read(GeometryData);
            IFeature feature = new GeometryFeature { Geometry = lineString };
            importLayer.Add(feature);
            
            return;
        }

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

            return;
        }

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

            return;
        }

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
