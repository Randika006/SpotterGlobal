using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{
    public class ZoneResultResponse
    {
        [JsonProperty("result")]

        public Dictionary<string, Dictionary<string, Zone>> Result { get; set; }

        //public List<Zone> Data { get; set; }
    }

    public class Zone
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }

}
