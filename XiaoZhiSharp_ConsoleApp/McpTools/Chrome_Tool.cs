using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp_ConsoleApp.McpTools
{
    [McpServerToolType]
    public sealed class Chrome_Tool
    {
        [McpServerTool, Description("Open the website")]
        public static string OpenUrl(string url)
        {
            return OpenUrlInChrome(url);
        }

        public static string OpenUrlInChrome(string url)
        {
            try
            {
                // If the URL is empty, use the default homepage.
                if (string.IsNullOrEmpty(url))
                    url = "https://www.google.com";

                // On Windows, use Process.Start() to directly open the URL
                // The system will automatically select the default browser
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return "Website opened successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening browser: {ex.Message}");

                // If the above methods fail, try launching Chrome directly.
                //TryOpenChromeDirectly(url);
                return "Website failed to open";
            }
        }
    }
}
