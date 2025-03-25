using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using log4net;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.API
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;

    public static class HttpClientWrapper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HttpClientWrapper));

        public static bool IsVerboseLogging { get; set; } = false;

        private static bool EasyCertCheck(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            // Easy Cert Check (ignore SSL issues) - typically used for development
            return true;
        }

        //private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        //{
        //    // Disable certificate validation (should be removed for production)
        //    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        //})
        //{
        //    Timeout = TimeSpan.FromSeconds(30) // Set a reasonable timeout
        //};

        private static HttpClient GetHttpClient()
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler
            {
                // Disable certificate validation (should be removed for production)
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            })
            {
                Timeout = TimeSpan.FromSeconds(30) // Set a reasonable timeout
            };
            //configure default headers here
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        static HttpClientWrapper()
        {
            // Configure default headers here, such as:
            // _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Perform GET request
        public static async Task<HTTPResponse> PerformGetAsync(string apiRequest, string token, CancellationToken ct)
        {
            var httpResponse = new HTTPResponse();
            try
            {
                if (IsVerboseLogging)
                    _log.Debug($"Issuing GET request - {apiRequest}");

                var request = new HttpRequestMessage(HttpMethod.Get, apiRequest);
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("x-avg-session", token);

                HttpResponseMessage response = await GetHttpClient().SendAsync(request, ct).ConfigureAwait(false); 

                if (response.IsSuccessStatusCode)
                {
                    httpResponse.responseBuffer = await response.Content.ReadAsStringAsync();
                    if (IsVerboseLogging)
                        _log.Debug($"Response for GET request - {httpResponse.responseBuffer}");
                }
                else
                {
                    httpResponse.statusCode = response.StatusCode;
                    httpResponse.ErrorMessage = await response.Content.ReadAsStringAsync();
                }

                return httpResponse;
            }
            catch (Exception ex)
            {
                _log.Error($"Error during GET request: {ex.Message}", ex);
                throw new Exception($"Request failed: {ex.Message}", ex);
            }
        }

        // Perform POST request
        public static async Task<HTTPResponse> PerformPostAsync(string apiRequest, string bodyText, string token, CancellationToken ct)
        {
            var httpResponse = new HTTPResponse();

            try
            {
                if (IsVerboseLogging)
                {
                    _log.Debug($"Issuing POST request - {apiRequest}");
                    _log.Debug($"POST request body - {bodyText}");
                }

                var request = new HttpRequestMessage(HttpMethod.Post, apiRequest)
                {
                    Content = new StringContent(bodyText, Encoding.UTF8, "application/json")
                };

                //if (!string.IsNullOrEmpty(token))
                //    request.Headers.Add("x-avg-session", token);

                HttpResponseMessage response = await GetHttpClient().SendAsync(request, ct).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    httpResponse.responseBuffer = await response.Content.ReadAsStringAsync();
                    if (IsVerboseLogging)
                        _log.Debug($"Response for POST request - {httpResponse.responseBuffer}");
                }
                else
                {
                    httpResponse.statusCode = response.StatusCode;
                    httpResponse.ErrorMessage = await response.Content.ReadAsStringAsync();
                }

                return httpResponse;
            }
            catch (Exception ex)
            {
                _log.Error($"Error during POST request: {ex.Message}", ex);
                throw new Exception($"Request failed: {ex.Message}", ex);
            }
        }

        // Perform PUT request
        public static async Task<HTTPResponse> PerformPutAsync(string apiRequest, string bodyText, string token, CancellationToken ct)
        {
            var httpResponse = new HTTPResponse();

            try
            {
                if (IsVerboseLogging)
                {
                    _log.Debug($"Issuing PUT request - {apiRequest}");
                    _log.Debug($"PUT request body - {bodyText}");
                }

                var request = new HttpRequestMessage(HttpMethod.Put, apiRequest)
                {
                    Content = new StringContent(bodyText, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("x-avg-session", token);

                HttpResponseMessage response = await GetHttpClient().SendAsync(request, ct).ConfigureAwait(false); ;

                if (response.IsSuccessStatusCode)
                {
                    httpResponse.responseBuffer = await response.Content.ReadAsStringAsync();
                    if (IsVerboseLogging)
                        _log.Debug($"Response for PUT request - {httpResponse.responseBuffer}");
                }
                else
                {
                    httpResponse.statusCode = response.StatusCode;
                    httpResponse.ErrorMessage = await response.Content.ReadAsStringAsync();
                }

                return httpResponse;
            }
            catch (Exception ex)
            {
                _log.Error($"Error during PUT request: {ex.Message}", ex);
                throw new Exception($"Request failed: {ex.Message}", ex);
            }
        }

        // Perform DELETE request
        public static async Task<HTTPResponse> PerformDeleteAsync(string apiRequest, string bodyText, string token, CancellationToken ct)
        {
            var httpResponse = new HTTPResponse();

            try
            {
                if (IsVerboseLogging)
                {
                    _log.Debug($"Issuing DELETE request - {apiRequest}");
                    _log.Debug($"DELETE request body - {bodyText}");
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, apiRequest)
                {
                    Content = new StringContent(bodyText, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("x-avg-session", token);

                HttpResponseMessage response = await GetHttpClient().SendAsync(request, ct).ConfigureAwait(false); ;

                if (response.IsSuccessStatusCode)
                {
                    httpResponse.responseBuffer = await response.Content.ReadAsStringAsync();
                    if (IsVerboseLogging)
                        _log.Debug($"Response for DELETE request - {httpResponse.responseBuffer}");
                }
                else
                {
                    httpResponse.statusCode = response.StatusCode;
                    httpResponse.ErrorMessage = await response.Content.ReadAsStringAsync();
                }

                return httpResponse;
            }
            catch (Exception ex)
            {
                _log.Error($"Error during DELETE request: {ex.Message}", ex);
                throw new Exception($"Request failed: {ex.Message}", ex);
            }
        }

        // Perform POST request for byte data (use UploadData equivalent)
        public static async Task<HTTPResponse> PerformPostGetBytesAsync(string apiRequest, string bodyText, string token, CancellationToken ct)
        {
            var httpResponse = new HTTPResponse();
            try
            {
                if (IsVerboseLogging)
                    _log.Debug($"Issuing POST request for binary data - {apiRequest}");

                var request = new HttpRequestMessage(HttpMethod.Post, apiRequest)
                {
                    Content = new StringContent(bodyText, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(token))
                    request.Headers.Add("x-avg-session", token);

                HttpResponseMessage response = await GetHttpClient().SendAsync(request, ct).ConfigureAwait(false); ;

                if (response.IsSuccessStatusCode)
                {
                    httpResponse.responseBytes = await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    httpResponse.statusCode = response.StatusCode;
                    httpResponse.ErrorMessage = await response.Content.ReadAsStringAsync();
                }

                return httpResponse;
            }
            catch (Exception ex)
            {
                _log.Error($"Error during POST request for binary data: {ex.Message}", ex);
                throw new Exception($"Request failed: {ex.Message}", ex);
            }
        }

        // HTTPResponse class to capture payload,status code and error message
        public class HTTPResponse
        {
            public byte[] responseBytes = null;
            public string responseBuffer = string.Empty;
            public HttpStatusCode statusCode = HttpStatusCode.OK;
            public string ErrorMessage;
        }
    }
}
