using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XiaoZhiSharp.Models;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp.Services
{
    /// <summary>
    /// OTA service class
    /// </summary>
    public class OtaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userAgent;
        private readonly string _deviceId;
        private readonly string _clientId;
        private readonly string _acceptLanguage;

        public OtaService(string userAgent, string deviceId, string clientId, string acceptLanguage = "vi-VN")
        {
            _httpClient = new HttpClient();
            _userAgent = userAgent;
            _deviceId = deviceId;
            _clientId = clientId;
            _acceptLanguage = acceptLanguage;

            // Set HTTP client timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Perform OTA check
        /// </summary>
        /// <param name="otaUrl">OTA server address</param>
        /// <param name="request">OTA request data</param>
        /// <returns>OTA response data</returns>
        public async Task<OtaResponse?> CheckOtaAsync(string otaUrl, OtaRequest request)
        {
            try
            {
                LogConsole.InfoLine($"Start OTA check, URL: {otaUrl}");

                // Set request header
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, otaUrl);
                httpRequest.Headers.Add("Device-Id", _deviceId);
                httpRequest.Headers.Add("Client-Id", _clientId);
                httpRequest.Headers.Add("User-Agent", _userAgent);
                httpRequest.Headers.Add("Accept-Language", _acceptLanguage);

                // Serialized request body
                var jsonContent = JsonSerializer.Serialize(request, ApplicationJsonContext.Default.OtaRequest);
                
                LogConsole.InfoLine($"OTA request data: {jsonContent}");
                
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send Request
                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                LogConsole.InfoLine($"OTA response status codes: {response.StatusCode}");
                LogConsole.InfoLine($"OTA response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Successful parsing response
                    var otaResponse = JsonSerializer.Deserialize(responseContent, ApplicationJsonContext.Default.OtaResponse);
                    LogConsole.InfoLine("OTA check successful");
                    return otaResponse;
                }
                else
                {
                    // Parsing error responses
                    try
                    {
                        var errorResponse =JsonSerializer.Deserialize(responseContent, ApplicationJsonContext.Default.OtaErrorResponse);
                        LogConsole.ErrorLine($"OTA check failed: {errorResponse?.Error ?? "Unknown error"}");
                    }
                    catch
                    {
                        LogConsole.ErrorLine($"OTA check failed, HTTP status code: {response.StatusCode}, Response content: {responseContent}");
                    }
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                LogConsole.ErrorLine($"OTA network request exception: {httpEx.Message}");
                return null;
            }
            catch (TaskCanceledException tcEx)
            {
                LogConsole.ErrorLine($"OTA request timed out: {tcEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"OTA check abnormal: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a default OTA request object.
        /// </summary>
        /// <param name="version">Current application version</param>
        /// <param name="elfSha256">ELF file SHA256 hash value</param>
        /// <param name="boardType">Development board type</param>
        /// <param name="boardName">Development board name</param>
        /// <returns>OTA Request Object</returns>
        public OtaRequest CreateDefaultOtaRequest(string version = "1.0.0", string elfSha256 = "", 
            string boardType = "xiaozhi-sharp", string boardName = "xiaozhi-sharp-client")
        {
            var request = new OtaRequest
            {
                Application = new ApplicationInfo
                {
                    Name = "xiaozhi",
                    Version = version,
                    ElfSha256 = !string.IsNullOrEmpty(elfSha256) ? elfSha256 : GenerateDefaultSha256(),
                    CompileTime = DateTime.UtcNow.ToString("MMM dd yyyy HH:mm:ss") + "Z",
                    IdfVersion = "net8.0"
                },
                MacAddress = _deviceId,
                Uuid = _clientId,
                Board = new BoardInfo
                {
                    Type = boardType,
                    Name = boardName,
                    Mac = _deviceId
                },
                Version = 2,
                Language = _acceptLanguage
            };

            return request;
        }

        /// <summary>
        /// Create an OTA request object containing network information
        /// </summary>
        /// <param name="version">Current application version</param>
        /// <param name="elfSha256">ELF file SHA256 hash value</param>
        // <param name="boardType">Development board type</param>
        // <param name="boardName">Development board name</param>
        // <param name="ssid">WiFi network name</param>
        // <param name="rssi">WiFi signal strength</param>
        // <param name="channel">WiFi channel</param>
        // <param name="ip">Device IP address</param>
        // <returns>OTA request object</returns>
        public OtaRequest CreateWifiOtaRequest(
            string version = "1.0.0",
            string elfSha256 = "",
            string boardType = "xiaozhi-sharp-wifi",
            string boardName = "xiaozhi-sharp-wifi-client",
            string ssid = "",
            int rssi = -50,
            int channel = 1,
            string ip = "")
        {
            var request = CreateDefaultOtaRequest(version, elfSha256, boardType, boardName);

            // Add WiFi information
            request.Board.Ssid = ssid;
            request.Board.Rssi = rssi;
            request.Board.Channel = channel;
            request.Board.Ip = ip;

            return request;
        }

        /// <summary>
        /// Generate a default SHA256 hash value (example value).
        /// </summary>
        /// <returns>SHA256 hash string</returns>
        private string GenerateDefaultSha256()
        {
            // This generates a sample SHA256 value; in actual use, it should be the real file hash.
            return "c8a8ecb6d6fbcda682494d9675cd1ead240ecf38bdde75282a42365a0e396033";
        }

        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 