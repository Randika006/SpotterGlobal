
namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{
    public static class RequestUriConstant
    {
        public const string Login = @"https://{0}:{1}/api/auth/login";
        public const string GetSpotterList = @"https://{0}:{1}/api/inputs/spotter";
        public const string GetCameraList = @"https://{0}:{1}/api/outputs/camera";
        public const string GetZonesList = @"https://{0}:{1}/api/zones";
        public const string GetUdcList = @"https://{0}:{1}/api/inputs/udc";
        public const string PushEvent = @"https://{0}:{1}/api/pushEvents";

     }
}
