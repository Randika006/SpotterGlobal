using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{
    public class RadarDataResponse
    {
        [JsonProperty("result")]
        public Dictionary<string, RadarData> Result { get; set; }
    }

    public class RadarData
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
        public string Port { get; set; }

        [JsonProperty("capabilities")]
        public string Capabilities { get; set; }

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

        [JsonProperty("ntpServer")]
        public string NtpServer { get; set; }

        [JsonProperty("geolocation")]
        public RadarGeolocation Geolocation { get; set; }

        [JsonProperty("rotation")]
        public string Rotation { get; set; }

        [JsonProperty("sensor")]
        public string Sensor { get; set; }

        [JsonProperty("gps")]
        public string Gps { get; set; }

        [JsonProperty("lineOfSight")]
        public string LineOfSight { get; set; }

        [JsonProperty("fieldOfView")]
        public string FieldOfView { get; set; }

        [JsonProperty("presets")]
        public string Presets { get; set; }
    }
    

    public class RadarGeolocation
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
