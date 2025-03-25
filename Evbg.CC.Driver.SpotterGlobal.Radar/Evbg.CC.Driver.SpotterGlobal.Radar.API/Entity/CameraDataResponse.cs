using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{
    public class CameraDataResponse
    {
       

        [JsonProperty("result")]
        public Dictionary<string, CameraData> Result { get; set; }

     
    }

    public class CameraData
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

        [JsonProperty("selectedVideoFeed")]
        public string SelectedVideoFeed { get; set; }

        [JsonProperty("override")]
        public bool Override { get; set; }

        [JsonProperty("capabilities")]
        public string Capabilities { get; set; }

        [JsonProperty("idleTimeout")]
        public int IdleTimeout { get; set; }

        [JsonProperty("maxImages")]
        public int MaxImages { get; set; }

        [JsonProperty("imageInterval")]
        public int ImageInterval { get; set; }

        [JsonProperty("cueLeadTime")]
        public int CueLeadTime { get; set; }

        [JsonProperty("FOVZoomAngle")]
        public int FOVZoomAngle { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("idlePreset")]
        public string IdlePreset { get; set; }

        [JsonProperty("features")]
        public string Features { get; set; }

        [JsonProperty("range")]
        public string Range { get; set; }

        [JsonProperty("angleRange")]
        public string AngleRange { get; set; }

        [JsonProperty("ptz")]
        public string Ptz { get; set; }

        [JsonProperty("driver")]
        public string Driver { get; set; }

        [JsonProperty("VideoFeeds")]
        public string VideoFeeds { get; set; }

        [JsonProperty("presets")]
        public string Presets { get; set; }

        [JsonProperty("actions")]
        public string Actions { get; set; }

        [JsonProperty("priorityZones")]
        public string PriorityZones { get; set; }

        [JsonProperty("geolocation")]
        public Geolocation Geolocation { get; set; }
    }
    public class Geolocation
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
