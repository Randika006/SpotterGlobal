using System;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs
{
    public class GenericTamperEventArgs : GenericEventArgs
    {
        public GenericAlarmStatus Status { get; }
        public GenericTamperEventArgs(DateTime timestamp, string sourceDeviceId, string description, GenericAlarmStatus status)
            : base(timestamp, sourceDeviceId)
        {
            Description = description;
            Status = status;
        }

        public GenericTamperEventArgs(DateTime timestamp, string sourceDeviceId, string description, GenericAlarmStatus status,
                                      double? latitude, double? longitude, double? altitude)
            : base(timestamp, sourceDeviceId, latitude, longitude, altitude)
        {
            Description = description;
            Status = status;
        }

        public override string ToString() => $"{base.ToString()}: Tamper {Status} - {Description}";

    }
}
