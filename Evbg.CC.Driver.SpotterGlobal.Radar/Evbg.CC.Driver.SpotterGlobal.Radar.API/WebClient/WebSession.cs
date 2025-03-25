using Evbg.CC.Driver.SpotterGlobal.Radar.API.Entity;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{

    public class WebSession
    {    
        private string _session;
        private string _userName;
        private string _password;
        private ILog _log = LogManager.GetLogger(typeof(WebSession));
        private string _Ip;
        private int _port;
        private bool _ssEnable;
        private CancellationToken _ct;


        // Loging Request 
        public bool Login(string IP, int Port, string userName, string Password, bool ssEnable)
        {
            bool retFlag = false;
            _userName = userName;
            _password = Password;
            _Ip = IP;
            _port = Port;
            _ssEnable = ssEnable;


            LoginRequest getLoginRequest = new LoginRequest
            {
                username = userName,
                password = Password,
                //clientName = "EBCC",
                //authorizationToken = tkn
            };
            string getLoginRequestBody = JsonConvert.SerializeObject(getLoginRequest);
            var loginURI = string.Format(RequestUriConstant.Login, IP, Port);
            var response = HttpClientWrapper.PerformGetAsync(loginURI, getLoginRequestBody,  _ct).Result;
            if (response == null)
            {
                _log.Error("Response from PerformGetAsync is null");
                return false;
            }

            if (response.statusCode == System.Net.HttpStatusCode.OK)
            {
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(response.responseBuffer);

                _session = loginResponse.SessionToken;
                retFlag = true;
            }
            else
            {
                _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
            }
            
            return retFlag;
        }



        //public static string GetToken(string usernonce, string userkey)
        //{
        //    // Current timestamp in seconds
        //    var timestamp = ((int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds())).ToString();

        //    // Define userNonce and userKey
        //    string userNonce = usernonce;
        //    string userKey = userkey;
        //    string integrationIdentifier = ""; // Leave empty if no integration identifier

        //    // Generate the hash using SHA-256
        //    using (var sha256 = SHA256.Create())
        //    {
        //        var data = Encoding.UTF8.GetBytes(timestamp + userKey);
        //        var hash = sha256.ComputeHash(data);
        //        var encrypted = BitConverter.ToString(hash).Replace("-", "").ToLower();

        //        // Build the token
        //        string authToken;
        //        if (!string.IsNullOrEmpty(integrationIdentifier))
        //        {
        //            authToken = $"{userNonce}:{timestamp}:{encrypted}:{integrationIdentifier}";
        //        }
        //        else
        //        {
        //            authToken = $"{userNonce}:{timestamp}:{encrypted}";
        //        }

        //        return authToken;
        //    }
        //}

        //// Send KeepAlive
        //public bool KeepAlive()
        //{
        //    string keepAlivePayload = $"{{\"token\" :\"{_session}\"}}";

        //    var keepAliveURI = string.Format(RequestUriConstant.KeepAlive, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPostAsync(keepAliveURI, keepAlivePayload, _session,_ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //        return false;
        //    }
        //}

        //// Get Camera
        public List<CameraData> GetCameraList()
        {
            List<CameraData> deviceList = new List<CameraData>();

            var getDeviceURI = string.Format(RequestUriConstant.GetCameraList, _Ip, _port, _session);
            var response = HttpClientWrapper.PerformGetAsync(getDeviceURI, _session, _ct).Result;
        
            if (response.statusCode == System.Net.HttpStatusCode.OK)
            {
                var deviceResponse = JsonConvert.DeserializeObject<CameraDataResponse>(response.responseBuffer);
                //deviceList = deviceResponse.data.devices;
                //foreach (var device in deviceResponse.Result.cameras)
                //{
                //    deviceList.Add(device);
                //}

                if (deviceResponse?.Result != null)
                {
                    foreach (var device in deviceResponse.Result.Values) // Corrected: Iterating over dictionary values
                    {
                        deviceList.Add(device);
                    }
                }
            }
            else
            {
                _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
            }

            return deviceList;
        }

        //Get Radar
        public List<RadarData> GetRadarList()
        {
            List<RadarData> deviceList = new List<RadarData>();

            var getDeviceURI = string.Format(RequestUriConstant.GetSpotterList, _Ip, _port, _session);
            var response = HttpClientWrapper.PerformGetAsync(getDeviceURI, _session, _ct).Result;

            if (response.statusCode == System.Net.HttpStatusCode.OK)
            {
                var deviceResponse = JsonConvert.DeserializeObject<RadarDataResponse>(response.responseBuffer);
                //deviceList = deviceResponse.data.devices;

                if (deviceResponse?.Result != null)
                {
                    foreach (var device in deviceResponse.Result.Values) // Corrected: Iterating over dictionary values
                    {
                        deviceList.Add(device);
                    }
                }
            }
            else
            {
                _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
            }

            return deviceList;
        }


        //Get Zone
        public List<Zone> GetZoneList()
        {
            List<Zone> deviceList = new List<Zone>();

            var getDeviceURI = string.Format(RequestUriConstant.GetZonesList, _Ip, _port, _session);
            var response = HttpClientWrapper.PerformGetAsync(getDeviceURI, _session, _ct).Result;

            if (response.statusCode == System.Net.HttpStatusCode.OK)
            {
                var deviceResponse = JsonConvert.DeserializeObject<ZoneResultResponse>(response.responseBuffer);
                //deviceList = deviceResponse.data.devices;

                if (deviceResponse?.Result != null) 
                {
                    foreach (var kvp in deviceResponse.Result) 
                    {
                        // deviceList.Add(device);
                        foreach (var result in kvp.Value.Values) // Extracting actual Zone objects
                        {
                            deviceList.Add(result);
                        }
                    }
                }
            }
            else
            {
                _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
            }

            return deviceList;
        }

        //Get UDC
        public List<UDCDevice> GetUDCList()
        {
            List<UDCDevice> deviceList = new List<UDCDevice>();

            var getDeviceURI = string.Format(RequestUriConstant.GetUdcList, _Ip, _port, _session);
            var response = HttpClientWrapper.PerformGetAsync(getDeviceURI, _session, _ct).Result;

            if (response.statusCode == System.Net.HttpStatusCode.OK)
            {
                var deviceResponse = JsonConvert.DeserializeObject<UDCResultResponse>(response.responseBuffer);
                //deviceList = deviceResponse.data.devices;

                if (deviceResponse?.Result != null) 
                {
                    //foreach (var device in deviceResponse.Result) 
                    //{
                    //    deviceList.Add(device);
                    //}
                    deviceList = deviceResponse.Result.Values.ToList();

                }
            }
            else
            {
                _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
            }

            return deviceList;
        }

        //Get Event
        public List<EventResult> GetPushEventList()
        {
            List<EventResult> eventList = new List<EventResult>();

            var getDeviceURI = string.Format(RequestUriConstant.PushEvent, _Ip, _port, _session);
            var response = HttpClientWrapper.PerformGetAsync(getDeviceURI, _session, _ct).Result;
            if (response.statusCode == System.Net.HttpStatusCode.OK)
            {
                var eventResponse = JsonConvert.DeserializeObject<PushEventResponse>(response.responseBuffer);

                if (eventResponse?.Result != null)
                {
                    eventList = eventResponse.Result;

                }

                //if (eventResponse?.Result != null)
                //{
                //    eventList = eventResponse.result.Values.ToList();
                //}
            }
            else
            {
                _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
            }

            return eventList;
        }


        //// Get Live Video
        //public string GetLiveVideo(string cameraID)
        //{            
        //    var realTimeVideoURI = string.Format(RequestUriConstant.RealTimeVideo, _Ip, _port, _session, cameraID);
        //    var response = HttpClientWrapper.PerformGetAsync(realTimeVideoURI, _session,_ct).Result;

        //    if ((response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //        || (response.statusCode == System.Net.HttpStatusCode.Forbidden))
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformGetAsync(realTimeVideoURI, _session,_ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(MPD));
        //        using (StringReader reader = new StringReader(response.responseBuffer))
        //        {
        //            MPD deserializedOperator = (MPD)serializer.Deserialize(reader);

        //            string cleanedUrl = deserializedOperator.Period.AdaptationSet.Representation.BaseURL.Replace("amp;", "");

        //            string urlStr = $"https://{_Ip}:{_port}{cleanedUrl}";

        //            return urlStr;
        //        }
        //    }
        //    else
        //    {
        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //        return null;
        //    }


        //}

        //// Get Playback Video URL 
        //public string GetPlaybackVideo(string cameraID, DateTime startTime, string mode)
        //{
        //    var playbackVideoURI = string.Format(RequestUriConstant.GetPlaybackVideo, _Ip, _port, _session, cameraID, startTime.ToString("yyyyMMdd'T'HHmmss.fff'Z'"), mode);
        //    var response = HttpClientWrapper.PerformGetAsync(playbackVideoURI, _session,_ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformGetAsync(playbackVideoURI, _session,_ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(MPD));
        //        using (StringReader reader = new StringReader(response.responseBuffer))
        //        {
        //            MPD deserializedOperator = (MPD)serializer.Deserialize(reader);

        //            string cleanedUrl = deserializedOperator.Period.AdaptationSet.Representation.BaseURL.Replace("amp;", "");

        //            string urlStr = $"https://{_Ip}:{_port}{cleanedUrl}";

        //            return urlStr;
        //        }
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //        return null;
        //    }

        //}

        //public List<TimelineRecord> GetPlaybackTimeline(string cameraID, DateTime startTime, DateTime endTime)
        //{
        //    var scope = "100_SECONDS"; //minimum length
        //    var playbackTimelineUri = string.Format(RequestUriConstant.GetPlaybackTimeline, _Ip, _port, _session, cameraID,
        //        scope, startTime.ToString("yyyy-MM-ddTHH:mm:ss.000Z"), endTime.ToString("yyyy-MM-ddTHH:mm:ss.000Z"));
        //    var response = HttpClientWrapper.PerformGetAsync(playbackTimelineUri, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)               
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformGetAsync(playbackTimelineUri, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var timeLineResponse = JsonConvert.DeserializeObject<TimeLineResponse>(response.responseBuffer);
        //        if (timeLineResponse.result.timelines?.Count > 0)
        //        {
        //            return timeLineResponse.result.timelines[0].record;
        //        }
        //        else
        //            return null;
        //    }
        //    else
        //    {
        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //        return null;
        //    }


        //}

        //// PTZ
        //public PTZResponse PTZDirection(string cameraID, int pan, int tilt, int zoom)
        //{
        //    PTZResponse PTZDirResponse = new PTZResponse();
        //    if (Math.Abs(pan) > 10)
        //    {
        //        pan = (int)(pan / Math.Abs(pan));
        //        pan = -1 * pan;
        //    }
        //    else
        //        pan = 0;

        //    if (Math.Abs(tilt) > 10)
        //    {
        //        tilt = (int)(tilt / Math.Abs(tilt));
        //        tilt = -1 * tilt;
        //    }
        //    else
        //        tilt = 0;
        //    if (Math.Abs(zoom) > 10)
        //        zoom = (int)(zoom / Math.Abs(zoom));
        //    else
        //        zoom = 0;

        //    PTZDirectionControlRequest ptzdirRequest = new PTZDirectionControlRequest
        //    {
        //        session = _session,
        //        id = cameraID,               
        //        continuous = new Continuous
        //        {
        //            panAmount = pan,
        //            tiltAmount = tilt,
        //            zoomAmount = zoom,
        //            action = "START"
        //        }
        //    };

        //    string ptzDirRequestBody = JsonConvert.SerializeObject(ptzdirRequest);
        //    var ptzDirRequesURI = string.Format(RequestUriConstant.PTZDirection, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPutAsync(ptzDirRequesURI, ptzDirRequestBody, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformPutAsync(ptzDirRequesURI, ptzDirRequestBody, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var responseBuffer = Regex.Unescape(response.responseBuffer);
        //        PTZDirResponse = JsonConvert.DeserializeObject<PTZResponse>(responseBuffer);
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }

        //    return PTZDirResponse;
        //}


        // PTZ
        //public PTZResponse PTZLense(string channelID, int direction, int command, int operation)
        //{
        //    PTZResponse PTZDirResponse = new PTZResponse();
        //    PTZLenseControlRequest ptzdirRequest = new PTZLenseControlRequest
        //    {
        //        clientType = "WINPC_V2",
        //        clientMac = "",
        //        clientPushId = "",
        //        method = "DMS.Ptz.OperateDirect",
        //        data = new PTZLenseData
        //        {
        //            extend = "",
        //            direct = direction.ToString(), //Direction: 1-increase; 2-decrease
        //            command = command.ToString(), //Command: 0-stop; 1-start
        //            channelId = channelID,
        //            step = "1",
        //            operateType = operation.ToString() //Operation type: 1-zoom; 2-focus; 3-iris
        //        }
        //    };

        //    string ptzDirRequestBody = JsonConvert.SerializeObject(ptzdirRequest);
        //    var ptzDirRequesURI = string.Format(RequestUriConstant.PTZLense, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPost(ptzDirRequesURI, ptzDirRequestBody, token, out var responseBuffer);

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformPost(ptzDirRequesURI, ptzDirRequestBody, token, out responseBuffer);
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        responseBuffer = Regex.Unescape(responseBuffer);
        //        PTZDirResponse = JsonConvert.DeserializeObject<PTZResponse>(responseBuffer);
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }

        //    return PTZDirResponse;
        //}

        // Get Presets
        //public GetPresetsResponse GetPresets(string cameraID)
        //{
        //    GetPresetsResponse GetPresetResponse = new GetPresetsResponse();

        //    var getpresetRequesURI = string.Format(RequestUriConstant.GetPresets, _Ip, _port, _session, cameraID);
        //    var response = HttpClientWrapper.PerformGetAsync(getpresetRequesURI, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformGetAsync(getpresetRequesURI, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var responseBuffer = Regex.Unescape(response.responseBuffer);
        //        GetPresetResponse = JsonConvert.DeserializeObject<GetPresetsResponse>(responseBuffer);
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }

        //    return GetPresetResponse;
        //}



        //// Set Preset
        //public PTZResponse SetPreset(string cameraID, int PresetId, string PresetName)
        //{
        //    PTZResponse PresetResponse = new PTZResponse();
        //    PresetControlRequest presetRequest = new PresetControlRequest
        //    {
        //        session = _session,
        //        id = cameraID,
        //        presetId = PresetId,
        //        name = PresetName,
        //        isHome = false
        //    };

        //    string presetRequestBody = JsonConvert.SerializeObject(presetRequest);
        //    var presetRequesURI = string.Format(RequestUriConstant.SetPresets, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPutAsync(presetRequesURI, presetRequestBody, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformPutAsync(presetRequesURI, presetRequestBody, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var responseBuffer = Regex.Unescape(response.responseBuffer);
        //        PresetResponse = JsonConvert.DeserializeObject<PTZResponse>(responseBuffer);
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }

        //    return PresetResponse;
        //}

        //// Move To Preset
        //public PTZResponse MoveToPreset(string cameralID, int PresetId)
        //{
        //    PTZResponse PresetResponse = new PTZResponse();
        //    PresetControlReq presetRequest = new PresetControlReq
        //    {
        //        session = _session,
        //        id = cameralID,
        //        presetId = PresetId
        //    };

        //    string presetRequestBody = JsonConvert.SerializeObject(presetRequest);
        //    var presetRequesURI = string.Format(RequestUriConstant.GoToPresets, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPutAsync(presetRequesURI, presetRequestBody, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformPostAsync(presetRequesURI, presetRequestBody, _session,_ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var responseBuffer = Regex.Unescape(response.responseBuffer);
        //        PresetResponse = JsonConvert.DeserializeObject<PTZResponse>(responseBuffer);
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }

        //    return PresetResponse;
        //}



        //// Delete Preset
        //public PTZResponse DeletePreset(string cameraID, int PresetId)
        //{
        //    PTZResponse PresetResponse = new PTZResponse();
        //    PresetControlReq presetRequest = new PresetControlReq
        //    {
        //        session = _session,
        //        id = cameraID,
        //        presetId = PresetId
        //    };

        //    string presetRequestBody = JsonConvert.SerializeObject(presetRequest);
        //    var presetRequesURI = string.Format(RequestUriConstant.DeletePresets, _Ip, _port);
        //    var response = HttpClientWrapper.PerformDeleteAsync(presetRequesURI, presetRequestBody, _session,_ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return null;
        //        response = HttpClientWrapper.PerformDeleteAsync(presetRequesURI, presetRequestBody, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var responseBuffer = Regex.Unescape(response.responseBuffer);
        //        PresetResponse = JsonConvert.DeserializeObject<PTZResponse>(responseBuffer);
        //    }
        //    else
        //    {

        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }

        //    return PresetResponse;
        //}

        //public bool SubscribeToEvents(string callbackUrl, string signature, out string subId)
        //{
        //    subId = string.Empty;
        //    WebhookSubscriptionResponse subscribtionResponse = new WebhookSubscriptionResponse();
        //    WebhookSubscriptionRequest subscribtionRequest = new WebhookSubscriptionRequest
        //    {
        //        session = _session,
        //        webhook = new Webhook()
        //        {
        //            authenticationToken = signature,
        //            url = callbackUrl,
        //            heartbeat = new Heartbeat()
        //            {
        //                enable = true
        //            },
        //            eventTopics = new EventTopics()
        //            {
        //                whitelist = new List<string>{ "ALL" }
        //            }
        //        }
        //    };

        //    string requestBody = JsonConvert.SerializeObject(subscribtionRequest);
        //    var subURI = string.Format(RequestUriConstant.WebHookSubscription, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPostAsync(subURI, requestBody, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return false;
        //        response = HttpClientWrapper.PerformPostAsync(subURI, requestBody, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        var responseBuffer = Regex.Unescape(response.responseBuffer);
        //        subscribtionResponse = JsonConvert.DeserializeObject<WebhookSubscriptionResponse>(responseBuffer);
        //    }
        //    else
        //    {
        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }
        //    return (subscribtionResponse.status == "success");
        //}

        //public bool UnSubscribeToEvents(string subId)
        //{
        //    WebhookUnSubscriptionRequest unSubscribtionRequest = new WebhookUnSubscriptionRequest
        //    {
        //        session = _session,
        //        ids = new List<string> { subId }
        //    };

        //    string requestBody = JsonConvert.SerializeObject(unSubscribtionRequest);
        //    var subURI = string.Format(RequestUriConstant.WebHookSubscription, _Ip, _port);
        //    var response = HttpClientWrapper.PerformDeleteAsync(subURI, requestBody, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.Unauthorized)
        //    {
        //        if (Login(_Ip, _port, _userName, _password, _UserNonce, _UserKey,_ct) == false)
        //            return false;
        //        response = HttpClientWrapper.PerformDeleteAsync(subURI, requestBody, _session, _ct).Result;
        //    }

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //    }
        //    return false;
        //}

        //// Send KeepAlive
        //public bool SendKeepAlive()
        //{
        //    string keepAlivePayload = $"{{\"token\" :\"{_session}\"}}";

        //    var keepAliveURI = string.Format(RequestUriConstant.KeepAlive, _Ip, _port);
        //    var response = HttpClientWrapper.PerformPostAsync(keepAliveURI, keepAlivePayload, _session, _ct).Result;

        //    if (response.statusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        _log.Error($"WebSession error {response.statusCode} : {response.ErrorMessage}");
        //        return false;
        //    }
        //}

        public static long getLongTime(DateTime dateTime)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 
            return (long)(dateTime - startTime).TotalMilliseconds; // 
        }

        public static DateTime getDateTime(long time)
        {
            DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 
            return startDateTime.AddMilliseconds(time);

        }
    }
}
