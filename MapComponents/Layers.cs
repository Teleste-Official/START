using Mapsui.Layers;
using Mapsui.Styles.Thematics;
using Mapsui.Styles;

namespace SmartTrainApplication;

public partial class MapViewControl
{
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

    private static WritableLayer CreateTunnelLayer()
    {
        return new WritableLayer
        {
            Name = "Tunnel",
            Style = CreatePointStyle()
        };
    }

    private static WritableLayer CreateTunnelstringLayer()
    {
        return new WritableLayer
        {
            Name = "Tunnelstring",
            Style = CreateTunnelstringStyle()
        };
    }

    public static IStyle CreateTunnelstringStyle()
    {
        return new VectorStyle
        {
            Fill = null,
            Outline = null,
#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
            Line = { Color = Color.FromString("Blue"), Width = 4 }
        };
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