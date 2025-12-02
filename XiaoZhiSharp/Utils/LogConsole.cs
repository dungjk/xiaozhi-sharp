using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    // Message type enumeration
    public enum MessageType
    {
        Send,
        Recv,
        Info,
        Warn,
        Erro
    }

    // Log console class
    public class LogConsole
    {
        public static bool IsWrite {get;set;} = true;
        // Methods for recording messages and adding line breaks
        public static void WriteLine(MessageType type, string message)
        {
            WriteMessage(type, message, true);
        }

        public static void WriteLine(string message)
        {
            WriteMessage(MessageType.Info, message, true);
        }

        // Methods to record messages without adding newlines
        public static void Write(MessageType type, string message)
        {
            WriteMessage(type, message, false);
        }
        public static void Write(string message)
        {
            WriteMessage(MessageType.Info, message, false);
        }

        // Private methods are used to handle message output and encapsulate public logic.
        private static void WriteMessage(MessageType type, string message, bool isNewLine)
        {
            if (!Global.IsDebug)
                return;

            if (!IsWrite)
                return;

            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                // Set the console foreground color according to the message type.
                SetConsoleColor(type);

                // Formatted messages
                string formattedMessage = FormatMessage(type, Regex.Unescape(message));

                // Select the output method based on whether a line break is needed.
                if (isNewLine)
                {
                    Console.WriteLine(formattedMessage);
                }
                else
                {
                    Console.Write(formattedMessage);
                }
            }
            finally
            {
                // Restore console to its original color
                Console.ForegroundColor = originalColor;
            }
        }

        // A private method to set the console foreground color based on the message type.
        private static void SetConsoleColor(MessageType type)
        {
            switch (type)
            {
                case MessageType.Send:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case MessageType.Recv:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case MessageType.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case MessageType.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageType.Erro:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    // If the message type does not match, the default gray area will be used.
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }

        // Private method, format message, add timestamp and message type
        private static string FormatMessage(MessageType type, string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] [{type}] {message}";
        }

        // Shortcut: Send a message and add a newline.
        public static void SendLine(string message)
        {
            WriteLine(MessageType.Send, message);
        }

        // Quick method: Send messages without line breaks
        public static void Send(string message)
        {
            Write(MessageType.Send, message);
        }

        // Shortcut: Receive message and add a newline
        public static void ReceiveLine(string message)
        {
            WriteLine(MessageType.Recv, message);
        }

        // Quick method: Receive messages without line breaks
        public static void Receive(string message)
        {
            Write(MessageType.Recv, message);
        }

        // Quick method: Record the information and add a line break.
        public static void InfoLine(string message)
        {
            WriteLine(MessageType.Info, message);
        }

        // Quick method: Record information without line breaks
        public static void Info(string message)
        {
            Write(MessageType.Info, message);
        }

        // Shortcut: Record the warning and add a newline.
        public static void WarningLine(string message)
        {
            WriteLine(MessageType.Warn, message);
        }

        // Quick method: Record warnings without line breaks
        public static void Warning(string message)
        {
            Write(MessageType.Warn, message);
        }

        // Quick method: Record the error and add a newline.
        public static void ErrorLine(string message)
        {
            WriteLine(MessageType.Erro, message);
        }

        // Quick method: Record errors without line breaks
        public static void Error(string message)
        {
            Write(MessageType.Erro, message);
        }
    }
}
