using CNL.IPSecurityCenter.Driver;
using CNL.IPSecurityCenter.Driver.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    public class DeviceRepositoryMock : IDeviceRepository
    {
        private ConnectionManagerMock _cm;

        public DeviceRepositoryMock(ConnectionManagerMock cm)
        {
            _cm = cm;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IDevice Read(Guid identifier) => _cm.ReadDevice(identifier);

        public TContract Read<TContract>(Guid identifier)
        {
            return (TContract)_cm.Devices.FirstOrDefault(d => d.Identifier == identifier && d.GetType().Equals(typeof(TContract)));
        }

        public TContract ReadStale<TContract>(Guid identifier)
        {
            throw new NotImplementedException();
        }

        public Guid GetConnectedDeviceIdentifier(Guid deviceIdentifier, string customIdentifier)
        {
            //for now assume there is only one parent device, so ignore deviceIdentifier parameter
            if (!_cm.ConnectedInterfaces.TryGetValue(customIdentifier, out var connection))
            {
                return default;
            }

            if (connection.DeviceInterface2.Parent.Parent == null)
            {
                return default;
            }

            return connection.DeviceInterface2.Parent.Parent.Identifier;
        }

        public Guid GetConnectedDeviceIdentifier(DeviceInterface deviceInterface)
        {
            throw new NotImplementedException();
        }

        public List<TContract> ReadAll<TContract>()
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cm = null;
            }
        }
    }
}