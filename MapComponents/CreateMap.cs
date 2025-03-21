#region

using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Editing;
using Mapsui.Nts.Layers;
using Mapsui.Tiling;
using Mapsui.UI;

#endregion

namespace SmartTrainApplication.Views;

public partial class MapViewControl {
  public static Map map { get; set; }

  /// <summary>
  /// Sets up the map and map control and initializes EditMode
  /// </summary>
  /// <param name="mapControl">(IMapControl) Mapsui map control</param>
  public void Setup(IMapControl mapControl) {
    _editManager = InitEditMode(mapControl, EditMode.Modify);
    //InitEditWidgets(mapControl.Map);
    _mapControl = mapControl;
  }

  /// <summary>
  /// Creates a New map and layers for it
  /// </summary>
  /// <returns>(Map) The map</returns>
  public static Map CreateMap() {
    map = new Map();

    map.Layers.Add(OpenStreetMap.CreateTileLayer());
    map.Layers.Add(CreatePointLayer());
    map.Layers.Add(CreateLineLayer());
    map.Layers.Add(CreatePolygonLayer());
    WritableLayer? editLayer = CreateEditLayer();
    map.Layers.Add(editLayer);
    map.Layers.Add(new VertexOnlyLayer(editLayer) { Name = "VertexLayer" });
    return map;
  }
}