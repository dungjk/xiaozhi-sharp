using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using XiaoZhiSharp.Models;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Services;
using XiaoZhiSharp.Services.Chat;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp
{
    public class XiaoZhiAgent : IDisposable
    {
        private string _otaUrl { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
        private string _wsUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        private string _token { get; set; } = "test-token";
        private string _deviceId { get; set; } = SystemInfo.GetMacAddress();
        private string _clientId { get; set; } = SystemInfo.GenerateClientId();
        private string _userAgent { get; set; } = SystemInfo.GetUserAgent();
        private string _currentVersion { get; set; } = SystemInfo.GetApplicationVersion();
        private Services.Chat.ChatService? _chatService = null;
        private Services.IAudioService? _audioService = null;
        private Services.AudioOpusService _audioOpusService = new Services.AudioOpusService();
        private Services.OtaService? _otaService = null;
        private OtaResponse? _latestOtaResponse = null;

        // Add a variable to track the current task.
        private Task? _monitoringTask = null;
        private bool _disposed = false;
        private CancellationTokenSource? _recordingCts = null;

        #region property
        public string WsUrl
        {
            get { return _wsUrl; }
            set { _wsUrl = value; }
        }
        public string OtaUrl
        {
            get { return _otaUrl; }
            set { _otaUrl = value; }
        }
        public Services.IAudioService? AudioService
        {
            get { return _audioService; }
            set { _audioService = value; }
        }
        public string DeviceId
        {
            get { return _deviceId; }
            set { _deviceId = value; }
        }
        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }
        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }
        public string CurrentVersion
        {
            get { return _currentVersion; }
            set { _currentVersion = value; }
        }
        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }
        public OtaResponse? LatestOtaResponse
        {
            get { return _latestOtaResponse; }
        }
        public bool IsPlaying
        {
            get { return _audioService != null && _audioService.IsPlaying; }
        }
        public bool IsRecording
        {
            get { return _audioService != null && _audioService.IsRecording; }
        }
        public WebSocketState ConnectState
        {
            get { return _chatService != null ? _chatService.ConnectState : WebSocketState.None; }
        }
        #endregion

        #region event
        public delegate Task MessageEventHandler(string type, string message);
        public event MessageEventHandler? OnMessageEvent = null;

        public delegate Task AudioEventHandler(byte[] opus);
        public event AudioEventHandler? OnAudioEvent = null;

        public delegate Task AudioPcmEventHandler(byte[] pcm);
        public event AudioPcmEventHandler? OnAudioPcmEvent = null;

        public delegate Task OtaEventHandler(OtaResponse? otaResponse);
        public event OtaEventHandler? OnOtaEvent = null;
        #endregion

        #region Constructor
        public XiaoZhiAgent() { }
        #endregion

        public async Task Start()
        {
            // 1. First, perform an OTA check.
            await CheckOtaUpdate();

            // 2. WebSocket connection parameters are determined based on the OTA response.
            string wsUrl = _latestOtaResponse?.WebSocket?.Url ?? _wsUrl;
            string token = _latestOtaResponse?.WebSocket?.Token ?? _token;

            LogConsole.InfoLine($"Using WebSocket URL: {wsUrl}");
            LogConsole.InfoLine($"Use Token: {token}");

            // 3. Initiate WebSocket connection
            _chatService = new Services.Chat.ChatService(wsUrl, token, _deviceId);
            _chatService.OnMessageEvent += ChatService_OnMessageEvent;
            if (Global.IsAudio)
                _chatService.OnAudioEvent += ChatService_OnAudioEvent;
            _chatService.Start();

            // 4. Initialize audio service
            if (Global.IsAudio)
            {
                if (_audioService == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        LogConsole.InfoLine("The current operating system is Windows.");
                        _audioService = new AudioWaveService();
                    }
                    else
                    {
                        _audioService = new AudioPortService();
                    }

                }
                if (_audioService != null)
                    _audioService.OnPcmAudioEvent += AudioService_OnPcmAudioEvent;
            }
        }
        public async Task Restart()
        {
            // Save the current audio service reference so that event handling can be reset.
            var currentAudioService = _audioService;

            // If an audio service exists, remove the event handler first.
            if (currentAudioService != null)
            {
                // Remove event handlers to avoid duplicate subscriptions
                currentAudioService.OnPcmAudioEvent -= AudioService_OnPcmAudioEvent;
            }

            // Release existing resources
            _chatService?.Dispose();
            _chatService = null;

            _otaService?.Dispose();
            _otaService = null;

            // Reset Recording Cancellation Token
            _recordingCts?.Cancel();
            _recordingCts?.Dispose();
            _recordingCts = null;

            // Restart the service
            await Start();

            // If the audio service instance has not changed, ensure the recording status is correct.
            if (currentAudioService != null && _audioService == currentAudioService)
            {
                // Ensure the recording status is correct.
                if (_audioService.IsRecording)
                {
                    _audioService.StopRecording();
                }
            }
            
            LogConsole.InfoLine("Reboot complete");
        }

        /// <summary>
        /// Perform OTA checks
        /// </summary>
        /// <returns></returns>
        public async Task<OtaResponse?> CheckOtaUpdate()
        {
            try
            {
                LogConsole.InfoLine("Start OTA check...");

                // Initialize OTA service
                _otaService ??= new Services.OtaService(_userAgent, _deviceId, _clientId);

                // Create OTA request
                var otaRequest = _otaService.CreateDefaultOtaRequest(_currentVersion, "", 
                    "xiaozhi-sharp", "xiaozhi-sharp-client");

                // Send OTA request
                _latestOtaResponse = await _otaService.CheckOtaAsync(_otaUrl, otaRequest);

                // Trigger OTA event
                if (OnOtaEvent != null)
                    await OnOtaEvent(_latestOtaResponse);

                if (_latestOtaResponse != null)
                {
                    LogConsole.InfoLine("OTA check complete, server configuration information obtained.");

                    // Display activation information
                    if (_latestOtaResponse.Activation != null)
                    {
                        LogConsole.InfoLine($"Activation code: {_latestOtaResponse.Activation.Code}");
                        LogConsole.InfoLine($"Activation message: {_latestOtaResponse.Activation.Message}");
                    }

                    // Display firmware information
                    if (_latestOtaResponse.Firmware != null)
                    {
                        LogConsole.InfoLine($"Firmware version: {_latestOtaResponse.Firmware.Version}");
                        if (!string.IsNullOrEmpty(_latestOtaResponse.Firmware.Url))
                        {
                            LogConsole.InfoLine($"Firmware download address: {_latestOtaResponse.Firmware.Url}");
                        }
                    }

                    // Display server time information
                    if (_latestOtaResponse.ServerTime != null)
                    {
                        LogConsole.InfoLine($"Server time: {DateTimeOffset.FromUnixTimeMilliseconds(_latestOtaResponse.ServerTime.Timestamp)}");
                        LogConsole.InfoLine($"Time zone: {_latestOtaResponse.ServerTime.Timezone}");
                    }

                    // Display MQTT configuration information
                    if (_latestOtaResponse.Mqtt != null)
                    {
                        LogConsole.InfoLine($"MQTT server: {_latestOtaResponse.Mqtt.Endpoint}");
                        LogConsole.InfoLine($"MQTT Client ID: {_latestOtaResponse.Mqtt.ClientId}");
                    }

                    // Display WebSocket configuration information
                    if (_latestOtaResponse.WebSocket != null)
                    {
                        LogConsole.InfoLine($"WebSocket server: {_latestOtaResponse.WebSocket.Url}");
                    }
                }
                else
                {
                    LogConsole.InfoLine("OTA check complete, use default configuration.");
                }

                return _latestOtaResponse;
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"OTA check abnormal: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create an OTA request containing WiFi information
        /// </summary>
        /// <param name="ssid">WiFi network name</param>
        /// <param name="rssi">WiFi signal strength</param>
        /// <param name="channel">WiFi channel</param>
        /// <param name="ip">Device IP address</param>
        /// <returns></returns>
        public async Task<OtaResponse?> CheckOtaUpdateWithWifi(string ssid, int rssi = -50, int channel = 1, string ip = "")
        {
            try
            {
                LogConsole.InfoLine($"Start OTA check (WiFi): {ssid}）...");

                // Initialize OTA service
                _otaService ??= new Services.OtaService(_userAgent, _deviceId, _clientId);

                // Create an OTA request containing WiFi information
                var otaRequest = _otaService.CreateWifiOtaRequest(_currentVersion, "", 
                    "xiaozhi-sharp-wifi", "xiaozhi-sharp-wifi-client", ssid, rssi, channel, ip);

                // 发送OTA请求
                _latestOtaResponse = await _otaService.CheckOtaAsync(_otaUrl, otaRequest);

                // Trigger OTA event
                if (OnOtaEvent != null)
                    await OnOtaEvent(_latestOtaResponse);

                return _latestOtaResponse;
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"OTA check abnormal: {ex.Message}");
                return null;
            }
        }
        private async Task AudioService_OnPcmAudioEvent(byte[] pcm)
        {
            byte[] opus = _audioOpusService.Encode(pcm);
            await _chatService.SendAudio(opus);
        }
        private async Task ChatService_OnAudioEvent(byte[] opus)
        {
            if (_audioService != null)
            {
                byte[] pcmData = _audioOpusService.Decode(opus);
                _audioService.AddOutSamples(pcmData);

                if(OnAudioPcmEvent!=null)
                    await OnAudioPcmEvent(pcmData);
            }

            if (OnAudioEvent != null)
                await OnAudioEvent(opus);
        }
        private async Task ChatService_OnMessageEvent(string type, string message)
        {
            //if (type == "answer_stop") {
            //    await StopRecording();
            //}
            if (OnMessageEvent != null)
                await OnMessageEvent(type, message);
        }
        public async Task ChatMessage(string message)
        {
            if (_chatService != null)
                await _chatService.ChatMessage(message);
        }
        public async Task ChatAbort()
        {
            if (_chatService != null)
                await _chatService.ChatAbort();
        }
        public async Task McpMessage(string message)
        {
            if (_chatService != null)
                await _chatService.McpMessage(message);
        }
        /// <summary>
        /// Start recording
        /// </summary>
        /// <param name="type">auto\manual</param>
        /// <returns></returns>
        public async Task StartRecording(string type= "manual")
        {
            if (_audioService != null)
            {
               if (type == "auto")
                {
                    await _chatService.StartRecordingAuto();

                    // Create a new listening task
                    _recordingCts?.Cancel(); // If a task already exists, cancel it first.
                    _recordingCts = new CancellationTokenSource();
                    var token = _recordingCts.Token;
                    
                    _ = Task.Run(async () =>
                    {
                        try 
                        {
                            while (!token.IsCancellationRequested) 
                            {
                                if (_audioService.VadCounter >= Global.VadThreshold)
                                {
                                    LogConsole.InfoLine($"VAD detected silence and automatically ended the recording (counting).: {_audioService.VadCounter})");
                                    _audioService.StopRecording();
                                    await _chatService.StopRecording();
                                    break;
                                }
                                await Task.Delay(100, token); // Check once every 0.1 seconds
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            // The task was cancelled; exit normally.
                        }
                        catch (Exception ex)
                        {
                            LogConsole.ErrorLine($"VAD monitoring task error: {ex.Message}");
                        }
                    }, token); 
                }
                else
                {
                    await _chatService.StartRecording();
                }
                _audioService.StartRecording();
            }
        }
        public async Task StopRecording()
        {
            if (_audioService != null)
            {
                // Cancel VAD monitoring task
                _recordingCts?.Cancel();
                _recordingCts?.Dispose();
                _recordingCts = null;
                
                _audioService.StopRecording();
                await _chatService.StopRecording();
            }
        }

        /// <summary>
        /// Release resources, call this when the application closes.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // Stop VAD listening task
                _recordingCts?.Cancel();
                _recordingCts?.Dispose();
                _recordingCts = null;

                // Stop recording
                if (_audioService != null && _audioService.IsRecording)
                {
                    _audioService.StopRecording();
                }

                // Stop recording
                if (_audioService is IDisposable disposableAudioService)
                {
                    disposableAudioService.Dispose();
                }

                // Release WebSocket connection
                _chatService?.Dispose();

                // Release OTA service
                _otaService?.Dispose();

                // Release audio encoding service
                if (_audioOpusService is IDisposable disposableOpusService)
                {
                    disposableOpusService.Dispose();
                }

                // GC recycling
                GC.SuppressFinalize(this);
            }
        }
    }
}
