using ModelContextProtocol.Server;
using System.ComponentModel;

namespace XiaoZhiSharp_ConsoleApp.McpTools
{
    [McpServerToolType]
    public sealed class IotThings_Tool
    {
        [McpServerTool, Description("Turn on the lights")]
        public static string Light_ON()
        {
            return "Lights turned on successfully";
        }

        [McpServerTool, Description("Turn off the lights")]
        public static string Light_OFF()
        {
            return "Lights off successfully";
        }
    }
}
