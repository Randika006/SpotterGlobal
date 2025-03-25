using CNL.IPSecurityCenter.Driver;
using CNL.IPSecurityCenter.Driver.Exceptions;
using CNL.IPSecurityCenter.Driver.Extensions;
using CNL.IPSecurityCenter.Driver.Utility.Logging;
using CNL.IPSecurityCenter.Driver.Utility.Threading;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    /// <summary>
    /// Implementation of Generic server device Contract IManagementServer, represents the main connection point to Access Control system.
    /// </summary>
    [Serializable]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class RadarServer : RadarServerBase, IRadarServer, IDeserializationCallback, IDdkDevice
    {
        #region private fields

        //prevent Connect() and Disconnect() overlap
        [NonSerialized]
        private object _lockInstance;

        [NonSerialized]
        private DriverLog _log;

        [NonSerialized]
        private bool _disposed;

        [NonSerialized]
        protected IGenericApi _api;

        [NonSerialized]
        private SafeTimer _keepAliveTimer;

        [NonSerialized]
        private DevicePopulation _devicePopulation;

        private LogLevel _logLevel;
        private string Username;
        private string Password;

        [NonSerialized]
        CancellationTokenSource _pollingCancellationTokenSource;

        #endregion

        public new LogLevel LogLevel
        {
            get => _logLevel;
            set
            {
                if (_logLevel != value)
                {
                    _log?.Info($"Generic server '{Label}': Logging level changed to: {value}");

                    _logLevel = value;
                    LogManagerStore.GetLogManager(Identifier).LogLevel = _logLevel;
                }
            }
        }

        public ILog Log => _log;

        //unused, only implemented to comply with IDdkDevice
        public string Id => null;


        public RadarServer()
        {
            OnDeserialization(null);
        }

        //// --------------- public methods ------------------------

        /// <summary>
        /// Runs when the entire object graph has been deserialized.
        /// </summary>
        /// <param name="sender">The object that initiated the callback. The functionality for this parameter is not currently implemented.</param>
        public void OnDeserialization(object sender)
        {
            InitializeFields();
        }

        public IGenericApi GetApi()
        {
            return _api;
        }

        /// <summary>
        /// Connect to the Generic server, populate child device upon connection
        /// </summary>
        public override void Connect()
        {
            lock (_lockInstance)
            {
                Disconnect();
            }

            ConnectInternal();
        }

        protected async void ConnectInternal()
        {
            try
            {
                lock (_lockInstance)
                {
                    SetProperties();
                    Log.Info($"Connecting to Generic Server: {IP}:{Port}");

                    //set device Online immediately after connecting to stop timer in Connection Manager
                    
                        _api = CreateApi();
                    

                   
                    _api.GenericEvent += GenericEventHandler;
                    _api.TamperEvent += GenericTamperHandler;
                    //_api.TrackUpdate += DeviceEventHandler;

                    _api.AlarmTrackRecieved += OnAlarmTrackRecieved;
                    _api.TrackUpdate += OnTrackUpdate;

                    //establish connection/log on to the Generic server
                    if (!_api.Connect(null, IP, Port, Username, Password, out var error))
                    {
                        throw new FatalDriverException(error);
                    }

                    //this is especially important on large-scale systems where device population and state sync takes a long time
                    OnStateChanged(DeviceState.Online);

                    Log.Info($"Connected successfully to Generic server: {IP}:{Port}");

                    StartKeepAliveTimer();

                    //device population object is created only once
                    if (_devicePopulation == null)
                    {
                        _devicePopulation = new DevicePopulation(this, _log);
                    }

                    //have to set the API and the batch size for every connection
                    _devicePopulation.Initialize(_api, DevicePopulationBatchSize);

                    if (!_devicePopulation.PopulateCameraListAsync(true).Result)
                    {
                       return;
                    }

                    if (!_devicePopulation.PopulateRadarListAsync(true).Result)
                    {
                        return;
                    }

                    if (!_devicePopulation.PopulateZoneListAsync(true).Result)
                    {
                        return;
                    }

                    if (_devicePopulation.PopulateUDCListAsync(true).Result)
                    {
                        StartPollinTasks();
                        return;
                    }

                    //if population was cancelled - don't report error
                    if (_devicePopulation != null && !_devicePopulation.Cancelled)
                    {
                        OnStateChanged(DeviceState.Failed, Messages.FailedToPopulatePanels);
                    }
                }
            }
            catch (FatalDriverException ex)
            {
                Log.Error(ex.Message, ex);

                //setting connectable device to Failed state will make Connection Manager to automatically re-connect
                OnStateChanged(DeviceState.Failed, ex.Message);
                Disconnect();
            }
            catch (Exception ex)
            {
                //catch generic Exception for unexpected exceptions which might crash the hosting service
                Log.Error(ex.Message, ex);

                //setting connectable device to Failed state will make Connection Manager to automatically re-connect
                OnStateChanged(DeviceState.Failed, Messages.DeviceConnectionFailed);
                Disconnect();
            }
        }

      

        // public event EventHandler<AlarmTrackRecievedArgs> AlarmTrackRecievedEvent;

        // Method to raise the event
        //protected virtual void OnAlarmTrackRecievedEvent(AlarmTrackRecievedArgs e)
        //{
        //    AlarmTrackRecievedEvent?.Invoke(this, e);
        //}
        private void OnAlarmTrackRecieved(object sender, AlarmTrackRecievedArgs e)
        {
            if (e == null)
            {
                _log.Error($"'{Label}': Cannot handle generic event - argument not proided ");
            }
            if (!string.IsNullOrEmpty(e.DeviceId))
            {
                var handlingDevice = GetRadarDeviceHandlingEvent(e.DeviceId);
                if (handlingDevice == null)
                {
                    _log.Error($"'{Label}': Cannot handle generic event - handling device not found");
                    return;
                }
                handlingDevice.OnAlarmTrackRecievedEvent(new AlarmTrackRecievedEventArgs(handlingDevice)
                {
                    TrackId = e.TrackId,
                    Description = e.Description,
                    ObjectCategory = e.ObjectCategory,
                    Confidence = e.Confidence,
                    Longitude = e.Longitude.HasValue ? e.Longitude.Value : 0,
                    Latitude = e.Latitude.HasValue ? e.Latitude.Value : 0,
                    Bearing = e.Bearing,
                    Velocity = e.Velocity,
                    IntialAlarmDateTime = e.Timestamp
                });
                return;
            }
           
            return;
        }

        private void OnTrackUpdate(object sender, TrackUpdateArgs e)
        {
            if (e == null)
            {
                _log.Error($"'{Label}': Cannot handle generic event - argument not proided ");
            }
            if (!string.IsNullOrEmpty(e.ZoneDeviceId))
            {
                var handlingDevice = GetZoneDeviceHandlingEvent(e.ZoneDeviceId);
                if (handlingDevice == null)
                {
                    _log.Error($"'{Label}': Cannot handle generic event - handling device not found");
                    return;
                }
                handlingDevice.OnTrackUpdateEvent(new TrackUpdateEventArgs(handlingDevice)
                {
                    Description =e.Description,
                    Bearing = e.Bearing,
                    Velocity = e.Velocity,
                    Altitude = e.Altitude.HasValue ? e.Altitude.Value : 0

                }); 
                return;
            }
            return;
        }


        //private void DeviceEventHandler(object sender, TrackUpdateArgs data)
        //{
        //    if (data == null)
        //    {
        //        _log.Error($"'{Label}': Cannot handle generic event - arguments not provided");
        //        return;
        //    }

        //    //foreach (var radarDevice in data.)
        //    //{
        //    //    if (radarDevice.Source == null)
        //    //    {
        //    //        //Generic event with device id as null is a server level event so raise on server device
        //    //        _log.Error($"'{Label}': Cannot handle radar source not found");

        //    //        return;
        //    //    }
        //        var handlingRadarDevice = GetRadarDeviceHandlingEvent(data.RadarDeviceId);

        //        if (handlingRadarDevice == null)
        //        {
        //            _log.Error($"'{Label}': Cannot handle radar event - handling device not found");
        //            return;
        //        }

        //        handlingRadarDevice.OnAlarmTrackRecievedEvent(new AlarmTrackRecievedEventArgs(handlingRadarDevice)
        //        {
        //         TrackId=data.TrackId,
        //         ObjectCategory=data.ObjectCategory,
        //         Confidence=data.Confidence,
        //         Longitude=data.Longitude,
        //         Bearing = data.Bearing,
        //         Velocity = data.Velocity,
        //         IntialAlarmDateTime =data.InitialAlarmDateTime,

        //       });



        //        // A device event arguments with the device properties
        //        //handlingRadarDevice.HandleAlarmTrackRecieved(RadarDeviceId);



        //    //}
        //}




        /// <summary>
        /// Created a api for the server
        /// </summary>
        /// <returns></returns>
        /// 


        protected virtual IGenericApi CreateApi()
        {
            return new GenericApi(IP, Port);
        }

        /// <summary>
        /// Verify Host availability by TCP, returns true is available, false - otherwise
        /// </summary>
        public static bool VerifyHost(string server, int port)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    tcpClient.Connect(server, port);
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        #region Event handlers

        /// <summary>
        /// Handles the event from api
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void GenericEventHandler(object sender, GenericEventArgs e)
        {
            if (e == null)
            {
                _log.Error($"'{Label}': Cannot handle generic event - arguments not provided");
                return;
            }
            if (e.DeviceId == null)
            {
                //Generic event with device id as null is a server level event so raise on server device
                OnServerEventEvent(new ServerEventEventArgs(this, e.Timestamp) { Description = e.Description });
                return;
            }

            //var handlingDevice = GetZoneDeviceHandlingEvent(e.DeviceId);
            //if (handlingDevice == null)
            //{
            //    _log.Error($"'{Label}': Cannot handle generic event - handling device not found");
            //    return;
            //}
            //// A device event arguments with the device properties
            //handlingDevice.HandleGenericEvent(e);
        }

        /// <summary>
        /// Handles the Tamper Event from api
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void GenericTamperHandler(object sender, GenericTamperEventArgs e)
        {
            if (e == null || e.DeviceId == null)
            {
                _log.Error($"'{Label}': Cannot handle generic Tamper event - device not provided");
                return;
            }

            //var handlingDevice = GetRadarDeviceHandlingEvent(e.DeviceId);
            //if (handlingDevice == null)
            //{
            //    _log.Error($"'{Label}': Cannot handle generic event - handling device not found");
            //    return;
            //}

            //handlingDevice.HandleTamper(e);
        }

        /// <summary>
        /// Gets the Device for the Event
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        protected virtual Radar GetRadarDeviceHandlingEvent(string deviceId)
        {
            return GetConnectedChildDevice<Radar>(CustomIds.GetGenericCustomId(deviceId));
        }

        protected virtual Zone.Zone GetZoneDeviceHandlingEvent(string deviceId)
        {
            return GetConnectedChildDevice<Zone.Zone>(CustomIds.GetGenericCustomId(deviceId));
        }

        #endregion

        /// <summary>
        /// Disconnect from the Generic server and dispose any unneeded resources
        /// </summary>
        public override void Disconnect()
        {
            Log.Info(Messages.Disconnecting.InvariantFormat(IP, Port));

            lock (_lockInstance)
            {
                //cancel device population if in progress
                _devicePopulation?.CancelPopulation();
                DisposeKeepAliveTimer();
                DisposeSdkSession();
            }
            Log.Info(Messages.Disconnected.InvariantFormat(IP));
        }

        public override bool RefreshDevices(bool refreshProperties)
        {
            _log.Info($"'{Label}': Refresh devices, refresh device properties - {refreshProperties}");

            if (_devicePopulation == null || _api == null || !_api.Connected)
            {
                _log.Info($"'{Label}': Failed to refresh devices - not connected");
                return false;
            }

            var devicePopulationQueue = DevicePopulationQueue.Instance(_log);
            devicePopulationQueue.Enqueue(async () => { await _devicePopulation.PopulateCameraListAsync(refreshProperties); });

            return true;
        }

       
        public override void SampleServerMethod()
        {
            //Sample method
        }

        #region IDDKDevice

        public virtual T GetConnectedChildDevice<T>(string customIdentifier)
            where T : IDevice
        {
            return GetConnectedDevice<T>(customIdentifier);
        }

        public void SetState(DeviceState state, string message)
        {
            if (!Enabled)
            {
                return;
            }

            _log.Debug($"'{Label}' - set state to {state} - '{message}'");

            if (message == null)
            {
                OnStateChanged(state);
            }
            else
            {
                OnStateChanged(state, message);
            }
        }

        public virtual void RePopulateDevice<T>(string customIdentifier, T device)
            where T : IDevice
        {
            Interfaces[customIdentifier].Connect(device.Interfaces[0]);
        }

        public virtual void AddAndConnectRange(InterfaceConnectionCollection connections)
        {
            _log.Debug($"{Label}: AddAndConnectRange");

            Interfaces.AddAndConnectRange(connections);
        }

        #endregion

        //// --------------- protected methods ------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }

                _disposed = true;
            }
        }

        //// --------------- private methods ------------------------

        /// <summary>
        /// Initializes the fields used by the instance.
        /// </summary>
        /// <remarks>
        /// NonSerialized fields should be initialized in this method otherwise
        /// they will only be initialized the first time the device is instantiated
        /// and will not initialize when a device instance is deserialized;
        /// </remarks>
        private void InitializeFields()
        {
            _lockInstance = new object();
            LogManagerStore.GetLogManager(Identifier).LogLevel = _logLevel;
            _log = LogManagerStore.GetLogManager(Identifier).GetLogger(typeof(RadarServer));
        }

        /// <summary>
        /// Set default values for device properties
        /// </summary>
        private void SetProperties()
        {
            //assign properties to default values if needed
            Port = DeviceDefaults.DefaultPort(this);
            DevicePopulationBatchSize = DeviceDefaults.DefaultDevicePopulationBatchSize(this);

            //print the properties for debugging purposes
            _log.Debug($"Port - {Port}");
            _log.Debug($"Device population batch size - {DevicePopulationBatchSize}");
        }

        private void DisposeSdkSession()
        {
            if (_api == null)
            {
                return;
            }

            _log.Debug($"{Label}: Dispose SDK session");

            _api.GenericEvent -= GenericEventHandler;
            _api.TamperEvent -= GenericTamperHandler;
            _api.AlarmTrackRecieved -= OnAlarmTrackRecieved;
            _api.TrackUpdate -= OnTrackUpdate;

            //disconnect from the Generic server, free all resources.
            _api.Disconnect();
            _api.Dispose();
            _api = null;

        }

        #region Keep alive Timer

        private void StartKeepAliveTimer()
        {
            //start keep alive loop
            if (KeepAliveInterval == -1)
            {
                _log.Debug("No keep alive checks");
            }
            else
            {
                _log.Info($"'{Label}': Start keep alive timer");

                var keepAliveIntervalMsec = DeviceDefaults.DefaultKeepAliveInterval(this) * 1000;

                _keepAliveTimer = new SafeTimer(true, keepAliveIntervalMsec, "Generic server connectivity check Timer");
                _keepAliveTimer.Elapsed += KeepAliveTimerElapsed;
                _keepAliveTimer.IntervalMilliseconds = keepAliveIntervalMsec;
                _keepAliveTimer.Enabled = true;
            }
        }

        private void DisposeKeepAliveTimer()
        {
            if (_keepAliveTimer != null)
            {
                _log.Debug($"'{Label}': Dispose Keep alive Timer");

                //TODO: explain
                Task.Run(() =>
                {
                    _keepAliveTimer.Enabled = false;
                    _keepAliveTimer.Elapsed -= KeepAliveTimerElapsed;
                    _keepAliveTimer.Dispose();
                    _keepAliveTimer = null;
                });
            }
        }

        private void KeepAliveTimerElapsed(object sender, System.EventArgs e)
        {
            //if there is a connection problem the session class will raise Disconnected event
            _api?.IsServerAvailable();
        }



        private void StartPollinTasks()
        {
            try
            {
                _log.Info("Starting polling tasks...");

                StopPollingTasks();
                _log.Debug("Stopped any existing polling tasks.");

                _pollingCancellationTokenSource = new CancellationTokenSource();
                var token = _pollingCancellationTokenSource.Token;

                if (PollingInterval > 0)
                {
                    _log.Debug($"Starting a new polling task with an interval of {PollingInterval} ms.");
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            //_api.PollEventConnectivity(PollingInterval, token);
                            _api.PollRadarAlarmTrackRecievedEvent(PollingInterval,token);
                            //_api.PollZoneTrackUpdateEvent(PollingInterval, token);
                        }
                        catch (OperationCanceledException)
                        {
                            _log.Info("Polling task was canceled.");
                        }
                        catch (Exception ex)
                        {
                            _log.Error("An error occurred in the polling task.", ex);
                        }
                    }, token);
                }
                else
                {
                    _log.Warn("Polling task was not started because the polling interval is set to 0.");
                }

                _log.Info("Polling threads have been successfully started.");
            }
            catch (Exception ex)
            {
                _log.Error("Failed to start polling tasks.", ex);
            }
        }

        /// <summary>
        /// Stops any active polling tasks by canceling the associated cancellation token and cleaning up resources.
        /// </summary>
        private void StopPollingTasks()
        {
            if (_pollingCancellationTokenSource != null)
            {
                _pollingCancellationTokenSource?.Cancel();
                _pollingCancellationTokenSource?.Dispose();
                _pollingCancellationTokenSource = null;
                //DisposeSusbcriptionRenewalTimer();
                _log.Debug("Polling threads stoped");
            }
        }



        #endregion
    }
}
