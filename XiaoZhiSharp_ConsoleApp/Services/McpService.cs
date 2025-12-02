using XiaoZhiSharp_ConsoleApp.McpTools;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace XiaoZhiSharp_ConsoleApp.Services
{
    public class McpService
    {
        private static IHost? _host;
        private static IMcpClient _mcpClient;
        public McpService()
        {
            var builder = Host.CreateApplicationBuilder();

            var mcpBuilder = builder.Services
                .AddMcpServer()
                .WithStreamServerTransport(Global.McpClientToServerPipe.Reader.AsStream(), Global.McpServerToClientPipe.Writer.AsStream())
                .WithTools<IotThings_Tool>();

            _host = builder.Build();
            _host.StartAsync();
        }

        public async Task<string> McpMessageHandle(string message)
        {
            try
            {
                if (_mcpClient == null)
                {
                    var clientTransport = new StreamClientTransport(
                        serverInput: Global.McpClientToServerPipe.Writer.AsStream(),
                        serverOutput: Global.McpServerToClientPipe.Reader.AsStream());

                    _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
                }

                JsonNode? root = JsonNode.Parse(message);
                if (root == null)
                    return string.Empty;

                var mcpMethod = root["method"]?.GetValue<string>() ?? string.Empty;
                var mcpId = root["id"]?.GetValue<long>() ?? 0;
                if (mcpMethod == "initialize")
                {
                    Global.McpVisionUrl = root?["params"]?["capabilities"]?["vision"]?["url"]?.GetValue<string>();
                    Global.McpVisionToken = root?["params"]?["capabilities"]?["vision"]?["token"]?.GetValue<string>();


                    // Handling initialization requests
                    var resultData = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = _mcpClient.ServerCapabilities,
                        serverInfo = new
                        {
                            name = "XiaoZhiSharp",
                            version = Global.CurrentVersion,
                        }
                    };

                    JsonNode resultNode = JsonSerializer.SerializeToNode(resultData);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId(mcpId),
                        JsonRpc = "2.0",
                        Result = resultNode
                    };

                    return JsonSerializer.Serialize(response);

                }

                if (mcpMethod == "tools/list")
                {
                    // Processing tool list request
                    var tools = await _mcpClient.ListToolsAsync();
                    List<Tool> toolsList = new List<Tool>();
                    foreach (var item in tools)
                    {
                        toolsList.Add(item.ProtocolTool);
                    }

                    var resultData = new
                    {
                        tools = toolsList
                    };

                    JsonNode resultNode = JsonSerializer.SerializeToNode(resultData);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId(mcpId),
                        JsonRpc = "2.0",
                        Result = resultNode
                    };

                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    return JsonSerializer.Serialize(response, options);
                }

                if (mcpMethod == "tools/call")
                {
                    // Processing tool call requests
                    //JsonNode? root = JsonNode.Parse(message);

                    string? name = root?["params"]?["name"]?.GetValue<string>();
                    JsonNode? argumentsNode = root?["params"]?["arguments"];

                    Dictionary<string, object>? arguments = null;
                    if (argumentsNode != null)
                    {
                        arguments = argumentsNode.Deserialize<Dictionary<string, object>>();
                    }

                    ModelContextProtocol.Protocol.CallToolResult? callToolResponse = await _mcpClient.CallToolAsync(name, arguments);
                    JsonNode jsonNode = JsonSerializer.SerializeToNode(callToolResponse);
                    JsonRpcResponse? response = new JsonRpcResponse()
                    {
                        Id = new RequestId(mcpId),
                        JsonRpc = "2.0",
                        Result = jsonNode
                    };

                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    return JsonSerializer.Serialize(response, options);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"MCP handles exceptions: {ex.Message}";
            }

            return string.Empty;
        }
    }
}
