using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XiaoZhiSharp.Models
{
    /// <summary>
    /// OTA Request Model
    /// </summary>
    public class OtaRequest
    {
        [JsonPropertyName("application")]
        public ApplicationInfo Application { get; set; } = new ApplicationInfo();

        [JsonPropertyName("mac_address")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MacAddress { get; set; }

        [JsonPropertyName("uuid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Uuid { get; set; }

        [JsonPropertyName("chip_model_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ChipModelName { get; set; }

        [JsonPropertyName("flash_size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? FlashSize { get; set; }

        [JsonPropertyName("psram_size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? PsramSize { get; set; }

        [JsonPropertyName("partition_table")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<PartitionInfo>? PartitionTable { get; set; }

        [JsonPropertyName("board")]
        public BoardInfo Board { get; set; } = new BoardInfo();

        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Version { get; set; }

        [JsonPropertyName("language")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Language { get; set; }

        [JsonPropertyName("minimum_free_heap_size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? MinimumFreeHeapSize { get; set; }

        [JsonPropertyName("ota")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OtaInfo? Ota { get; set; }
    }

    /// <summary>
    /// Application Information
    /// </summary>
    public class ApplicationInfo
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; } = "xiaozhi";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("elf_sha256")]
        public string ElfSha256 { get; set; } = "";

        [JsonPropertyName("compile_time")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CompileTime { get; set; }

        [JsonPropertyName("idf_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IdfVersion { get; set; }
    }

    /// <summary>
    /// Partition information
    /// </summary>
    public class PartitionInfo
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("subtype")]
        public int Subtype { get; set; }

        [JsonPropertyName("address")]
        public long Address { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// Development board information
    /// </summary>
    public class BoardInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("ssid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Ssid { get; set; }

        [JsonPropertyName("rssi")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Rssi { get; set; }

        [JsonPropertyName("channel")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Channel { get; set; }

        [JsonPropertyName("ip")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Ip { get; set; }

        [JsonPropertyName("mac")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Mac { get; set; }

        [JsonPropertyName("revision")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Revision { get; set; }

        [JsonPropertyName("carrier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Carrier { get; set; }

        [JsonPropertyName("csq")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Csq { get; set; }

        [JsonPropertyName("imei")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Imei { get; set; }

        [JsonPropertyName("iccid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Iccid { get; set; }
    }

    /// <summary>
    /// OTA信息
    /// </summary>
    public class OtaInfo
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";
    }

    /// <summary>
    /// OTA response model
    /// </summary>
    public class OtaResponse
    {
        [JsonPropertyName("activation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ActivationInfo? Activation { get; set; }

        [JsonPropertyName("mqtt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MqttInfo? Mqtt { get; set; }

        [JsonPropertyName("websocket")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WebSocketInfo? WebSocket { get; set; }

        [JsonPropertyName("server_time")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ServerTimeInfo? ServerTime { get; set; }

        [JsonPropertyName("firmware")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FirmwareInfo? Firmware { get; set; }
    }

    /// <summary>
    /// Activation information
    /// </summary>
    public class ActivationInfo
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// MQTT configuration information
    /// </summary>
    public class MqttInfo
    {
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = "";

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = "";

        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "";

        [JsonPropertyName("publish_topic")]
        public string PublishTopic { get; set; } = "";
    }

    /// <summary>
    /// WebSocket configuration information
    /// </summary>
    public class WebSocketInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("token")]
        public string Token { get; set; } = "";
    }

    /// <summary>
    /// Server time information
    /// </summary>
    public class ServerTimeInfo
    {
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "";

        [JsonPropertyName("timezone_offset")]
        public int TimezoneOffset { get; set; }
    }

    /// <summary>
    /// Firmware information
    /// </summary>
    public class FirmwareInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
    }

    /// <summary>
    /// OTA Error Response
    /// </summary>
    public class OtaErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = "";
    }
}
