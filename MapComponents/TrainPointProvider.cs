#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
using NLog;

#endregion

namespace SmartTrainApplication.MapComponents;

/// <summary>
/// Provides TrainPoint for use in animated TrainRoute simulation playback
/// </summary>
internal sealed class TrainPointProvider : MemoryProvider, IDynamic, IDisposable {
  public event DataChangedEventHandler? DataChanged;

  private static readonly HttpClient HttpClient = new();
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  private readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(1000);
  private CancellationTokenSource? _cancellationTokenSource; // For stopping the simulation
  private (double Lon, double Lat) _prevCoords = (24.945831, 60.192059);

  public TrainPointProvider() {
    Logger.Info("TrainPointProvider initialized.");
  }

  /// <summary>
  /// Starts the simulation playback.
  /// </summary>
  public void StartSimulation() {
    StopSimulation(); // Ensure no previous simulation is running
    _cancellationTokenSource = new CancellationTokenSource();

    Logger.Info("Starting simulation playback...");
    Catch.TaskRun(() => RunTimerAsync(_cancellationTokenSource.Token));
  }

  /// <summary>
  /// Stops the simulation playback.
  /// </summary>
  public void StopSimulation() {
    if (_cancellationTokenSource != null) {
      Logger.Info("Stopping simulation playback...");
      _cancellationTokenSource.Cancel();
      _cancellationTokenSource.Dispose();
      _cancellationTokenSource = null;
    }
  }

  /// <summary>
  /// Plays the simulation by sending TickData to the server,
  /// and waits for an OK response before proceeding to the next tick.
  /// </summary>
  private async Task RunTimerAsync(CancellationToken cancellationToken) {
    try {
      _prevCoords = (Simulation.LatestSimulation.TickData.First<TickData>().longitude,
        Simulation.LatestSimulation.TickData.First<TickData>().latitude);
      int tickPointIndex = 0;

      while (true) {
        cancellationToken.ThrowIfCancellationRequested(); // Exit if simulation is stopped

        DateTime tickStartTime = DateTime.UtcNow;
        if (tickPointIndex >= Simulation.LatestSimulation.TickData.Count) {
          Logger.Info("All ticks processed. Ending simulation.");
          break;
        }

        TickData tickData = Simulation.LatestSimulation.TickData[tickPointIndex];
        _prevCoords = (tickData.longitude, tickData.latitude);
        Logger.Debug($"Processing tick #{tickPointIndex + 1}: {JsonSerializer.Serialize(tickData)}");

        bool success = await SendTickRequestAsync(tickData, cancellationToken);
        if (!success) {
          Logger.Error($"Failed to process tick #{tickPointIndex + 1}. Stopping simulation.");
          break;
        }

        tickPointIndex++;
        TimeSpan elapsedTime = DateTime.UtcNow - tickStartTime;
        if (elapsedTime < TickInterval) {
          TimeSpan delay = TickInterval - elapsedTime;
          Logger.Debug($"Tick #{tickPointIndex} completed early. Waiting additional {delay.TotalMilliseconds}ms.");
          await Task.Delay(delay, cancellationToken);
        }

        OnDataChanged(); // Notify listeners of data change
      }
    }
    catch (OperationCanceledException) {
      Logger.Info("Simulation playback canceled.");
    }
    catch (Exception ex) {
      Logger.Error(ex, "An error occurred during simulation playback.");
    }
  }

  /// <summary>
  /// Sends the current TickData as an HTTP POST request to the server.
  /// </summary>
  private async Task<bool> SendTickRequestAsync(TickData tickData, CancellationToken cancellationToken) {
    try {
      string url = SettingsManager.CurrentSettings.RestAPIUrl;
      string jsonTickData = JsonSerializer.Serialize(tickData);

      using MultipartFormDataContent content = new MultipartFormDataContent();
      StringContent jsonContent = new StringContent(jsonTickData, Encoding.UTF8, "application/json");
      content.Add(jsonContent, "tick");

      Logger.Debug($"Sending tick data to {url}");
      HttpResponseMessage response = await HttpClient.PostAsync(url, content, cancellationToken);

      if (response.IsSuccessStatusCode) {
        Logger.Info($"Tick data successfully sent: {response.StatusCode}");
        return true;
      }
      else {
        Logger.Warn($"Failed to send tick data. Response: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()}");
        return false;
      }
    }
    catch (OperationCanceledException) {
      Logger.Info("Tick request canceled.");
      return false;
    }
    catch (Exception ex) {
      Logger.Error($"Error while sending tick data: {ex.Message}");
      return false;
    }
  }

  void IDynamic.DataHasChanged() {
    OnDataChanged();
  }

  /// <summary>
  /// Invokes DataChangedEventHandler on data change.
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
    Logger.Info("TrainPointProvider disposed.");
    StopSimulation();
    HttpClient.Dispose();
  }
}