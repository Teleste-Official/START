using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts.Editing;
using Mapsui.UI;
using System.Collections.Generic;

namespace SmartTrainApplication.Views;

public partial class MapViewControl : UserControl
{
    private EditManager _editManager = new();
    private WritableLayer? _targetLayer;
    private IMapControl? _mapControl;
    private List<IFeature>? _tempFeatures;
    public MapViewControl()
    {
        InitializeComponent();

        var mapControl = new Mapsui.UI.Avalonia.MapControl();
        mapControl.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        Setup(mapControl);
        Content = _mapControl;
    }
}