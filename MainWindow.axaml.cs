using Avalonia.Controls;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Editing;
using Mapsui.Nts.Layers;
using Mapsui.Nts.Widgets;
using Mapsui.Styles.Thematics;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.Widgets.BoxWidget;
using Mapsui.Widgets.ButtonWidget;
using Mapsui.Widgets.MouseCoordinatesWidget;
using Mapsui.Widgets;
using NetTopologySuite.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using Mapsui.Projections;
using SmartTrainApplication.Data;
using NetTopologySuite.Geometries;
using Mapsui.Nts.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SmartTrainApplication
{
    public partial class MainWindow : Window
    {
        private EditManager _editManager = new();
        private WritableLayer? _targetLayer;
        private IMapControl? _mapControl;
        private List<IFeature>? _tempFeatures;

        public MainWindow()
        {
            InitializeComponent();
            var mapControl = new Mapsui.UI.Avalonia.MapControl();
            mapControl.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
            Setup(mapControl);
            Content = _mapControl;
        }

        public void Setup(IMapControl mapControl)
        {
            _editManager = InitEditMode(mapControl, EditMode.Modify);
            InitEditWidgets(mapControl.Map);
            _mapControl = mapControl;
        }

        public static EditManager InitEditMode(IMapControl mapControl, EditMode editMode)
        {
            var map = CreateMap();

            var editManager = new EditManager
            {
                Layer = (WritableLayer)map.Layers.First(l => l.Name == "EditLayer")
            };
            var targetLayer = (WritableLayer)map.Layers.First(l => l.Name == "Layer 3");

            // Load the polygon layer on startup so you can start modifying right away
            editManager.Layer.AddRange(targetLayer.GetFeatures().Copy());
            targetLayer.Clear();

            editManager.EditMode = editMode;

            var editManipulation = new EditManipulation();

            map.CRS = "EPSG:3857";
            var centerOfTampere = new MPoint(23.76227433384882, 61.49741016814548); //TODO move coords to a editable variable -Metso

            // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centerOfTampere.X, centerOfTampere.Y).ToMPoint();
            // Set the center of the viewport to the coordinate. The UI will refresh automatically
            // Additionally you might want to set the resolution, this could depend on your specific purpose
            map.Home = n => n.CenterOnAndZoomTo(sphericalMercatorCoordinate, n.Resolutions[14]);

            map.Widgets.Add(new EditingWidget(mapControl, editManager, editManipulation));
            mapControl.Map = map;
            return editManager;
        }

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
                HorizontalAlignment =   Mapsui.Widgets.HorizontalAlignment.Left,
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
                if (importLayer == null){
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

            // Mouse Position Widget
            map.Widgets.Add(new MouseCoordinatesWidget(map));

        }

        private static WritableLayer CreateImportLayer()
        {
            var importLayer = new WritableLayer
            {
                Name = "Import",
                Style = CreateImportStyle()
            };

            return importLayer;
        }

        public static IStyle CreateImportStyle()
        {
            return new VectorStyle
            {
                Fill = null,
                Outline = null,
                #pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
                Line = { Color = Color.FromString("Red"), Width = 4 }
            };
        }

        public static Map CreateMap()
        {
            var map = new Map();

            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            map.Layers.Add(CreatePointLayer());
            map.Layers.Add(CreateLineLayer());
            map.Layers.Add(CreatePolygonLayer());
            var editLayer = CreateEditLayer();
            map.Layers.Add(editLayer);
            map.Layers.Add(new VertexOnlyLayer(editLayer) { Name = "VertexLayer" });
            return map;
        }

        private static WritableLayer CreateEditLayer()
        {
            return new WritableLayer
            {
                Name = "EditLayer",
                Style = CreateEditLayerStyle(),
                IsMapInfoLayer = true
            };
        }

        private static StyleCollection CreateEditLayerStyle()
        {
            // The edit layer has two styles. That is why it needs to use a StyleCollection.
            // In a future version of Mapsui the ILayer will have a Styles collections just
            // as the GeometryFeature has right now.
            // The first style is the basic style of the features in edit mode.
            // The second style is the way to show a feature is selected.
            return new StyleCollection
            {
                Styles = {
                CreateEditLayerBasicStyle(),
                CreateSelectedStyle()
            }
            };
        }

        private static IStyle CreateEditLayerBasicStyle()
        {
            var editStyle = new VectorStyle
            {
                Fill = new Brush(EditModeColor),
                Line = new Pen(EditModeColor, 3),
                Outline = new Pen(EditModeColor, 3)
            };
            return editStyle;
        }

        private static readonly Color EditModeColor = new Color(124, 22, 111, 180);
        private static readonly Color PointLayerColor = new Color(240, 240, 240, 240);
        private static readonly Color LineLayerColor = new Color(150, 150, 150, 240);
        private static readonly Color PolygonLayerColor = new Color(20, 20, 20, 240);


        private static readonly SymbolStyle? SelectedStyle = new SymbolStyle
        {
            Fill = null,
            Outline = new Pen(Color.Red, 3),
            Line = new Pen(Color.Red, 3)
        };

        private static readonly SymbolStyle? DisableStyle = new SymbolStyle { Enabled = false };

        private static IStyle CreateSelectedStyle()
        {
            // To show the selected style a ThemeStyle is used which switches on and off the SelectedStyle
            // depending on a "Selected" attribute.
            return new ThemeStyle(f => (bool?)f["Selected"] == true ? SelectedStyle : DisableStyle);
        }

        private static WritableLayer CreatePointLayer()
        {
            return new WritableLayer
            {
                Name = "Layer 1",
                Style = CreatePointStyle()
            };
        }

        private static WritableLayer CreateLineLayer()
        {
            var lineLayer = new WritableLayer
            {
                Name = "Layer 2",
                Style = CreateLineStyle()
            };

            // todo: add data

            return lineLayer;
        }

        private static WritableLayer CreatePolygonLayer()
        {
            var polygonLayer = new WritableLayer
            {
                Name = "Layer 3",
                Style = CreatePolygonStyle()
            };

            return polygonLayer;
        }

        private static IStyle CreatePointStyle()
        {
            return new VectorStyle
            {
                Fill = new Brush(PointLayerColor),
                Line = new Pen(PointLayerColor, 3),
                Outline = new Pen(Color.Gray, 2)
            };
        }

        private static IStyle CreateLineStyle()
        {
            return new VectorStyle
            {
                Fill = new Brush(LineLayerColor),
                Line = new Pen(LineLayerColor, 3),
                Outline = new Pen(LineLayerColor, 3)
            };
        }
        private static IStyle CreatePolygonStyle()
        {
            return new VectorStyle
            {
                Fill = new Brush(new Color(PolygonLayerColor)),
                Line = new Pen(PolygonLayerColor, 3),
                Outline = new Pen(PolygonLayerColor, 3)
            };
        }
    }
}