using System;
using System.Collections.Generic;
using System.Linq;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Editing;
using Mapsui.Styles;
using Mapsui.Widgets.BoxWidget;
using Mapsui.Widgets.ButtonWidget;
using Mapsui.Widgets.MouseCoordinatesWidget;
using NetTopologySuite.IO;
using SmartTrainApplication.Data;
using static System.Net.Mime.MediaTypeNames;

namespace SmartTrainApplication;

public partial class MapViewControl
{
    public Map Map { get; internal set; }

    private void InitEditWidgets(Map map)
    {
        _targetLayer = map.Layers.FirstOrDefault(f => f.Name == "Layer 3") as WritableLayer;

        map.Widgets.Add(new BoxWidget
        {
            MarginY = 5,
            MarginX = 0,
            Width = 130,
            Height = 250,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            BackColor = Color.WhiteSmoke,
        });

        map.Widgets.Add(new Mapsui.Widgets.TextBox
        {
            MarginY = 10,
            MarginX = 5,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "START",
            BackColor = Color.Transparent,
        });

        var cancel = new ButtonWidget
        {
            MarginY = 25,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Clear",
            BackColor = Color.LightGray,
        };
        cancel.WidgetTouched += (_, e) =>
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
            e.Handled = true;
        };
        map.Widgets.Add(cancel);

        map.Widgets.Add(new Mapsui.Widgets.TextBox
        {
            MarginY = 60,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Editing Modes:",
            BackColor = Color.Transparent,
        });
        // Editing Modes
        var addPoint = new ButtonWidget
        {
            MarginY = 80,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Add Point",
            BackColor = Color.LightGray,
        };
        addPoint.WidgetTouched += (_, e) =>
        {
            var features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            _tempFeatures = new List<IFeature>(features);

            _editManager.EditMode = EditMode.AddPoint;
            e.Handled = true;
        };
        map.Widgets.Add(addPoint);
        var addLine = new ButtonWidget
        {
            MarginY = 100,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Add Line",
            BackColor = Color.LightGray,
        };
        addLine.WidgetTouched += (_, e) =>
        {
            var features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            _tempFeatures = new List<IFeature>(features);

            _editManager.EditMode = EditMode.AddLine;
            e.Handled = true;
        };
        map.Widgets.Add(addLine);
        var modify = new ButtonWidget
        {
            MarginY = 120,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Modify",
            BackColor = Color.LightGray,
        };
        modify.WidgetTouched += (_, e) =>
        {
            _editManager.EditMode = EditMode.Modify;
            e.Handled = true;
        };
        map.Widgets.Add(modify);
        var none = new ButtonWidget
        {
            MarginY = 140,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "None",
            BackColor = Color.LightGray,
        };
        none.WidgetTouched += (_, e) =>
        {
            _editManager.EditMode = EditMode.None;
            e.Handled = true;
        };
        map.Widgets.Add(none);

        // Deletion
        var selectForDelete = new ButtonWidget
        {
            MarginY = 170,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Select (for delete)",
            BackColor = Color.LightGray,
        };
        selectForDelete.WidgetTouched += (_, e) =>
        {
            _editManager.SelectMode = !_editManager.SelectMode;
            e.Handled = true;
        };
        map.Widgets.Add(selectForDelete);
        var delete = new ButtonWidget
        {
            MarginY = 190,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Delete",
            BackColor = Color.LightGray,
        };
        delete.WidgetTouched += (_, e) =>
        {
            if (_editManager.SelectMode)
            {
                var selectedFeatures = _editManager.Layer?.GetFeatures().Where(f => (bool?)f["Selected"] == true) ??
                                       Array.Empty<IFeature>();

                foreach (var selectedFeature in selectedFeatures)
                {
                    _editManager.Layer?.TryRemove(selectedFeature);
                }

                _mapControl?.RefreshGraphics();
            }

            e.Handled = true;
        };
        map.Widgets.Add(delete);


        var Export = new ButtonWidget
        {
            MarginY = 210,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Export",
            BackColor = Color.LightGray,
        };
        Export.WidgetTouched += (_, e) =>
        {
            // TODO: Add naming and multible feature saving with it
            var selectedFeatures = _editManager.Layer?.GetFeatures();
            if (selectedFeatures.Any())
            {
                foreach (var selectedFeature in selectedFeatures)
                {
                    GeometryFeature testFeature = selectedFeature as GeometryFeature;

                    // If there is multiple feature this overrides all others and only gets the frist one
                    // Fix when routes can be named -Metso
                    DataManager.Export(testFeature.Geometry.ToString());

                    // Currently this deletes all features, from the editlayer -Metso
                    _editManager.Layer?.TryRemove(selectedFeature);
                }
            }

            e.Handled = true;
        };
        map.Widgets.Add(Export);

        var Import = new ButtonWidget
        {
            MarginY = 230,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Import",
            BackColor = Color.LightGray,
        };
        Import.WidgetTouched += (_, e) =>
        {
            string GeometryData = DataManager.Import();
            var importLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Import");
            if (importLayer == null)
            {
                // Import layer doesnt exist yet, create the import layer
                map.Layers.Add(CreateImportLayer());
                importLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Import");
            }

            var lineString = new WKTReader().Read(GeometryData);
            IFeature feature = new GeometryFeature { Geometry = lineString };
            importLayer.Add(feature);

            e.Handled = true;
        };
        map.Widgets.Add(Import);

        var EditImport = new ButtonWidget
        {
            MarginY = 250,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Edit Import",
            BackColor = Color.LightGray,
        };
        EditImport.WidgetTouched += (_, e) =>
        {
            // Move this to it's own function so it can be used in "AddTunnel" - Metso

            // Get the import layer if it exists
            var importLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Import");
            if (importLayer != null)
            {
                // Throw the imported feature into edit layer for editing
                _editManager.Layer.AddRange(importLayer.GetFeatures().Copy());
                importLayer.Clear();
            }

            e.Handled = true;
        };
        map.Widgets.Add(EditImport);

        var ApplyEditImport = new ButtonWidget
        {
            MarginY = 270,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Apply Import Edit",
            BackColor = Color.LightGray,
        };
        ApplyEditImport.WidgetTouched += (_, e) =>
        {
            // Move this to it's own function so it can be used in "ConfirmTunnel" - Metso

            // Get the import layer if it exists
            var importLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Import");
            if (importLayer != null)
            {
                importLayer.AddRange(_editManager.Layer.GetFeatures().Copy());
                _editManager.Layer.Clear();
            }

            e.Handled = true;
        };
        map.Widgets.Add(ApplyEditImport);

        var AddTunnel = new ButtonWidget
        {
            MarginY = 290,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Add Tunnels",
            BackColor = Color.LightGray,
        };
        AddTunnel.WidgetTouched += (_, e) =>
        {
            var features = _targetLayer?.GetFeatures().Copy() ?? Array.Empty<IFeature>();

            foreach (var feature in features)
            {
                feature.RenderedGeometry.Clear();
            }

            _tempFeatures = new List<IFeature>(features);

            _editManager.EditMode = EditMode.AddPoint;

            e.Handled = true;
        };
        map.Widgets.Add(AddTunnel);

        var ConfirmTunnel = new ButtonWidget
        {
            MarginY = 310,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Confirm Tunnels",
            BackColor = Color.LightGray,
        };
        ConfirmTunnel.WidgetTouched += (_, e) =>
        {
            var tunnelLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Tunnel");
            var tunnelstringLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Tunnelstring");
            if (tunnelLayer == null)
            {
                // Import layer doesnt exist yet, create the import layer
                map.Layers.Add(CreateTunnelLayer());
                tunnelLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Tunnel");
            }
            if (tunnelstringLayer == null)
            {
                // Import layer doesnt exist yet, create the import layer
                map.Layers.Add(CreateTunnelstringLayer());
                tunnelstringLayer = (WritableLayer)map.Layers.FirstOrDefault(l => l.Name == "Tunnelstring");
            }

            // Take created tunnel point
            tunnelLayer.AddRange(_editManager.Layer.GetFeatures().Copy());
            // Clear the editlayer
            _editManager.Layer?.Clear();

            // List of the tunnel points added
            List<string> tunnelPoints = new List<string>();

            var features = tunnelLayer?.GetFeatures().Copy();

            foreach (var feature in features)
            {
                GeometryFeature testFeature = feature as GeometryFeature;

                string test = testFeature.Geometry.ToString();
                tunnelPoints.Add(test);
            }

            // Add tunnels to data
            List<string> tunnelStrings = DataManager.AddTunnels(tunnelPoints);

            foreach (var tunnelString in tunnelStrings)
            {
                var lineString = new WKTReader().Read(tunnelString);
                IFeature feature = new GeometryFeature { Geometry = lineString };
                tunnelstringLayer.Add(feature);
            }

            // THIS REMOVES THE TUNNEL POINTS FROM THE LAYER, see if we want this or not -Metso
            tunnelLayer.Clear();


            _mapControl?.RefreshGraphics();

            _editManager.EditMode = EditMode.None;

            _tempFeatures = null;

            e.Handled = true;
        };
        map.Widgets.Add(ConfirmTunnel);

        // Mouse Position Widget
        map.Widgets.Add(new MouseCoordinatesWidget(map));
    }
}