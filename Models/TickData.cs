using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Models
{
    public class TickData
    {
        //RouteDataPoint: 0, Dafug is this? -Metso
        double latitudeDD { get; set; }
        bool IsGpsFix { get; set; }
        double longitudeDD { get; set; }
        float speedKmh { get; set; }
        bool doorsOpen { get; set; }
        float distanceMeters { get; set; }
        float trackTimeSecs { get; set; }

        TickData() { }

        TickData(double _latitudeDD, bool _isGpsFix, double _longitudeDD, float _speedKmh, bool _doorsOpen, float _distanceMeters, float _trackTimeSecs)
        {
            latitudeDD = _latitudeDD;
            IsGpsFix = _isGpsFix;
            longitudeDD = _longitudeDD;
            speedKmh = _speedKmh;
            doorsOpen = _doorsOpen;
            distanceMeters = _distanceMeters;
            trackTimeSecs = _trackTimeSecs;
        }
    }
}
