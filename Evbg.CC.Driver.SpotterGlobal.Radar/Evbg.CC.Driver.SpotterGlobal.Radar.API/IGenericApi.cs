using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{
    public interface IGenericApi : IDisposable
    {
        bool Connected { get; set; }
        bool Connect(string exampleProperty, string ip, int port, string username, string password, out string error);
        bool Disconnect();
        bool IsServerAvailable();

        event EventHandler<GenericEventArgs> GenericEvent;

        event EventHandler<GenericTamperEventArgs> TamperEvent;
        event EventHandler<TrackUpdateArgs> TrackUpdate;
        event EventHandler<AlarmTrackRecievedArgs> AlarmTrackRecieved;

        Task<GenericApiDevice> GetDevicesById(string deviceId);
        Task<IEnumerable<GenericApiDevice>> GetCameraList();


        Task<IEnumerable<GenericApiDevice>> GetRadarList();

        Task<IEnumerable<GenericApiDevice>> GetZoneList();

        Task<IEnumerable<GenericApiDevice>> GetUDCList();

        Task<IEnumerable<GenericApiDevice>> GetPushEventList();
        Task PollRadarAlarmTrackRecievedEvent(int pollingInterval, CancellationToken token);

        //Task PollZoneTrackUpdateEvent(int pollingInterval, CancellationToken token);




        //void OnAlarmTrackRecieved(AlarmTrackRecievedArgs e);
    }
}
