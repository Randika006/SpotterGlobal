using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.DeviceContracts
{
    public class GenericParent : IGenericParent
    {
        public RadarServer GetGenericServer(Radar d)
        {
            return d.GetConnectedServer();
        }

        public RadarServer GetGenericServer(UDC.DeviceContracts.UDC udc)
        {
            return udc.GetConnectedServer();
        }

        public RadarServer GetGenericServer(Camera.Camera camera)
        {
            return camera.GetConnectedServer();

        }

        public RadarServer GetGenericServer(Zone.Zone zone)
        {
            return zone.GetConnectedServer();
        }
    }
}
