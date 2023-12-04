using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace SmartTrainApplication.MapComponents
{
    internal sealed class TrainPointProvider : MemoryProvider, IDynamic, IDisposable
    {
        public event DataChangedEventHandler? DataChanged;
        static double intervalTime = 5;

        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        public TrainPointProvider()
        {
            Catch.TaskRun(RunTimerAsync);
        }

        private (double Lon, double Lat) _prevCoords = (24.945831, 60.192059);
        private async Task RunTimerAsync()
        {
            _prevCoords = (Simulation.LatestSimulation.TickData.First<TickData>().longitudeDD, Simulation.LatestSimulation.TickData.First<TickData>().latitudeDD);
            int TickPointIndex = 0;

            while (true)
            {
                await _timer.WaitForNextTickAsync();

                _prevCoords = (Simulation.LatestSimulation.TickData[TickPointIndex].longitudeDD, Simulation.LatestSimulation.TickData[TickPointIndex].latitudeDD);
                TickPointIndex = TickPointIndex + (int)intervalTime;

                OnDataChanged();
            }
        }

        void IDynamic.DataHasChanged()
        {
            OnDataChanged();
        }

        private void OnDataChanged()
        {
            DataChanged?.Invoke(this, new DataChangedEventArgs(null, false, null));
        }

        public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
        {
            var trainFeature = new PointFeature(SphericalMercator.FromLonLat(_prevCoords.Lon, _prevCoords.Lat).ToMPoint());
            trainFeature["ID"] = "train";
            return Task.FromResult((IEnumerable<IFeature>)new[] { trainFeature });
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}

