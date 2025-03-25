using System;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{
    public class GenericApiDevice : ITamper
    {
        public GenericApiDevice(string deviceId, string label)
        {
            DeviceId = deviceId;
            Label = label;
        }

        public string DeviceId { get; set; }
        public string Label { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double? Altitude { get; set; }
        public bool IsTamper { get; set; }
    }
}
