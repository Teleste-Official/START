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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication
{
    internal class LayerManager
    {
        public static void ClearFeatures(WritableLayer _targetLayer, List<IFeature> _tempFeatures, IMapControl? _mapControl, EditManager _editManager)
        {
            if (_targetLayer != null && _tempFeatures != null)
            {
                _targetLayer.Clear();
                _targetLayer.AddRange(_tempFeatures.Copy());
                _mapControl?.RefreshGraphics();
            }

            _editManager.Layer?.Clear();

            _mapControl?.RefreshGraphics();

            _editManager.EditMode = EditMode.None;

            _tempFeatures = null;
            
            return;
        }

        public static void ExportNewRoute(EditManager _editManager)
        {
            // TODO: Add naming and multible feature saving with it
            DataManager.Export(GetRouteAsString(_editManager));

            return;
        }

        public static void ImportNewRoute()
        {
            string GeometryData = DataManager.Import();
            var importLayer = LayerManager.CreateImportLayer();
            List<string> tunnelStrings = DataManager.GetTunnelStrings();
            List<string> stopsStrings = DataManager.GetStopStrings();

            LayerManager.TurnImportToFeature(GeometryData, importLayer);
            LayerManager.RedrawTunnelsToMap(tunnelStrings);
            LayerManager.RedrawStopsToMap(stopsStrings);
        }

        public static void ConfirmNewRoute(EditManager _editManager)
        {
            string RouteString = GetRouteAsString(_editManager);

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

        static string GetRouteAsString(EditManager _editManager)
        {
            string RouteString = "";
            var selectedFeatures = _editManager.Layer?.GetFeatures();
            if (selectedFeatures.Any())
            {
                foreach (var selectedFeature in selectedFeatures)
                {
                    GeometryFeature testFeature = selectedFeature as GeometryFeature;

                    // If there is multiple feature this overrides all others and only gets the frist one
                    // Fix when routes can be named -Metso
                    RouteString = testFeature.Geometry.ToString();

                    // Currently this deletes all features, from the editlayer -Metso
                    _editManager.Layer?.TryRemove(selectedFeature);
                }
            }
            return RouteString;
        }

        public static void AddTunnel(WritableLayer _targetLayer, List<IFeature> _tempFeatures, EditManager _editManager)
        {
            var features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            _tempFeatures = new List<IFeature>(features);

            _editManager.EditMode = EditMode.AddPoint;

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

        public static WritableLayer TurnImportToEdit(EditManager _editManager)
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
                _editManager.Layer.AddRange(importLayer.GetFeatures().Copy());
                importLayer.Clear();
            }

            _editManager.EditMode = EditMode.Modify;

            return importLayer;
        }

        public static void ApplyEditing(EditManager _editManager)
        {
            ConfirmNewRoute(_editManager);
                
            _editManager.Layer.Clear();

            _editManager.EditMode = EditMode.None;

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

        public static void ConfirmTunnel(EditManager _editManager, IMapControl _mapControl, List<IFeature> _tempFeatures)
        {
            var tunnelLayer = CreateTunnelLayer();
            var tunnelstringLayer = CreateTunnelStringLayer();

            // Take created tunnel points
            tunnelLayer.AddRange(_editManager.Layer.GetFeatures().Copy());
            // Clear the editlayer
            _editManager.Layer?.Clear();

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

            _mapControl?.RefreshGraphics();

            _editManager.EditMode = EditMode.None;

            _tempFeatures = null;

            return;
        }

        public static void ConfirmStops(EditManager _editManager, IMapControl _mapControl, List<IFeature> _tempFeatures)
        {
            var stopsLayer = CreateStopsLayer();

            // Take created tunnel points
            stopsLayer.AddRange(_editManager.Layer.GetFeatures().Copy());
            // Clear the editlayer
            _editManager.Layer?.Clear();

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

            _mapControl?.RefreshGraphics();

            _editManager.EditMode = EditMode.None;

            _tempFeatures = null;

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
