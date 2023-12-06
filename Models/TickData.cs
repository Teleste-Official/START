using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    /// <summary>
    /// Data points used for Train's data in TrainRoute generation and simulation
    /// <list type="bullet">
    /// <item>(double) latitudeDD</item>
    /// <item>(bool) IsGpsFix</item>
    /// <item>(double) longitudeDD</item>
    /// <item>(float) speedKmh</item>
    /// <item>(bool) doorsOpen</item>
    /// <item>(float) distanceMeters</item>
    /// <item>(float) trackTimeSecs</item>
    /// </list>
    /// </summary>
    public class TickData
    {
        //RouteDataPoint: 0, Dafug is this? -Metso
        public double latitudeDD { get; set; }
        public bool IsGpsFix { get; set; }
        public double longitudeDD { get; set; }
        public float speedKmh { get; set; }
        public bool doorsOpen { get; set; }
        public float distanceMeters { get; set; }
        public float trackTimeSecs { get; set; }

        public TickData() { }

        public TickData(double _latitudeDD, double _longitudeDD, bool _isGpsFix, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs)
        {
            latitudeDD = _latitudeDD;
            longitudeDD = _longitudeDD;
            IsGpsFix = _isGpsFix;
            speedKmh = _speedKmh;
            doorsOpen = _doorsOpen;
            distanceMeters = _distanceMeters;
            trackTimeSecs = _trackTimeSecs;
        }
    }
}
