namespace SmartTrainApplication.Models;

/// <summary>
/// Represents a data point capturing train's position, status, and movement metrics in the simulation. Uses EPSG:4326 (WGS 84) coordinate system.
/// </summary>
/// <param name="latitude">(double) Geographic latitude in decimal degrees (-90 to 90)</param>
/// <param name="longitude">(double) Geographic longitude in decimal degrees (-180 to 180)</param>
/// <param name="isGpsFix">(bool) GPS signal validity status (true = inside a tunnel)</param>
/// <param name="isAtStop">(bool) Stop alignment status (true = train is considered to be at a stop)</param>
/// <param name="isDoorsOpen">(bool) Door state signal (true = door indicator is shown for passengers)</param>
/// <param name="speedKmh">(float) Current speed in kilometers per hour</param>
/// <param name="distance">(float) Total traveled distance in meters from route start</param>
/// <param name="timeSecs">(float) Elapsed simulation time in seconds</param>
public class TickData {
  public double latitude { get; set; }
  public double longitude { get; set; }
  public bool isGpsFix { get; set; }
  public bool isAtStop { get; set; }
  public bool isDoorsOpen { get; set; }
  public float speedKmh { get; set; }
  public float distance { get; set; }
  public float timeSecs { get; set; }

  public TickData() {
  }

  public TickData(double _latitude, double _longitude, bool _isGpsFix, float _speedKmh, bool _isAtStop,
    float _distance, float _timeSecs, bool _doorsOpen) {
    latitude = _latitude;
    longitude = _longitude;
    isGpsFix = _isGpsFix;
    speedKmh = _speedKmh;
    isAtStop = _isAtStop;
    distance = _distance;
    timeSecs = _timeSecs;
    isDoorsOpen = _doorsOpen;
  }

  public override string ToString()
  {
    return $"latitude={latitude} longitude={longitude} isGpsFix={isGpsFix.ToString().ToLower()} " +
           $"isAtStop={isAtStop.ToString().ToLower()} isDoorsOpen={isDoorsOpen.ToString().ToLower()} " +
           $"speedKmh={speedKmh} distance={distance} timeSecs={timeSecs}";
  }
}