using Microsoft.Extensions.Hosting;
using XiaoZhiSharp;
using XiaoZhiSharp.Utils;
using XiaoZhiSharp.Models;
using XiaoZhiSharp_ConsoleApp.Services;
using System.Text;

class Program
{
    private static XiaoZhiAgent _agent;
    private static McpService _mcpService;
    private static string _audioMode = "";
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var builder = Host.CreateApplicationBuilder(args);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Title = "XiaoZhiSharp client";
        string logoAndCopyright = @"
========================================================================
Welcome to the XiaoZhiSharp client!

Current Features:
1. Voice Messages: Press Enter to start recording; press Enter again to stop recording.
Note: The Caps Lock key can also control recording. Press Caps Lock to start recording, press it again to stop recording.
After recording, the voice message will be automatically sent to the server, which will return the speech recognition result.
2. Text Messages: You can freely enter text messages for conversation.
3. Full round-trip protocol output for easy debugging.

If you have any thoughts or encounter any problems while using it, don't hesitate to contact us:
WeChat: Zhu-Lige Email: zhuLige@qq.com
For any updates, please follow https://github.com/zhulige/xiaozhi-sharp
========================================================================";
        Console.WriteLine(logoAndCopyright);
        Console.ForegroundColor = ConsoleColor.White;

        XiaoZhiSharp.Global.IsDebug = true;
        XiaoZhiSharp.Global.IsMcp = true;

        _mcpService = new McpService();


        _agent = new XiaoZhiAgent();
        //XiaoZhiSharp.Global.SampleRate_WaveOut = 24000;
        //_agent.WsUrl = "wss://coze.nbee.net/xiaozhi/v1/"; 
        //_agent.AudioService = new 
        //_agent.OnAudioPcmEvent =
        _agent.OnMessageEvent += Agent_OnMessageEvent;
        _agent.OnOtaEvent += Agent_OnOtaEvent;
        LogConsole.InfoLine($"Initial OTA URL: {_agent.OtaUrl}");
        LogConsole.InfoLine($"Initial WebSocket URL: {_agent.WsUrl}");
        LogConsole.InfoLine($"Device ID: {_agent.DeviceId}");
        LogConsole.InfoLine($"Client ID: {_agent.ClientId}");
        LogConsole.InfoLine($"User-Agent: {_agent.UserAgent}");
        await _agent.Start();

        _ = Task.Run(async () =>
        {
            while (true)
            {
                if (_agent.ConnectState != System.Net.WebSockets.WebSocketState.Open)
                {
                    await _agent.Restart();
                    LogConsole.InfoLine("Server reconnection...");
                    await Task.Delay(10000);
                }

                bool isCapsLockOn = Console.CapsLock;
                if (isCapsLockOn)
                {
                    if (_agent.IsRecording == false)
                    {
                        _audioMode = "manual";
                        LogConsole.InfoLine("Start recording... Press the Caps Lock button again to stop recording.");
                        await _agent.StartRecording("manual");
                        continue;
                    }
                }
                if (!isCapsLockOn)
                {
                    if (_agent.IsRecording == true)
                    {
                        if (_audioMode == "manual")
                        {
                            await _agent.StopRecording();
                            LogConsole.InfoLine("End of recording");
                            continue;
                        }
                    }
                }
                await Task.Delay(100); // Avoid excessively frequent checkups
            }
        });

        while (true)
        {
            string? input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                if (input.ToLower() == "restart")
                {
                    await _agent.Restart();
                }
                else
                {
                    await _agent.ChatMessage(input);
                }
            }
            else
            {
                if (!_agent.IsRecording)
                {
                    _audioMode = "auto";
                    LogConsole.InfoLine("Start recording... Auto");
                    await _agent.StartRecording("auto");
                }
                else
                {
                    //await _agent.StopRecording();
                    //Console.Title = "XiaoZhiSharp client";
                    //LogConsole.InfoLine("End of recording");
                }
            }
        }
    }

    private static async Task Agent_OnOtaEvent(OtaResponse? otaResponse)
    {
        if (otaResponse != null)
        {
            LogConsole.InfoLine("=== OTA inspection results ===");

            if (otaResponse.Activation != null)
            {
                LogConsole.InfoLine($"Device activation code: {otaResponse.Activation.Code}");
                LogConsole.InfoLine($"Activation message: {otaResponse.Activation.Message}");
            }

            if (otaResponse.Firmware != null && !string.IsNullOrEmpty(otaResponse.Firmware.Url))
            {
                LogConsole.InfoLine($"Firmware update detected: {otaResponse.Firmware.Version}");
                LogConsole.InfoLine($"Download link: {otaResponse.Firmware.Url}");
            }

            if (otaResponse.WebSocket != null)
            {
                LogConsole.InfoLine($"WebSocket server: {otaResponse.WebSocket.Url}");
            }

            if (otaResponse.Mqtt != null)
            {
                LogConsole.InfoLine($"MQTT server: {otaResponse.Mqtt.Endpoint}");
            }

            LogConsole.InfoLine("=== OTA check complete ===");
        }
        else
        {
            LogConsole.InfoLine("OTA check failed, default configuration will be used.");
        }
    }

    private static async Task Agent_OnMessageEvent(string type, string message)
    {
        switch (type.ToLower())
        {
            case "question":
                LogConsole.WriteLine(MessageType.Send, $"[{type}] {message}");
                break;
            case "answer":
                LogConsole.WriteLine(MessageType.Recv, $"[{type}] {message}");
                break;
            case "mcp":
                string resultMessage = await _mcpService.McpMessageHandle(message);
                if (!string.IsNullOrEmpty(resultMessage))
                    await _agent.McpMessage(resultMessage);
                break;
            default:
                LogConsole.InfoLine($"[{type}] {message}");
                break;
        }
        //LogConsole.InfoLine($"[{type}] {message}");

        //if (_mcpClient == null)
        //{
        //    var clientTransport = new StreamClientTransport(
        //        serverInput: _clientToServerPipe.Writer.AsStream(),
        //        serverOutput: _serverToClientPipe.Reader.AsStream());

        //    _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
        //}

        //if (type == "mcp")
        //{
        //    dynamic? mcp = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(message);
        //    if (mcp.method == "initialize")
        //    {
        //        // 构造 Result 数据（非匿名类型，需确保属性名匹配）
        //        var resultData = new
        //        {
        //            protocolVersion = "2024-11-05",
        //            capabilities = _mcpClient.ServerCapabilities, // ServerCapabilities 对象
        //            serverInfo = new
        //            {
        //                name = "RestSharp", // 设备名称 (BOARD_NAME)
        //                version = "112.1.0.0" // 设备固件版本
        //            }
        //        };

        //        // 直接序列化为 JsonNode（关键步骤）
        //        JsonNode resultNode = System.Text.Json.JsonSerializer.SerializeToNode(resultData);
        //        ModelContextProtocol.Protocol.JsonRpcResponse? response = new ModelContextProtocol.Protocol.JsonRpcResponse()
        //        {
        //            Id = new RequestId((long)mcp.id),
        //            JsonRpc = "2.0",
        //            Result = resultNode
        //        };

        //        await _agent.McpMessage(System.Text.Json.JsonSerializer.Serialize(response));
        //    }

        //    if (mcp.method == "tools/list")
        //    {
        //        var tools = await _mcpClient.ListToolsAsync();
        //        List<Tool> toolss = new List<Tool>();
        //        foreach (var item in tools)
        //        {
        //            toolss.Add(item.ProtocolTool);
        //        }
        //        var resultData = new
        //        {
        //            tools = toolss
        //        };

        //        // 直接序列化为 JsonNode（关键步骤）
        //        JsonNode resultNode = System.Text.Json.JsonSerializer.SerializeToNode(resultData);
        //        ModelContextProtocol.Protocol.JsonRpcResponse? response = new ModelContextProtocol.Protocol.JsonRpcResponse()
        //        {
        //            Id = new RequestId((long)mcp.id),
        //            JsonRpc = "2.0",
        //            Result = resultNode
        //        };
        //        var options = new JsonSerializerOptions
        //        {
        //            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 关键配置
        //        };

        //        await _agent.McpMessage(System.Text.Json.JsonSerializer.Serialize(response));
        //    }

        //    if (mcp.method == "tools/call") {
        //        // 解析整个 JSON
        //        JsonNode? root = JsonNode.Parse(message);

        //        // 安全提取 name 和 arguments
        //        string? name = root?["params"]?["name"]?.GetValue<string>();
        //        JsonNode? argumentsNode = root?["params"]?["arguments"];

        //        // 将 arguments 转换为 Dictionary<string, object>
        //        Dictionary<string, object>? arguments = null;
        //        if (argumentsNode != null)
        //        {
        //            arguments = argumentsNode.Deserialize<Dictionary<string, object>>();
        //        }

        //        CallToolResponse? callToolResponse = await _mcpClient.CallToolAsync(name, arguments);
        //        JsonNode jsonNode = System.Text.Json.JsonSerializer.SerializeToNode(callToolResponse);
        //        ModelContextProtocol.Protocol.JsonRpcResponse? response = new ModelContextProtocol.Protocol.JsonRpcResponse()
        //        {
        //            Id = new RequestId((long)mcp.id),
        //            JsonRpc = "2.0",
        //            Result = jsonNode
        //        };

        //        var options = new JsonSerializerOptions
        //        {
        //            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 关键配置
        //        };
        //        await _agent.McpMessage(System.Text.Json.JsonSerializer.Serialize(response));
        //    }
        //}
    }
}