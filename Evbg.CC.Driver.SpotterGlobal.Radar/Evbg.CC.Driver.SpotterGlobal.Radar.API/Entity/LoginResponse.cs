using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{

    public class LoginResponse
    {
        public bool IsSuccessful { get; set; }
        public string SessionToken { get; set; }
        public string Message { get; set; }
    }
   
 
}
