#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using SmartTrainApplication.Data;
using SmartTrainApplication.Models;

#endregion

namespace SmartTrainApplication.MapComponents;

/// <summary>
/// Provides TrainPoint for use in animated TrainRoute simulation playback
/// </summary>
internal sealed class TrainPointProvider : MemoryProvider, IDynamic, IDisposable {
  public event DataChangedEventHandler? DataChanged;

  // TODO make this modifiable in GUI
  private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(1000));

  public TrainPointProvider() {
    Catch.TaskRun(RunTimerAsync);
  }

  private (double Lon, double Lat) _prevCoords = (24.945831, 60.192059);

  /// <summary>
  /// Creates an async timer which "runs" the playback
  /// </summary>
  /// <returns>(Task) Async task for the timer</returns>
  private async Task RunTimerAsync() {
    _prevCoords = (Simulation.LatestSimulation.TickData.First<TickData>().longitudeDD,
      Simulation.LatestSimulation.TickData.First<TickData>().latitudeDD);
    int TickPointIndex = 0;

    while (true) {
      await _timer.WaitForNextTickAsync();

      _prevCoords = (Simulation.LatestSimulation.TickData[TickPointIndex].longitudeDD,
        Simulation.LatestSimulation.TickData[TickPointIndex].latitudeDD);
      ++TickPointIndex;// = TickPointIndex + (int)Simulation.TickLength;

      OnDataChanged();
    }
  }

  void IDynamic.DataHasChanged() {
    OnDataChanged();
  }

  /// <summary>
  /// Invokes DataChangedEventHandler on data change
  /// </summary>
  private void OnDataChanged() {
    DataChanged?.Invoke(this, new DataChangedEventArgs(null, false, null));
  }

  public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo) {
    PointFeature? trainFeature = new(SphericalMercator.FromLonLat(_prevCoords.Lon, _prevCoords.Lat).ToMPoint());
    trainFeature["ID"] = "train";
    return Task.FromResult((IEnumerable<IFeature>)new[] { trainFeature });
  }

  public void Dispose() {
    _timer.Dispose();
  }
}