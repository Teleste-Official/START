using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
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

namespace SmartTrainApplication.Views;

public partial class MapViewControl
{
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
            LayerManager.ClearFeatures();
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
            LayerManager.ExportNewRoute(MainWindow.TopLevel);

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
            LayerManager.ImportNewRoute(MainWindow.TopLevel);

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
            LayerManager.TurnImportToEdit();

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
            LayerManager.ApplyEditing();

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
            LayerManager.AddTunnel();

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
            LayerManager.ConfirmTunnel();

            e.Handled = true;
        };
        map.Widgets.Add(ConfirmTunnel);

        var ConfirmNewRoute = new ButtonWidget
        {
            MarginY = 330,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Confirm Route",
            BackColor = Color.LightGray,
        };
        ConfirmNewRoute.WidgetTouched += (_, e) =>
        {
            LayerManager.ConfirmNewRoute();

            e.Handled = true;
        };
        map.Widgets.Add(ConfirmNewRoute);

        var SaveRoute = new ButtonWidget
        {
            MarginY = 350,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Save",
            BackColor = Color.LightGray,
        };
        SaveRoute.WidgetTouched += (_, e) =>
        {
            FileManager.Save();

            e.Handled = true;
        };
        map.Widgets.Add(SaveRoute);

        var ConfirmStop = new ButtonWidget
        {
            MarginY = 370,
            MarginX = 5,
            Height = 18,
            Width = 120,
            CornerRadius = 2,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top,
            Text = "Confirm Stops",
            BackColor = Color.LightGray,
        };
        ConfirmStop.WidgetTouched += (_, e) =>
        {
            LayerManager.ConfirmStops();

            e.Handled = true;
        };
        map.Widgets.Add(ConfirmStop);

        // Mouse Position Widget
        map.Widgets.Add(new MouseCoordinatesWidget(map));
    }
}