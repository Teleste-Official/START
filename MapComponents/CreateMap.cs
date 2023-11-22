using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Editing;
using Mapsui.Nts.Layers;
using Mapsui.Styles.Thematics;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;

namespace SmartTrainApplication.Views;

public partial class MapViewControl
{
    public static Map map {  get; set; }

    public void Setup(IMapControl mapControl)
    {
        _editManager = InitEditMode(mapControl, EditMode.Modify);
       // InitEditWidgets(mapControl.Map);
        _mapControl = mapControl;
    }
    public static Map CreateMap()
    {
        map = new Map();

        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        map.Layers.Add(CreatePointLayer());
        map.Layers.Add(CreateLineLayer());
        map.Layers.Add(CreatePolygonLayer());
        var editLayer = CreateEditLayer();
        map.Layers.Add(editLayer);
        map.Layers.Add(new VertexOnlyLayer(editLayer) { Name = "VertexLayer" });
        return map;
    }
}