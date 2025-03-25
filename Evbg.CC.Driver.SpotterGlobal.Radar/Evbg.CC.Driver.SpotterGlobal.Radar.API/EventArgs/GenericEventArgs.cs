using System;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs
{
    public class GenericEventArgs : System.EventArgs
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }
        public double? Altitude { get; }
        public GenericEventArgs(DateTime timestamp, string deviceId)
        {
            Timestamp = timestamp;
            DeviceId = deviceId;
        }
        public GenericEventArgs(DateTime timestamp, string deviceId, double? latitude, double? longitude, double? altitude)
            : this(timestamp, deviceId)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }
    }
}
