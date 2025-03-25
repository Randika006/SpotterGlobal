using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{
   public class UDCResultResponse
   {
        [JsonProperty("result")]
        public Dictionary<string, UDCDevice> Result { get; set; }
        //  public List<UDCDevice> Data { get; set; }
    }

    public class UDCDevice
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ready")]
        public bool Ready { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("licenseType")]
        public string LicenseType { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public int? Port { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("poll")]
        public bool Poll { get; set; }

        [JsonProperty("usePublicAddr")]
        public bool UsePublicAddr { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("serial")]
        public string Serial { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("preset")]
        public string Preset { get; set; }

        [JsonProperty("geolocation")]
        public UDCGeolocation Geolocation { get; set; }
    }

    public class UDCGeolocation
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("altitude")]
        public double Altitude { get; set; }

        [JsonProperty("bearing")]
        public double Bearing { get; set; }

        [JsonProperty("heading")]
        public double Heading { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("verticalSpeed")]
        public double VerticalSpeed { get; set; }
    }
}
