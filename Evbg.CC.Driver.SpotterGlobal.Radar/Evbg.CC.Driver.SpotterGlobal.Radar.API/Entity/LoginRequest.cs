using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{
    public class LoginRequest
    {
        internal string password;

        public string userName { get; set; }
        public string username { get; internal set; }
        public string Password { get; set; }

    }
}
