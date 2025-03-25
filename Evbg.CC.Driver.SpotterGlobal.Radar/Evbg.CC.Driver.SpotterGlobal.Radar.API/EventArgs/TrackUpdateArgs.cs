using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs
{
    public class TrackUpdateArgs : System.EventArgs
    {
        public int Id { get; set; }
        public string Description { get; set; }

        public int Bearing { get; set; }
        public int Velocity { get; set; }
        public DateTime Timestamp { get; set; }
        public string RadarDeviceId { get; }
        public string ZoneDeviceId { get; set; }

        public double? Latitude { get; }
        public double Longitude { get; }
        public double? Altitude { get; set; }
        public TrackUpdateArgs(DateTime timestamp, string deviceId)
        {
            Timestamp = timestamp;
            RadarDeviceId = deviceId;
        }
        public TrackUpdateArgs(DateTime timestamp, string deviceId, double? latitude, double longitude, double? altitude)
            : this(timestamp, deviceId)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }
    }
}
