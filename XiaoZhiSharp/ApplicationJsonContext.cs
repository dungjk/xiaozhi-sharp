using System.Text.Json.Serialization;
using XiaoZhiSharp.Models;
using XiaoZhiSharp.Protocols;

namespace XiaoZhiSharp;

[JsonSerializable(typeof(OtaRequest))]
[JsonSerializable(typeof(OtaErrorResponse))]
[JsonSerializable(typeof(OtaResponse))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal partial class ApplicationJsonContext : JsonSerializerContext
{
}
