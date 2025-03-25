using CNL.IPSecurityCenter.Driver;
using CNL.IPSecurityCenter.Driver.Attributes;
using CNL.IPSecurityCenter.Driver.ServiceLocation;
using Evbg.CC.Driver.SpotterGlobal.Radar;
using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using System.Collections.Generic;
using System.Linq;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    public class GenericServerMock : RadarServer
    {
        //injected implementation of IDeviceRepository
        public IDeviceRepository DeviceRepository { get; set; }
        public IList<IDevice> PopulatedDevices { get; } = new List<IDevice>();

        public IList<IRadar> DevicesAdded { get; } = new List<IRadar>();

        public void InjectApiAndConnect(IGenericApi api)
        {
            _api = api;
            ConnectInternal();
        }

        protected override IGenericApi CreateApi()
        {
            return _api;
        }

        public override void RePopulateDevice<T>(string customIdentifier, T device)
        {
            //test device re-populated only once
            PopulatedDevices.Add(device);
        }

        public override T GetConnectedChildDevice<T>(string customIdentifier)
        {
            var deviceId = DeviceRepository.GetConnectedDeviceIdentifier(Identifier, customIdentifier);
            return DeviceRepository.Read<T>(deviceId);
        }

        
    }
}