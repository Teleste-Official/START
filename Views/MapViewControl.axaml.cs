#region

using System.Collections.Generic;
using Avalonia.Controls;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Editing;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.UI.Avalonia;

#endregion

namespace SmartTrainApplication.Views;

public partial class MapViewControl : UserControl {
  public static EditManager _editManager = new();
  private WritableLayer? _targetLayer;
  public static IMapControl? _mapControl;
  public static List<IFeature>? _tempFeatures;

  public MapViewControl() {
    InitializeComponent();
    var mapControl = new MapControl();
    mapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
    Setup(mapControl);
    Content = _mapControl;
  }
}