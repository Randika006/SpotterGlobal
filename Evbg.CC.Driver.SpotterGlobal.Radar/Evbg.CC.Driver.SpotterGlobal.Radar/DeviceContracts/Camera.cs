using CNL.IPSecurityCenter.Driver;
using Evbg.CC.Driver.SpotterGlobal.Radar;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using Evbg.CC.Driver.SpotterGlobal.Radar.DeviceContracts;
using log4net;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Evbg.CC.Driver.SpotterGlobal.Camera
{
    /// <summary>
    /// Represents Generic Device
    /// </summary>
    [Serializable]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Camera : CameraBase, ICamera, IDeserializationCallback, INotifyPropertyChanged
    {
        [NonSerialized]
        private ILog _log;

        [NonSerialized]
        private IGenericParent _genericParent;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public Camera()
        {
            Interfaces.Add(new DeviceInterface(DeviceInterfaceType.Other, "GenericDevice Output"));
            InitializeFields();
        }



        #region Public Methods

        /// <summary>
        /// Runs when the entire object graph has been deserialized.
        /// </summary>
        /// <param name="sender">The object that initiated the callback. The functionality for this parameter is not currently implemented.</param>
        public void OnDeserialization(object sender)
        {
            InitializeFields();
        }
        public void SetState(DeviceState state, string message = null)
        {
            if (!Enabled)
            {
                return;
            }

            _log.Debug($"'{Label}' - set state to {state}");

            if (message == null)
            {
                OnStateChanged(state);
            }
            else
            {
                OnStateChanged(state, message);
            }
        }

        /// <summary>
        /// Notifies the connection manager that a property of this device has changed, which subsequently serializes the device object and writes to the database.<para/>
        /// </summary>
        public void SaveChangedProperties(string propertyName)
        {
            _log.Debug($"{Label}: Save properties");

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Updates the properties when device is added or connected
        /// </summary>
        public void UpdateProperties()
        {
            //// Update Properties Logic 
        }

        /// <summary>
        /// Handle the generic event for the device
        /// </summary>
        /// <param name="e"></param>
        public void HandleGenericEvent(GenericEventArgs e)
        {
            DeviceEventEventArgs deviceEventArgs = new DeviceEventEventArgs(this, e.Timestamp)
            {
                Description = e.Description,
                Altitude = e.Altitude,
                Longitude = e.Longitude,
                Latitude = e.Latitude,
                SpatialReferenceIdentifier = this.SpatialReferenceIdentifier
            };
            OnDeviceEventEvent(deviceEventArgs);
        }

        private void OnDeviceEventEvent(DeviceEventEventArgs deviceEventArgs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handle the Tamper Event for the device
        /// </summary>
        /// <param name="e"></param>
        //public void HandleTamper(GenericTamperEventArgs e)
        //{
        //    if (!Enabled)
        //    {
        //        return;
        //    }

        //    _log.Info(e);
        //    //raise Tamper event
        //    OnTamperEvent(new TamperEventArgs(this, e.Timestamp)
        //    {
        //        TamperStatus = e.Status == GenericAlarmStatus.Start ? TamperStatus.Start : TamperStatus.End,
        //        StatusDescription = e.Status == GenericAlarmStatus.Start ? Events.TamperStart : Events.TamperEnd,
        //        Description = e.Description,
        //        Latitude = e.Latitude,
        //        Longitude = e.Longitude,
        //        Altitude = e.Altitude,
        //        SpatialReferenceIdentifier = this.SpatialReferenceIdentifier
        //    });

        //    if (e.Status == GenericAlarmStatus.Start)
        //    {
        //        OnCustomStateChanged(new SampleState(), Events.TamperStart);
        //    }
        //    else
        //    {
        //        UpdateDeviceState();
        //    }
        //}

        /// <summary>
        /// Inject the server. Only required for Unit Tests
        /// </summary>
        /// <param name="genericParent"></param>
        public void InjectGetServerInterface(IGenericParent genericParent)
        {
            //Only required for Unit Tests
            _genericParent = genericParent;
        }

        #endregion

        #region Private Methods
        private void InitializeFields()
        {
            _log = LogManager.GetLogger(typeof(Camera));
            _genericParent = new GenericParent();
            EnabledChanged += GenericDeviceEnabledChanged;
        }

        private void GenericDeviceEnabledChanged(object sender, EventArgs e)
        {
            UpdateDeviceState();
        }

        private void UpdateDeviceState()
        {
            _log.Debug($"{Label}: Update device state");

            ////poll for the current state
            var server = _genericParent.GetGenericServer(this);
            if (server == null)
            {
                OnStateChanged(DeviceState.Failed, Messages.ParentDeviceNotFound);
            }
            else
            {
                IGenericApi api = server.GetApi();
                if (api == null || !api.IsServerAvailable())
                {
                    OnStateChanged(DeviceState.Failed, Messages.NotConnectedToGenericServer);
                    return;
                }

                var apiDevice = api.GetDevicesById(Id);

                if (apiDevice == null)
                {
                    OnStateChanged(DeviceState.Failed, Messages.DeviceNotFound);
                }
                else if (apiDevice.Result != null && apiDevice.Result.IsTamper)
                {
                    OnCustomStateChanged(new SampleState(), Events.Tamper);
                }
                else
                {
                    OnStateChanged(DeviceState.Online);
                }
            }
        }

        /// <summary>
        /// Get the connected server for the device
        /// </summary>
        /// <returns></returns>
        public RadarServer GetConnectedServer()
        {
            return GetConnectedParentDevice<RadarServer>();
        }

        public void SampleDeviceMethod()
        {
            throw new NotImplementedException();
        }
        #endregion

        //public override void SampleDeviceMethod()
        //{
        //    //Sample method
        //}
    }
}
