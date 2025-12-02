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
    public sealed class WindowsApp_Tool
    {
        [McpServerTool, Description("Open Notepad")]
        public static string OpenNotepad()
        {
            return OpenWindowsApp("Notepad");
        }

        public static string OpenWindowsApp(string name)
        {
            try
            {
                switch(name.ToLower())
                {
                    case "File Explorer":
                        name = "explorer.exe"; // Open File Explorer
                        break;
                    case "Notepad":
                        name = "notepad.exe"; // Open Notepad
                        break;
                    case "calculator":
                        name = "calc.exe"; // Open the calculator
                        break;
                    case "Command Prompt":
                        name = "cmd.exe"; // Open command prompt
                        break;
                    case "powershell":
                        name = "powershell.exe"; // Open PowerShell
                        break;
                    default:
                        // For other applications, simply use the name.
                        break;
                }
                Process.Start(name);
                return "Application opened successfully";
            }
            catch (Exception ex)
            {
                return "Application failed to open";
            }
        }
    }
}
