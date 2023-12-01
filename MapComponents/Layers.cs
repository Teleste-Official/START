using Mapsui.Layers;
using Mapsui.Styles.Thematics;
using Mapsui.Styles;
using Mapsui.Extensions;

namespace SmartTrainApplication.Views;

public partial class MapViewControl
{
    /// <summary>
    /// Creates a new layer for Imports
    /// </summary>
    /// <returns>(WritableLayer) The import layer</returns>
    public static WritableLayer CreateImportLayer()
    {
        var importLayer = new WritableLayer
        {
            Name = "Import",
            Style = CreateImportStyle()
        };

        return importLayer;
    }

    /// <summary>
    /// Creates a new style for Imports
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style for imports</returns>
    public static IStyle CreateImportStyle()
    {
        return new VectorStyle
        {
            Fill = null,
            Outline = null,
#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
            Line = { Color = Color.FromString("Red"), Width = 6 }
        };
    }

    /// <summary>
    /// Creates a new layer for Tunnels
    /// </summary>
    /// <returns>(WritableLayer) The tunnel layer</returns>
    public static WritableLayer CreateTunnelLayer()
    {
        return new WritableLayer
        {
            Name = "Tunnel",
            Style = CreatePointStyle()
        };
    }

    /// <summary>
    /// Creates a new layer for Tunnel strings
    /// </summary>
    /// <returns>(WritableLayer) The tunnel string layer</returns>
    public static WritableLayer CreateTunnelstringLayer()
    {
        return new WritableLayer
        {
            Name = "Tunnelstring",
            Style = CreateTunnelstringStyle()
        };
    }

    /// <summary>
    /// Creates a new style for Tunnel strings
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style for tunnel strings</returns>
    public static IStyle CreateTunnelstringStyle()
    {
        return new VectorStyle
        {
            Fill = null,
            Outline = null,
#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
            Line = { Color = Color.FromString("Blue"), Width = 6 }
        };
    }

    /// <summary>
    /// Creates a new layer for Stops
    /// </summary>
    /// <returns>(WritableLayer) The stops layer</returns>
    public static WritableLayer CreateStopsLayer()
    {
        return new WritableLayer
        {
            Name = "Stops",
            IsMapInfoLayer = true,
            Style = CreateStopsStyle()
        };
    }

    /// <summary>
    /// Creates a new style for Stops
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style for Stops</returns>
    private static IStyle CreateStopsStyle()
    {
        return new VectorStyle
        {
            Fill = new Brush(Color.WhiteSmoke),
            Line = null,
            Outline = new Pen(Color.FromString("Red"), 5)
        };
    }

    /// <summary>
    /// Creates a new layer for edit
    /// </summary>
    /// <returns>(WritableLayer) The edit layer</returns>
    private static WritableLayer CreateEditLayer()
    {
        return new WritableLayer
        {
            Name = "EditLayer",
            Style = CreateEditLayerStyle(),
            IsMapInfoLayer = true
        };
    }

    /// <summary>
    /// Creates the EditLayer style collection
    /// </summary>
    /// <returns>(StyleCollection) Mapsui style collection</returns>
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

    /// <summary>
    /// Creates a new style for EditLayer
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style</returns>
    private static IStyle CreateEditLayerBasicStyle()
    {
        var editStyle = new VectorStyle
        {
            Fill = new Brush(EditModeColor),
            Line = new Pen(EditModeColor, 4),
            Outline = new Pen(EditModeColor, 3)
        };
        return editStyle;
    }

    // Define colors for use in vector styles
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

    /// <summary>
    /// Creates a new style for feature "selected"-status on EditLayer
    /// </summary>
    /// <returns>(IStyle) Mapsui theme style</returns>
    private static IStyle CreateSelectedStyle()
    {
        // To show the selected style a ThemeStyle is used which switches on and off the SelectedStyle
        // depending on a "Selected" attribute.
        return new ThemeStyle(f => (bool?)f["Selected"] == true ? SelectedStyle : DisableStyle);
    }

    /// <summary>
    /// Creates a new layer for Points
    /// </summary>
    /// <returns>(WritableLayer) The point layer</returns>
    private static WritableLayer CreatePointLayer()
    {
        return new WritableLayer
        {
            Name = "Layer 1",
            Style = CreatePointStyle()
        };
    }

    /// <summary>
    /// Creates a new layer for Lines
    /// </summary>
    /// <returns>(WritableLayer) The line layer</returns>
    private static WritableLayer CreateLineLayer()
    {
        var lineLayer = new WritableLayer
        {
            Name = "Layer 2",
            Style = CreateLineStyle()
        };

        return lineLayer;
    }

    /// <summary>
    /// Creates a new layer for Polygons
    /// </summary>
    /// <returns>(WritableLayer) The polygon layer</returns>
    private static WritableLayer CreatePolygonLayer()
    {
        var polygonLayer = new WritableLayer
        {
            Name = "Layer 3",
            Style = CreatePolygonStyle()
        };

        return polygonLayer;
    }

    /// <summary>
    /// Creates a new style for Points
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style for Points</returns>
    private static IStyle CreatePointStyle()
    {
        return new VectorStyle
        {
            Fill = new Brush(PointLayerColor),
            Line = new Pen(PointLayerColor, 3),
            Outline = new Pen(Color.Gray, 2)
        };
    }

    /// <summary>
    /// Creates a new style for Lines
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style for Lines</returns>
    private static IStyle CreateLineStyle()
    {
        return new VectorStyle
        {
            Fill = new Brush(LineLayerColor),
            Line = new Pen(LineLayerColor, 3),
            Outline = new Pen(LineLayerColor, 3)
        };
    }

    /// <summary>
    /// Creates a new style for Polygons
    /// </summary>
    /// <returns>(IStyle) Mapsui vector style for Polygons</returns>
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