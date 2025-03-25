using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Evbg.CC.Driver.SpotterGlobal.Radar.API.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec.Mocks
{
    public class GenericApiMock : GenericApi
    {
        public GenericApiMock(string ip, int port, IList<GenericApiDevice> devices)
        : base(ip, port)
        {
            _devices = devices;
        }

        public void RaiseGenericEvent(GenericEventArgs e)
        {
            OnGenericEvent(e);
        }

        public void RaiseTamper(GenericTamperEventArgs e)
        {
            OnTamper(e);
        }
    }
}
