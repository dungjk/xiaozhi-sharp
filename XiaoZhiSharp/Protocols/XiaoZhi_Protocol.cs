using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XiaoZhiSharp.Protocols;

public class XiaoZhi_Protocol
{
    private const string KeySessionId = "session_id";
    private const string KeyState = "state";
    private const string KeyType = "type";
    private const string KeyMode = "mode";
    private const string KeyText = "text";

    // 1. When a client connects to a WebSocket server, it needs to include the following headers:
    // Authorization: Bearer<access_token>
    // Protocol-Version: 1
    // Device-Id: <Device MAC address>
    // Client-Id: <Device UUID>

    // 2. After a successful connection, the client sends a hello message:
    public static string Hello(bool mcp = false, string sessionId = "")
    {
        var mcpMessage = new JsonObject
        {
            [KeySessionId] = sessionId,
            [KeyType] = "hello",
            ["version"] = 1,
            ["features"] = new JsonObject
            {
                ["mcp"] = mcp
            },
            ["transport"] = "websocket",
            ["audio_params"] = new JsonObject
            {
                ["format"] = "opus",
                ["sample_rate"] = 24000,
                ["channels"] = 1,
                ["frame_duration"] = 60
            }
        };
        //string message = @"{
        //    ""type"": ""hello"",
        //    ""version"": 1,
        //    ""features"": {
        //        ""mcp"": true
        //      },
        //    ""transport"": ""websocket"",
        //    ""audio_params"": {
        //        ""format"": ""opus"",
        //        ""sample_rate"": 24000,
        //        ""channels"": 1,
        //        ""frame_duration"": 60
        //        },
        //    ""session_id"":""<Session ID>""
        //}";
        //message = message.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace(" ", "");
        //if (string.IsNullOrEmpty(sessionId))
        //    message = message.Replace(",\"session_id\":\"<Session ID>\"", "");
        //else
        //    message = message.Replace("<Session ID>", sessionId);
        string message = ToJsonMessage(mcpMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // 3. The server responds with a hello message.:
    public static string Hello_Receive()
    {
        var helloMessage = new JsonObject
        {
            [KeyType] = "hello",
            ["transport"] = "websocket",
            ["audio_params"] = new JsonObject
            {
                ["sample_rate"] = 24000
            }
        };
        var message = ToJsonMessage(helloMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // Message Type

    // 1. Speech recognition related news

    // Start listening Listening mode: "auto": Automatic stop "manual": Manual stop "realtime": Continuous monitoring
    public static string Listen_Start(string? sessionId, string mode)
    {
        var modeStr = mode switch
        {
            "realtime" or "manual" => mode,
            _ => "auto"
        };
        var listenStartMessage = new JsonObject
        {
            [KeySessionId] = sessionId,
            [KeyType] = "listen",
            [KeyState] = "start",
            [KeyMode] = modeStr
        };
        var message = ToJsonMessage(listenStartMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // Stop listening
    public static string Listen_Stop(string? sessionId)
    {
        var listenStopMessage = new JsonObject
        {
            [KeySessionId] = sessionId,
            [KeyType] = "listen",
            [KeyState] = "stop"
        };
        var message = ToJsonMessage(listenStopMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // wake word detection
    public static string Listen_Detect(string text)
    {
        var listenDetectMessage = new JsonObject
        {
            [KeyType] = "listen",
            [KeyState] = "detect",
            [KeyText] = text
        };
        var message = ToJsonMessage(listenDetectMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // 2. Speech synthesis related news

    // TTS status message sent by the server:
    // State type:
    // "start": Start playing
    // "stop": Stop playing  
    // "sentence_start": New sentence begins
    public static string TTS_Sentence_Start(string text, string? sessionId = "")
    {
        var sentenceMessage = new JsonObject
        {
            [KeyType] = " tts",
            [KeyState] = "sentence_start",
            [KeyText] = text
        };
        if (!string.IsNullOrEmpty(sessionId))
        {
            sentenceMessage[KeySessionId] = sessionId;
        }
        var message = ToJsonMessage(sentenceMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    public static string TTS_Sentence_End(string text = "", string? sessionId = "")
    {
        var sentenceEnd = new JsonObject
        {
            [KeyType] = "tts",
            [KeyState] = "sentence_end"
        };
        if (!string.IsNullOrEmpty(text))
        {
            sentenceEnd[KeyText] = text;
        }
        if (!string.IsNullOrEmpty(sessionId))
        {
            sentenceEnd[KeySessionId] = sessionId;
        }
        var message = ToJsonMessage(sentenceEnd);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    public static string TTS_Start(string sessionId = "")
    {
        var ttsStart = new JsonObject
        {
            [KeyType] = "tts",
            [KeyState] = "start",
        };
        if (!string.IsNullOrEmpty(sessionId))
        {
            ttsStart[KeySessionId] = sessionId;
        }
        var message = ToJsonMessage(ttsStart);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    public static string TTS_Stop(string sessionId = "")
    {
        var ttsStop = new JsonObject
        {
            [KeyType] = "tts",
            [KeyState] = "stop",
        };
        if (!string.IsNullOrEmpty(sessionId))
        {
            ttsStop[KeySessionId] = sessionId;
        }
        var message = ToJsonMessage(ttsStop);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    public static string STT(string text, string? sessionId = "")
    {
        var sttMessage = new JsonObject
        {
            [KeyType] = "stt",
            [KeyText] = text
        };
        if (!string.IsNullOrEmpty(sessionId))
        {
            sttMessage[KeySessionId] = sessionId;
        }
        var message = ToJsonMessage(sttMessage);
        return message;
    }

    // 3. Suspension Announcement
    public static string Abort()
    {
        var abortMessage = new JsonObject
        {
            [KeyType] = "abort",
            ["reason"] = "wake_word_detected"
        };
        var message = ToJsonMessage(abortMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // 4. News related to IoT devices

    // Equipment Description
    public static string Device_Info()
    {
        var deviceInfoMessage = new JsonObject
        {
            [KeyType] = "iot",
            [KeySessionId] = "<Session ID>",
            ["sescriptors"] = "<Device description JSON>"
        };
        var message = ToJsonMessage(deviceInfoMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    // Equipment status
    public static string Device_Status()
    {
        var deviceStatusMessage = new JsonObject
        {
            [KeyType] = "iot",
            [KeySessionId] = "<Session ID>",
            ["descriptors"] = "<Status JSON>"
        };
        var message = ToJsonMessage(deviceStatusMessage);
        Console.WriteLine($"Message sent: {message}");
        return message;
    }

    public static string Deivce_Commands(string commands = "", string sessionId = "")
    {
        var deviceCommandMsg = new JsonObject
        {
            [KeyType] = "iot",
            ["commands"] = commands,
            [KeySessionId] = sessionId
        };
        var message = ToJsonMessage(deviceCommandMsg);
        return message;
    }

    // 5. Emotional status messages
    // Server sends:
    public static string Emotion(string emo)
    {
        var emotionMsg = new JsonObject
        {
            [KeyType] = "llm",
            ["emotion"] = "<Emotional type>"
        };
        return ToJsonMessage(emotionMsg);
    }

    public static string NewSessionId(int byteCount)
    {
        Random random = new Random();
        byte[] bytes = new byte[byteCount];
        random.NextBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    public static string Heartbeat()
    {
        var heartbeatMsg = new JsonObject
        {
            [KeyType] = "heartbeat"
        };
        return ToJsonMessage(heartbeatMsg);
    }

    public static string Mcp(string msg, string? sessionId = "")
    {
        var mcpMessage = new JsonObject
        {
            [KeyType] = "mcp",
            [KeySessionId] = sessionId,
            ["payload"] = JsonNode.Parse(msg)
        };
        return ToJsonMessage(mcpMessage);
    }

    private static string ToJsonMessage(JsonNode node) => node.ToJsonString(new JsonSerializerOptions
    {
        WriteIndented = false
    });

    //public static string Mcp_Initialize_Receive(string sessionId = "")
    //{
    //    JObject jsonObj = new JObject
    //    {
    //        ["session_id"] = sessionId,
    //        ["type"] = "mcp",
    //        ["payload"] = new JObject
    //        {
    //            ["jsonrpc"] = "2.0",
    //            ["id"] = 1,
    //            ["result"] = new JObject
    //            {
    //                ["protocolVersion"] = "2024-11-05",
    //                ["capabilities"] = new JObject
    //                {
    //                    ["tools"] = new JObject { }
    //                },
    //                ["serverInfo"] = new JObject {
    //                  ["name"] = "RestSharp", // Equipment Name (BOARD_NAME)
    //                  ["version"] = "112.1.0.0" // Device firmware version
    //                }

    //}
    //        }
    //    };
    //    string message = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj);
    //    return message;
    //}

    //Binary data transmission

    //- Audio data is transmitted using binary frames
    //- The client sends OPUS-encoded audio data
    //- The server returns OPUS-encoded TTS audio data

    //Error Handling
    //When a network error occurs, the client will receive an error message and close the connection. The client needs to implement a reconnection mechanism.

    //Session Flow
    //1. Establish a WebSocket connection
    //2. Exchange hello messages
    //3. Start voice interaction:
    //- Send "Start Listening"
    //- Send audio data
    //- Receive recognition results
    //- Receive TTS audio
    //4. Close the connection when ending the session

}
