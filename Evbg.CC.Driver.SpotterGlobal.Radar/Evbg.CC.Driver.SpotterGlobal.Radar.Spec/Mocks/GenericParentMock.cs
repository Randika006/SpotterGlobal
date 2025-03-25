using Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity;
using Evbg.CC.Driver.SpotterGlobal.Radar.DeviceContracts;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec.Mocks
{
    public class GenericParentMock : IGenericParent
    {
        private readonly RadarServer _server;
        public GenericParentMock(RadarServer genericServer)
        {
            _server = genericServer;
        }
        public RadarServer GetGenericServer(Radar d)
        {
            return _server;
        }

        public RadarServer GetGenericServer(Zone.Zone zone)
        {
            return _server;
        }

        public RadarServer GetGenericServer(UDC.DeviceContracts.UDC udc)
        {
            return _server;
        }

        public RadarServer GetGenericServer(Camera.Camera camera)
        {
            return _server;
        }

    }
}
