using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Utils;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace XiaoZhiSharp.Services.Chat
{
    public class ChatService : IDisposable
    {
        private string TAG = "XiaoZhi";
        private string _wsUrl { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
        private string? _token { get; set; } = "test-token";
        private string? _deviceId { get; set; }
        private string? _sessionId = "";
        // First connection
        private bool _isFirst = true;
        private ClientWebSocket? _webSocket = null;
        private bool _disposed = false;
        private System.Timers.Timer _onAudioTimeout;

        #region property
        public WebSocketState ConnectState { get { return _webSocket?.State ?? WebSocketState.None; } }
        #endregion

        #region event
        public delegate Task MessageEventHandler(string type, string message);
        public event MessageEventHandler? OnMessageEvent = null;

        public delegate Task AudioEventHandler(byte[] opus);
        public event AudioEventHandler? OnAudioEvent = null;
        #endregion

        #region Constructor
        public ChatService(string wsUrl, string token, string deviceId)
        {
            _wsUrl = wsUrl;
            _token = token;
            _deviceId = deviceId;
        }
        #endregion

        public void Start()
        {
            Uri uri = new Uri(_wsUrl);
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", "Bearer " + _token);
            _webSocket.Options.SetRequestHeader("Protocol-Version", "1");
            _webSocket.Options.SetRequestHeader("Device-Id", _deviceId);
            _webSocket.Options.SetRequestHeader("Client-Id", Guid.NewGuid().ToString());
            _webSocket.ConnectAsync(uri, CancellationToken.None);
            LogConsole.InfoLine($"{TAG} Connecting...");

            Task.Run(async () =>
            {
                await ReceiveMessagesAsync();
            });

            // Speech synthesis broadcast timeout
            _onAudioTimeout = new System.Timers.Timer(500);
            _onAudioTimeout.Elapsed += async (sender, e) => await OnAudioTimeout();
            _onAudioTimeout.AutoReset = false;
        }

        /// <summary>
        /// 语音播报完成
        /// </summary>
        private async Task OnAudioTimeout()
        {
            if (OnMessageEvent != null)
                await OnMessageEvent("audio_stop", "");
        }

        private async Task ReceiveMessagesAsync()
        {
            if (_webSocket == null)
                return;

            var buffer = new byte[1024 * 15];
            while (true)
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        // first
                        if (_isFirst)
                        {
                            _isFirst = false;
                            LogConsole.InfoLine($"{TAG} Connection successful");
                            await SendMessageAsync(XiaoZhi_Protocol.Hello(Global.IsMcp));
                        }

                        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        byte[] messageBytes = new byte[result.Count];
                        Array.Copy(buffer, messageBytes, result.Count);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(messageBytes);
                            LogConsole.ReceiveLine($"{TAG} {message}");

                            if (!string.IsNullOrEmpty(message))
                            {
                                using var jsonDocument = JsonDocument.Parse(message);
                                if (jsonDocument == null)
                                {
                                    LogConsole.ErrorLine($"{TAG} Received message format error: {message}");
                                    continue;
                                }
                                _sessionId = jsonDocument.RootElement.GetProperty("session_id").GetString();
                                var messageType = jsonDocument.RootElement.GetProperty("type").GetString();
                                if (messageType == "mcp")
                                {
                                    if (OnMessageEvent != null)
                                    {
                                        var payload = jsonDocument.RootElement.GetProperty("payload");
                                        await OnMessageEvent("mcp", payload.ToString());
                                    }
                                }
                                // ask
                                if (messageType == "stt")
                                {
                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("question", System.Convert.ToString(jsonDocument.RootElement.GetProperty("text").GetString()));

                                    if (OnMessageEvent != null)
                                        await OnMessageEvent("audio_start", "");
                                }
                                // answer
                                if (messageType == "tts")
                                {
                                    var messageState = jsonDocument.RootElement.GetProperty("state").GetString();
                                    if (messageState == "sentence_start")
                                    {
                                        if (OnMessageEvent != null)
                                            await OnMessageEvent("answer", System.Convert.ToString(jsonDocument.RootElement.GetProperty("text").GetString()));
                                    }

                                    if (messageState == "stop")
                                    {
                                        if (OnMessageEvent != null)
                                            await OnMessageEvent("answer_stop", "");
                                    }
                                }
                                // emotion
                                if (messageType == "llm")
                                {
                                    if (OnMessageEvent != null)
                                    {
                                        await OnMessageEvent("emotion", System.Convert.ToString(jsonDocument.RootElement.GetProperty("emotion").GetString()));
                                    }
                                    if (OnMessageEvent != null)
                                    {
                                        await OnMessageEvent("emotion_text", System.Convert.ToString(jsonDocument.RootElement.GetProperty("text").GetString()));
                                    }
                                }
                            }

                        }

                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            _onAudioTimeout.Stop();
                            // Triggering event
                            if (OnAudioEvent != null)
                                await OnAudioEvent(messageBytes);
                            _onAudioTimeout.Start();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogConsole.ErrorLine($"{TAG} {ex.Message}");
                    }
                }

                //Thread.Sleep(1); // Avoid overly frequent loops
            }
        }
        private async Task SendMessageAsync(string message)
        {
            if (_webSocket == null)
                return;

            if (_webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                LogConsole.SendLine($"{TAG} {message}");
            }
        }
        private async Task SendAudioAsync(byte[] opus)
        {
            if (_webSocket == null)
                return;

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(opus), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
        public async Task SendAudio(byte[] audio)
        {
            await SendAudioAsync(audio);
        }
        public async Task ChatAbort()
        {
            await SendMessageAsync(XiaoZhi_Protocol.Abort());
        }
        public async Task ChatMessage(string message)
        {
            //await ChatAbort();
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Detect(message));
        }
        public async Task McpMessage(string message)
        {
            await SendMessageAsync(XiaoZhi_Protocol.Mcp(message, _sessionId));
        }
        public async Task StartRecording()
        {
            //await ChatAbort();
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Start("", "manual"));
        }
        public async Task StartRecordingAuto()
        {
            //await ChatAbort();
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Start("", "auto"));
        }
        public async Task StopRecording()
        {
            await SendMessageAsync(XiaoZhi_Protocol.Listen_Stop(_sessionId));
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // Close WebSocket connection
                try
                {
                    if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting))
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None).Wait();
                    }
                }
                catch (Exception ex)
                {
                    LogConsole.ErrorLine($"{TAG} An error occurred while closing the WebSocket connection.: {ex.Message}");
                }

                _webSocket?.Dispose();
                _webSocket = null;
            }
        }
    }
}
