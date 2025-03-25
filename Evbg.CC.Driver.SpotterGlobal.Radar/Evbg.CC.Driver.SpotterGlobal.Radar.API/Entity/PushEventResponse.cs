using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity
{
    public class PushEventResponse
    {
        [JsonProperty("errors")]
        public List<string> Errors { get; set; }

        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("serial")]
        public string Serial { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("userSession")]
        public string UserSession { get; set; }

        [JsonProperty("result")]
        public List<EventResult> Result { get; set; }

        //[JsonProperty("result")]
        //public Dictionary<string, EventResult> result { get; set; }

    }

    public class EventResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("zones")]
        public List<string> Zones { get; set; }

        [JsonProperty("geolocation")]
        public EventGeolocation Geolocation { get; set; }

        [JsonProperty("observation")]
        public EventObservation Observation { get; set; }

        [JsonProperty("stats")]
        public EventStats Stats { get; set; }

        [JsonProperty("classification")]
        public EventClassification Classification { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("totalDistance")]
        public double TotalDistance { get; set; }

        [JsonProperty("cameras")]
        public List<string> Cameras { get; set; }

        [JsonProperty("approach")]
        public Approach Approach { get; set; }
    }

    public class EventGeolocation
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("altitude")]
        public double Altitude { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("bearing")]
        public double Bearing { get; set; }

        [JsonProperty("heading")]
        public double Heading { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("verticalSpeed")]
        public double VerticalSpeed { get; set; }
    }

    public class EventObservation
    {
        [JsonProperty("range")]
        public double Range { get; set; }

        [JsonProperty("radialVelocity")]
        public double RadialVelocity { get; set; }

        [JsonProperty("horizontalAngle")]
        public double HorizontalAngle { get; set; }

        [JsonProperty("azimuthAngle")]
        public double AzimuthAngle { get; set; }

        [JsonProperty("verticalAngle")]
        public double VerticalAngle { get; set; }

        [JsonProperty("altitudeAngle")]
        public double AltitudeAngle { get; set; }
    }

    public class EventStats
    {
        [JsonProperty("amplitude")]
        public double Amplitude { get; set; }

        [JsonProperty("rcs")]
        public double Rcs { get; set; }

        [JsonProperty("rcsInstant")]
        public double RcsInstant { get; set; }

        [JsonProperty("localSnr")]
        public double LocalSnr { get; set; }

        [JsonProperty("rangeMean")]
        public double RangeMean { get; set; }

        [JsonProperty("rangeVariance")]
        public double RangeVariance { get; set; }

        [JsonProperty("dopplerMean")]
        public double DopplerMean { get; set; }

        [JsonProperty("dopplerVariance")]
        public double DopplerVariance { get; set; }

        [JsonProperty("energyMean")]
        public double EnergyMean { get; set; }

        [JsonProperty("energyVariance")]
        public double EnergyVariance { get; set; }

        [JsonProperty("verticalAngleVariance")]
        public double VerticalAngleVariance { get; set; }

        [JsonProperty("horizontalAngleVariance")]
        public double HorizontalAngleVariance { get; set; }
    }

    public class EventClassification
    {
        [JsonProperty("type")]
        public Type Type { get; set; }

        [JsonProperty("intent")]
        public Intent Intent { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; }
    }

    public class Type
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }

    public class Intent
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }

    public class Approach
    {
        [JsonProperty("approachRate")]
        public double ApproachRate { get; set; }

        [JsonProperty("changeInAltitude")]
        public double ChangeInAltitude { get; set; }
    }

}
