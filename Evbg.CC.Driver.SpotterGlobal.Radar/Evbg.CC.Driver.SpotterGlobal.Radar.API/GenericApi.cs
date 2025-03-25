using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{
    public class GenericApi : IGenericApi
    {
        protected IList<GenericApiDevice> _devices;
        private readonly string _ip;
        private readonly int _port;
        public WebSession _ws;

        ILog _log = LogManager.GetLogger(typeof(GenericApi));

        public GenericApi(string ip, int port)
        {
            _ip = ip;
            _port = port;
            ////Sample code that  should be changed with actual
            _devices = new List<GenericApiDevice>();
            //{
            //    new GenericApiDevice("Device1", "First Device"),
            //    new GenericApiDevice("Device2", "Second Device")
            //};
        }

        public bool Connected { get; set; }
        public event EventHandler<GenericEventArgs> GenericEvent;
        public event EventHandler<GenericTamperEventArgs> TamperEvent;
        public event EventHandler<TrackUpdateArgs> TrackUpdate;
        public event EventHandler<AlarmTrackRecievedArgs> AlarmTrackRecieved;



        protected void OnGenericEvent(GenericEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            GenericEvent?.Invoke(this, e);
        }

        protected void OnTamper(GenericTamperEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            TamperEvent?.Invoke(this, e);
        }

        protected void OnTrackUpdate(TrackUpdateArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            TrackUpdate?.Invoke(this, e);
        }

        public void OnAlarmTrackRecieved(AlarmTrackRecievedArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            AlarmTrackRecieved?.Invoke(this, e);
        }


        //public IEnumerable<GenericApiDevice> GetCameraList()
        //{
        //    _ws = new WebSession();
        //    var cameraList = _ws.GetCameraList();
        //    return _devices;
        //}

        public async Task<IEnumerable<GenericApiDevice>> GetCameraList()
        {
            
            var cameraList = _ws.GetCameraList();

            var genericApiDevices = new List<GenericApiDevice>();

            if ( cameraList !=null)
            {
                foreach (var camera in cameraList)
                {
                    genericApiDevices.Add(new GenericApiDevice(camera.Id, camera.Name));
                }
            }

            return genericApiDevices;
        }

        public async Task<IEnumerable<GenericApiDevice>> GetRadarList()
        {
            var radarList = _ws.GetRadarList();
            var genericApiDevices = new List<GenericApiDevice>();
            if (radarList != null)
            {
                foreach (var radar in radarList)
                {
                    genericApiDevices.Add(new GenericApiDevice(radar.Id,radar.Name));
                }
            }
            return genericApiDevices;
        }

        public async Task<IEnumerable<GenericApiDevice>> GetZoneList()
        {
            var zoneList = _ws.GetZoneList();
            var genericApiDevices = new List<GenericApiDevice>();
            if (zoneList != null)
            {
                foreach (var zoneDevice in zoneList)
                {
                    genericApiDevices.Add(new GenericApiDevice(zoneDevice.Id, zoneDevice.Name));
                }
            }
            return genericApiDevices;

        }


        public async Task<IEnumerable<GenericApiDevice>> GetUDCList()
        {
            var udcList = _ws.GetUDCList();
            var genericApiDevices = new List<GenericApiDevice>();
            if (udcList != null)
            {
                foreach (var udc in udcList)
                {
                    genericApiDevices.Add(new GenericApiDevice(udc.Id, udc.Name));
                }
            }
            return genericApiDevices;
        }

        public async Task<IEnumerable<GenericApiDevice>> GetPushEventList()
        {
            var eventList = _ws.GetPushEventList();
            var genericApiDevices = new List<GenericApiDevice>();
            if (eventList != null)
            {
                foreach (var deviceEvent in eventList)
                {
                    //genericApiDevices.Add(new GenericApiDevice(deviceEvent.Id,deviceEvent.Stats));

                    genericApiDevices.Add(new GenericApiDevice(deviceEvent.Id, deviceEvent.Stats.ToString()));

                }
            }
            return genericApiDevices;
        }

        public async Task PollRadarAlarmTrackRecievedEvent(int pollingPeriodInMS, CancellationToken cancellationToken)
        {
            var pollingPeriod = TimeSpan.FromMilliseconds(pollingPeriodInMS);
            var StartTime = DateTime.Now.AddMilliseconds(-pollingPeriodInMS);
            var EndTime = DateTime.Now;
            

            _log.Info($"Starting PollRadarAlarmTrackRecievedEvent with a polling period of {pollingPeriodInMS} ms.");

            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //List<EventResult> eventList = _eventService.GetPushEventList();
                    //List<EventResult> eventList = List<EventResult>._ws();
                    var eventList = _ws.GetPushEventList();
                    if (eventList?.Any() == true)
                    {
                        foreach (var eventItem in eventList)
                        {
                            var timestamp = WebSession.getDateTime(eventItem.Timestamp);
                            OnAlarmTrackRecieved(new AlarmTrackRecievedArgs(timestamp, eventItem.Source)
                            { 
                                Id= int.Parse(eventItem.Id),
                                Timestamp = timestamp, 
                            });

                            foreach (var zone in eventItem.Zones)
                            {
                                OnTrackUpdate(new TrackUpdateArgs(timestamp, eventItem.Source)
                                {
                                    ZoneDeviceId = zone,
                                    Id = int.Parse(eventItem.Id),
                                    Timestamp = timestamp,
                                });
                            }

                            _log.Info($"Received Event: {eventItem.Id} at {eventItem.Stats}");
                        }
                    }
                    else
                    {
                        _log.Info("No new events received.");
                    }

                }
                catch (Exception ex)
                {
                    _log.Error($"Error in PollRadarAlarmTrackRecievedEvent: {ex.Message}");

                }
                await Task.Delay(pollingPeriod, cancellationToken);

            }
            _log.Info("Stopping PollRadarAlarmTrackRecievedEvent.");
        }


        //public async Task PollZoneTrackUpdateEvent(int pollingPeriodInMS, CancellationToken cancellationToken)
        //{
        //    var pollingPeriod = TimeSpan.FromMilliseconds(pollingPeriodInMS);
        //    var StartTime = DateTime.Now.AddMilliseconds(-pollingPeriodInMS);
        //    var EndTime = DateTime.Now;


        //    _log.Info($"Starting PollZoneTrackUpdateEvent with a polling period of {pollingPeriodInMS} ms.");

        //    while (!cancellationToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            //List<EventResult> eventList = _eventService.GetPushEventList();
        //            //List<EventResult> eventList = List<EventResult>._ws();
        //            var eventList = _ws.GetPushEventList();
        //            if (eventList?.Any() == true)
        //            {
        //                foreach (var eventItem in eventList)
        //                {
        //                    var timestamp = WebSession.getDateTime(eventItem.Timestamp);
        //                    OnTrackUpdate(new TrackUpdateArgs(timestamp, eventItem.Source) {
        //                        Id = int.Parse(eventItem.Id),
        //                        Timestamp = timestamp,
        //                    });

        //                    _log.Info($"Received Event: {eventItem.Id} at {eventItem.Stats}");
        //                }
        //            }
        //            else
        //            {
        //                _log.Info("No new events received.");
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            _log.Error($"Error in PollZoneTrackUpdateEvent: {ex.Message}");

        //        }
        //        await Task.Delay(pollingPeriod, cancellationToken);

        //    }
        //    _log.Info("Stopping PollZoneTrackUpdateEvent.");

        //}




        public async Task<GenericApiDevice> GetDevicesById(string deviceId)
        {
            ////Should use logic of getting the devices with at-least one async method
            return _devices.FirstOrDefault(d => d.DeviceId == deviceId);
        }

        public bool Connect(string exampleProperty, string ip, int port, string username, string password, out string error)
        {
            error = null;
            _ws = new WebSession();
            _ws.Login(ip, port, username, password, true);
            this.Connected = true;
            return true;
        }


        public bool Disconnect()
        {
            ////Disconnect logic goes here
            this.Connected = false;
            return true;
        }

        public bool IsServerAvailable()
        {
            return Connected;
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ////Dispose logic goes here
            }
        }

       


















        #endregion
    }

    
}
