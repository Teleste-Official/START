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

  private static readonly string REST_API_CONTENT_KEY = "tick";

  private int _currentTickIndex;


  public TrainPointProvider() {
    _currentTickIndex = 0;
  }

  /// <summary>
  /// Starts the simulation playback.
  /// </summary>
  public void StartSimulation() {
    CancelToken();
    _currentTickIndex = 0;
    _cancellationTokenSource = new CancellationTokenSource();

    Logger.Info("Starting simulation playback...");
    Catch.TaskRun(() => RunTimerAsync(_cancellationTokenSource.Token));
  }

  /// <summary>
  /// Stops the simulation playback.
  /// </summary>
  public void StopSimulation() {
    Logger.Info("Stopping simulation playback...");
    CancelToken();
    _currentTickIndex = 0;
  }

  public void PauseSimulation() {
    Logger.Info($"Pause simulation playback at index {_currentTickIndex}");
    CancelToken();
  }

  public void ResumeSimulation() {
    CancelToken();
    _cancellationTokenSource = new CancellationTokenSource();

    Logger.Info($"Resume simulation playback from index {_currentTickIndex}");
    Catch.TaskRun(() => RunTimerAsync(_cancellationTokenSource.Token));
  }


  private void CancelToken() {
    if (_cancellationTokenSource != null) {
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
      //int tickPointIndex = 0;

      string url = SettingsManager.CurrentSettings.RestAPIUrl;
      int amountOfTicks = Simulation.LatestSimulation.TickData.Count;

      while (true) {
        cancellationToken.ThrowIfCancellationRequested(); // Exit if simulation is stopped

        if (_currentTickIndex >= amountOfTicks) {
          Logger.Info("All ticks processed. Ending simulation.");
          break;
        }

        DateTime tickStartTime = DateTime.UtcNow;

        TickData tickData = Simulation.LatestSimulation.TickData[_currentTickIndex];
        _prevCoords = (tickData.longitude, tickData.latitude);

        if (url != "") {
          Logger.Trace($"Processing tick #{_currentTickIndex + 1}: {tickData}");

          bool success = await SendTickRequestAsync(url, tickData, cancellationToken);
          if (!success) {
            Logger.Error($"Failed to process tick #{_currentTickIndex + 1}. Stopping simulation.");
            break;
          }
          TimeSpan elapsedTime = DateTime.UtcNow - tickStartTime;
          if (elapsedTime < TickInterval) {
            TimeSpan delay = TickInterval - elapsedTime;
            Logger.Trace($"Tick #{_currentTickIndex + 1} completed early. Waiting additional {delay.TotalMilliseconds}ms.");
            await Task.Delay(delay, cancellationToken);
          }
        } else {
          await Task.Delay(TickInterval, cancellationToken);
        }

        _currentTickIndex++;
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
  private async Task<bool> SendTickRequestAsync(string url, TickData tickData, CancellationToken cancellationToken) {
    try {
      string jsonTickData = JsonSerializer.Serialize(tickData);

      using MultipartFormDataContent content = new MultipartFormDataContent();
      StringContent jsonContent = new StringContent(jsonTickData, Encoding.UTF8, "application/json");
      content.Add(jsonContent, REST_API_CONTENT_KEY);

      HttpResponseMessage response = await HttpClient.PostAsync(url, content, cancellationToken);

      if (response.IsSuccessStatusCode) {
        Logger.Info($"Tick: {tickData} sent succesfully, Response: {response.StatusCode}");
        return true;
      }
      else {
        Logger.Warn($"Failed to send tick: {tickData}, Response: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()}");
        return false;
      }
    }
    catch (OperationCanceledException) {
      Logger.Info($"Request canceled for tick {tickData}.");
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