using CNL.IPSecurityCenter.Driver.Utility.Threading;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using Evbg.CC.Driver.SpotterGlobal.Radar.ServerService;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.GenericSimulator
{
    public class GenericSimulator : IGenericApi
    {
        private const int KeepAliveIntervalMsec = 1000;
        private const int EventPollingIntervalMsec = 2000;
        private readonly string _ip;
        private readonly int _port;
        private string _username;
        private string _password;
        private string _error;
        private SafeTimer _keepAliveTimer;
        private SafeTimer _eventPollingTimer;
        private DateTime _lastEventPollEndTime;

        private static readonly ConcurrentDictionary<string, IGenericApi> Instances = new ConcurrentDictionary<string, IGenericApi>();

        protected ILog Log { get; }
        protected ServerServiceClient Client { get; private set; }

        ////------------ public properties ------------------

        public static GenericSimulator Instance(string serverAddress, int port, ILog log)
        {
            var sessionKey = GetSessionKey(serverAddress, port);
            if (!Instances.TryGetValue(sessionKey, out var session))
            {
                session = new GenericSimulator(serverAddress, port, log);
                Instances[sessionKey] = session;
            }

            return (GenericSimulator)session;
        }

        public static string GetSessionKey(string ip, int port) => $"{ip}:{port}";

        public static void DisposeInstance(string ip, int port)
        {
            var sessionKey = GetSessionKey(ip, port);
            if (Instances.TryGetValue(sessionKey, out var instance))
            {
                instance.Dispose();
                Instances.TryRemove(sessionKey, out _);
            }
        }

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Unknown;
        public bool Connected
        {
            get;
            set;
        }

        public event EventHandler<GenericEventArgs> GenericEvent;
        public event EventHandler<GenericTamperEventArgs> TamperEvent;
        public event EventHandler ConnectedEvent;
        public event EventHandler<TrackUpdateArgs> TrackUpdate;
        public event EventHandler<AlarmTrackRecievedArgs> AlarmTrackRecieved;

        protected GenericSimulator(string ip, int port, ILog log)
        {
            _ip = ip;
            _port = port;
            Log = log;
        }

        public bool Connect(string exampleProperty, string ip, int port, string username, string password, out string error)
        {
            error = string.Empty;
            if (ConnectionState == ConnectionState.Connected || ConnectionState == ConnectionState.Connecting)
            {
                return true;
            }

            //if (ConnectionState == ConnectionState.Disposed)
            //{
            //    return false;
            //}

            ConnectionState = ConnectionState.Connecting;

            _username = username;
            _password = password;
            var remoteAddress = GetSimulatorServiceEndpointAddress(ip, port);
            var binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            Client = new ServerServiceClient(binding, remoteAddress);

            Log.Debug($"Simulator: Log in to WCF service '{remoteAddress}'");

            try
            {
                var response = Client.Login(ip, port, username, password);
                Log.Debug($"Logon result: {response.Type}: '{response.Description}'");

                if (response.Type == ResultType.Success)
                {
                    ConnectionState = ConnectionState.Connected;
                    StartKeepAliveTimer();
                    StartEventPollingTimer();

                    OnConnected(this, EventArgs.Empty);
                    this.Connected = true;
                    return true;
                }

                //connection failed
                ConnectionState = ConnectionState.Error;
                return false;
            }
            catch (EndpointNotFoundException)
            {
                ConnectionState = ConnectionState.Error;
                return false;
            }
        }

        public bool Disconnect()
        {
            Log.Info("Generic Simulator: disconnect");

            DisposeEventPollingTimer();
            DisposeKeepAliveTimer();

            if (Client == null || ConnectionState != ConnectionState.Disposed)
            {
                Log.Warn("Not connected");
                ConnectionState = ConnectionState.Disposed;
                return true;
            }

            bool success;

            try
            {
                success = Client.Logout();
                if (!success)
                {
                    Log.Warn("Failed to log out");
                }
            }
            catch (Exception ex)
            {
                success = false;
                Log.Error(ex.Message, ex);
            }

            ConnectionState = ConnectionState.Disposed;
            return success;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public GenericApiDevice GetDevice(string id, GenericAssetType type)
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                Log.Error($"Cannot get {type} - not connected to server");
                return null;
            }

            switch (type)
            {
                case GenericAssetType.Device:
                    return ToDevice(Client.GetDevice(id));
            }
            return null;
        }
        private GenericApiDevice ToDevice(GenericDeviceInfo info)
        {
            if (info == null)
            {
                return null;
            }

            return new GenericApiDevice(info.ID, info.Name);
        }

        public static EndpointAddress GetSimulatorServiceEndpointAddress(string host, int port) //, int recorderId)
        {
            return new EndpointAddress($"http://{host}:{port}/ServerService.svc");
        }

        ////--------- protected methods --------

        protected virtual void OnConnected(object sender, EventArgs args)
        {
            ConnectedEvent?.Invoke(this, args);
        }
        protected virtual void OnGenericEvent(object sender, GenericEventArgs args)
        {
            GenericEvent?.Invoke(this, args);
        }

        protected virtual void OnTamper(object sender, GenericTamperEventArgs args)
        {
            TamperEvent?.Invoke(this, args);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();

                Log.Info("Simulator: shutting down WCF client");
                Client?.Close();
                Client = null;
            }
        }

        #region Keep alive Timer

        private void StartKeepAliveTimer()
        {
            if (_keepAliveTimer != null)
            {
                Log.Debug($"{_ip}: Keep Alive timer is already running");
                return;
            }

            //start keep alive loop
            Log.Info($"{_ip}: Start keep alive timer");

            _keepAliveTimer = new SafeTimer(true, KeepAliveIntervalMsec, "Generic server connectivity check Timer");
            _keepAliveTimer.Elapsed += KeepAliveTimerElapsed;
            _keepAliveTimer.IntervalMilliseconds = KeepAliveIntervalMsec;
            _keepAliveTimer.Enabled = true;
        }

        private void DisposeKeepAliveTimer()
        {
            if (_keepAliveTimer == null)
            {
                return;
            }

            Log.Debug($"{_ip}: Dispose Keep alive Timer");

            Task.Run(() =>
            {
                _keepAliveTimer.Elapsed -= KeepAliveTimerElapsed;
                _keepAliveTimer.Enabled = false;
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
            });
        }

        private void KeepAliveTimerElapsed(object sender, EventArgs e)
        {
            if (!RadarServer.VerifyHost(_ip, _port))
            {
                if (ConnectionState != ConnectionState.Connected)
                {
                    return;
                }

                ConnectionState = ConnectionState.Error;
                Log.Warn($"Generic {_ip}:{_port} disconnected");
            }
            else if (ConnectionState == ConnectionState.Error)
            {
                //connection restored
                Log.Info($"Generic {_ip}:{_port} became available, re-connecting...");
                Task.Run(() => Connect(string.Empty, _ip, _port, _username, _password, out _error));
            }
        }

        #endregion

        #region Event Polling and handling

        private void StartEventPollingTimer()
        {
            if (_eventPollingTimer != null)
            {
                Log.Debug($"{_ip}: Event polling timer is already running");
                return;
            }

            _lastEventPollEndTime = DateTime.UtcNow.AddHours(-1);

            //start event polling loop
            Log.Info($"{_ip}: Start event polling timer, assume last polling time: {_lastEventPollEndTime}");

            _eventPollingTimer = new SafeTimer(true, EventPollingIntervalMsec, "Generic event polling Timer");
            _eventPollingTimer.Elapsed += EventPollingTimerElapsed;
            _eventPollingTimer.IntervalMilliseconds = EventPollingIntervalMsec;
            _eventPollingTimer.Enabled = true;
        }

        private void DisposeEventPollingTimer()
        {
            if (_eventPollingTimer == null)
            {
                return;
            }

            Log.Debug($"{_ip}: Dispose Event Polling Timer");

            Task.Run(() =>
            {
                _eventPollingTimer.Elapsed -= EventPollingTimerElapsed;
                _eventPollingTimer.Enabled = false;
                _eventPollingTimer.Dispose();
                _eventPollingTimer = null;
            });
        }

        private void EventPollingTimerElapsed(object sender, EventArgs e)
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            var startTime = _lastEventPollEndTime;
            var endTime = DateTime.UtcNow;
            var lastEventTime = DateTime.MinValue;

            try
            {
                var events = Client.GetEvents(startTime, endTime);
                if (events == null)
                {
                    Log.Error("Failed to poll events - no results received");
                    return;
                }

                if (events.Any())
                {
                    lastEventTime = events.Last().TimestampUtc;
                }

                _lastEventPollEndTime = lastEventTime == DateTime.MinValue ? DateTime.UtcNow : lastEventTime.AddMilliseconds(1);

                Task.Run(() =>
                {
                    foreach (var evt in events)
                    {
                        HandleEvent(evt);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to poll for events: {ex.Message}", ex);
            }
        }

        private void HandleEvent(GenericEvent @event)
        {
            Log.Debug(@event);

            switch (@event.Type)
            {
                case GenericEventType.GenericEvent:
                    HandleGenericEvent(@event);
                    break;
                case GenericEventType.TamperStart:
                case GenericEventType.TamperEnd:
                    HandleTamper(@event);
                    break;
            }
        }

        private void HandleGenericEvent(GenericEvent @event)
        {
            var asset = GetDevice(@event.DeviceId, @event.AssetType);
            GenericEventArgs genericEventArgs;
            if (asset == null)
            {
                Log.Info($"A generic event raised on server");
                genericEventArgs = new GenericEventArgs(@event.TimestampUtc, null);
            }
            else
            {
                Log.Info($"A generic event raised on device {@event.DeviceId}");
                genericEventArgs = new GenericEventArgs(@event.TimestampUtc, @event.DeviceId,
                    @event.GeoSpatialEventData.Latitude, @event.GeoSpatialEventData.Longitude, @event.GeoSpatialEventData.Altitude);
            }

            OnGenericEvent(this, genericEventArgs);
        }
        private void HandleTamper(GenericEvent @event)
        {
            var asset = GetDevice(@event.DeviceId, @event.AssetType);
            if (asset == null)
            {
                Log.Warn($"Cannot handle {@event.Type} event - {@event.AssetType} [{@event.DeviceId}] not found");
                return;
            }

            var eventType = @event.Type == GenericEventType.TamperStart ? GenericAlarmStatus.Start : GenericAlarmStatus.End;
            OnTamper(this, new GenericTamperEventArgs(@event.TimestampUtc, @event.DeviceId, @event.Description, eventType,
                @event.GeoSpatialEventData.Latitude, @event.GeoSpatialEventData.Longitude, @event.GeoSpatialEventData.Altitude));
        }
        public bool IsServerAvailable()
        {
            if (!RadarServer.VerifyHost(_ip, _port))
            {
                if (ConnectionState != ConnectionState.Connected)
                {
                    return false;
                }

                ConnectionState = ConnectionState.Error;
                Log.Warn($"Generic {_ip}:{_port} disconnected");
            }

            return true;
        }
        public async Task<GenericApiDevice> GetDevicesById(string deviceId)
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                Log.Error($"Cannot get {deviceId} - not connected to server");
                return null;
            }

            return ToDevice(await Client.GetDeviceAsync(deviceId));
        }
        public async Task<IEnumerable<GenericApiDevice>> GetCameraList()
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                Log.Error("Cannot get Inputs - not connected to server");
                return null;
            }

            var devices = await Client.GetDevicesAsync();
            var ret = new List<GenericApiDevice>();
            foreach (var device in devices)
            {
                ret.Add(ToDevice(device));
            }

            return ret;
        }

        public Task<IEnumerable<GenericApiDevice>> GetRadarList()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GenericApiDevice>> GetZoneList()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GenericApiDevice>> GetUDCList()
        {
            throw new NotImplementedException();
        }

        public void OnAlarmTrackRecieved(AlarmTrackRecievedArgs e)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GenericApiDevice>> GetPushEventList()
        {
            throw new NotImplementedException();
        }

        public Task PollRadarAlarmTrackRecievedEvent(int pollingInterval, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task PollZoneTrackUpdateEvent(int pollingInterval, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}