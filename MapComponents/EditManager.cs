#region

using System.Linq;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts.Editing;
using Mapsui.Nts.Widgets;
using Mapsui.Projections;
using Mapsui.UI;
using SmartTrainApplication.Data;

#endregion

namespace SmartTrainApplication.Views;

public partial class MapViewControl {
  /// <summary>
  /// Creates a new map, applies the map projection and starting coordinates, and initializes the edit mode 
  /// </summary>
  /// <param name="mapControl">(IMapControl) Mapsui map control</param>
  /// <param name="editMode">(EditMode) The edit mode</param>
  /// <returns>(EditManager) Mapsui edit manager</returns>
  public static EditManager InitEditMode(IMapControl mapControl, EditMode editMode) {
    // Get the settings
    FileManager.LoadSettings();

    Map? map = CreateMap();

    EditManager? editManager = new()
    {
      Layer = (WritableLayer)map.Layers.First(l => l.Name == "EditLayer")
    };
    WritableLayer? targetLayer = (WritableLayer)map.Layers.First(l => l.Name == "Layer 3");

    // Load the polygon layer on startup so you can start modifying right away
    editManager.Layer.AddRange(targetLayer.GetFeatures().Copy());
    targetLayer.Clear();

    editManager.EditMode = editMode;

    EditManipulation? editManipulation = new();

    map.CRS = "EPSG:3857";

    MPoint? homePoint = new(SettingsManager.CurrentSettings.Longitude, SettingsManager.CurrentSettings.Latitude);

    // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
    MPoint? sphericalMercatorCoordinate = SphericalMercator.FromLonLat(homePoint.X, homePoint.Y).ToMPoint();
    // Set the center of the viewport to the coordinate. The UI will refresh automatically
    // Additionally you might want to set the resolution, this could depend on your specific purpose
    map.Home = n => n.CenterOnAndZoomTo(sphericalMercatorCoordinate, n.Resolutions[14]);

    map.Widgets.Add(new EditingWidget(mapControl, editManager, editManipulation));
    mapControl.Map = map;

    return editManager;
  }
}