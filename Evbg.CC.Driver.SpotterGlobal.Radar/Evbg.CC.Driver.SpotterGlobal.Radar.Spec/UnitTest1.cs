using Evbg.CC.Driver.SpotterGlobal.Radar.API;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    [TestClass]
    public class UnitTest1
    {
        //private readonly IHttpClientWrapper _httpClientWrapper;
        //private readonly ILogger _log;
        //const string _Ip = "127.0.0.1";
        //const string _port = "8080";
        //const string _session = "session123";
       // public CancellationToken _ct = CancellationToken.None;
        public int port = 443;
        //public string ip = "917c6688-25d8-4fc9-be9a-7efdc44775bc.mock.pstmn.io";
        public string ip = "spotterproject.free.beeceptor.com";
        public string username = "root";
        public string password = "pass";
        public string exampleProperty = "";


        [TestMethod]
        public void TestMethodCamera()
        {
            var api = new GenericApi(ip,port);
            string error = string.Empty;
            var result = api.Connect("", ip, port, username, password, out error);
            Assert.IsTrue(result);

            var devices = api.GetCameraList().Result;
        }
        [TestMethod]
        public void TestMethodZone()
        {
            var api = new GenericApi(ip, port);
            string error = string.Empty;
            var result = api.Connect(exampleProperty, ip, port, username, password, out error);
            Assert.IsTrue(result);

            var devices = api.GetZoneList().Result;
        }


        [TestMethod]
        public void TestMethodUDC()
        {
            var api = new GenericApi(ip, port);
            string error = string.Empty;
            var result = api.Connect(exampleProperty, ip, port, username, password, out error);
            Assert.IsTrue(result);

            var devices = api.GetUDCList().Result;
        }



        [TestMethod]
        public void TestMethodRadar()
        {
            var api = new GenericApi(ip, port);
            string error = string.Empty;
            var result = api.Connect(exampleProperty, ip, port, username, password, out error);
            Assert.IsTrue(result);

            var devices = api.GetRadarList().Result;
        }

        [TestMethod]
        public void TestMethodPushEvent()
        {
            var api = new GenericApi(ip, port);
            string error = string.Empty;
            var result = api.Connect(exampleProperty, ip, port, username, password, out error);
            Assert.IsTrue(result);

            var events = api.GetPushEventList().Result;
        }

    }
}
