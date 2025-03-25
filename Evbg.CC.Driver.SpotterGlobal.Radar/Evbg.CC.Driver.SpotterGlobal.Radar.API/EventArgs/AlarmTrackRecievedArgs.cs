using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs
{
    public class AlarmTrackRecievedArgs : System.EventArgs
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; }
        public double? Latitude { get; }
        public double? Longitude { get; set; }
        public double? Altitude { get; }


        public string TrackId { get; set; }

        public string ObjectCategory { get; set; }
        public string Confidence { get; set; }

        public int Bearing { get; set; }
        public int Velocity { get; set; }

        public string InitialAlarmDateTime { get; set; }

        public object Sender { get; set; }  // Add a property for the sender



        public AlarmTrackRecievedArgs(DateTime timestamp, string deviceId)
        {
            Timestamp = timestamp;
            DeviceId = deviceId;
        }
        public AlarmTrackRecievedArgs(DateTime timestamp, string deviceId, double? latitude, double? longitude, double? altitude)
            : this(timestamp, deviceId)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

    }
}
