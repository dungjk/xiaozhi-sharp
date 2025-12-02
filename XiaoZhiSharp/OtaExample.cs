using System;
using System.Threading.Tasks;
using XiaoZhiSharp.Protocols;
using XiaoZhiSharp.Models;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp
{
    /// <summary>
    /// OTA Function Usage Examples
    /// </summary>
    public class OtaExample
    {
        /// <summary>
        /// Basic OTA usage examples
        /// </summary>
        public static async Task BasicExample()
        {
            LogConsole.InfoLine("=== Basic OTA usage examples ===");

            // Create XiaoZhiAgent instance
            var agent = new XiaoZhiAgent();

            // Subscription Events
            agent.OnMessageEvent += (type, message) =>
            {
                LogConsole.InfoLine($"[{type}] {message}");
                return Task.CompletedTask;
            };

            agent.OnOtaEvent += (otaResponse) =>
            {
                if (otaResponse != null)
                {
                    LogConsole.InfoLine("OTA check successful, server configuration obtained.");
                    
                    // 可以访问各种配置信息
                    if (otaResponse.WebSocket != null)
                    {
                        LogConsole.InfoLine($"WebSocket URL: {otaResponse.WebSocket.Url}");
                        LogConsole.InfoLine($"WebSocket Token: {otaResponse.WebSocket.Token}");
                    }

                    if (otaResponse.Mqtt != null)
                    {
                        LogConsole.InfoLine($"MQTT server: {otaResponse.Mqtt.Endpoint}");
                        LogConsole.InfoLine($"MQTT Client ID: {otaResponse.Mqtt.ClientId}");
                    }
                }
                else
                {
                    LogConsole.InfoLine("OTA check failed, use default configuration.");
                }
                return Task.CompletedTask;
            };

            // Start (OTA check will be performed automatically)
            await agent.Start();

            LogConsole.InfoLine("XiaoZhiAgent has started; OTA check complete.");
        }

        /// <summary>
        /// Custom OTA Request Example
        /// </summary>
        public static async Task CustomOtaExample()
        {
            LogConsole.InfoLine("=== Custom OTA Request Example ===");

            var agent = new XiaoZhiAgent();

            // Set custom parameters
            agent.CurrentVersion = "1.2.3";
            agent.UserAgent = "custom-device/1.2.3";

            agent.OnOtaEvent += (otaResponse) =>
            {
                LogConsole.InfoLine("Received OTA response");
                return Task.CompletedTask;
            };

            // Manually perform OTA check (with WiFi information)
            var otaResponse = await agent.CheckOtaUpdateWithWifi(
                ssid: "Test-WiFi",
                rssi: -45,
                channel: 6,
                ip: "192.168.1.100"
            );

            if (otaResponse != null)
            {
                LogConsole.InfoLine("Custom OTA check successful");
            }
        }

        /// <summary>
        /// OTA check example only (without starting WebSocket)
        /// </summary>
        public static async Task OtaOnlyExample()
        {
            LogConsole.InfoLine("=== OTA inspection example only ===");

            var agent = new XiaoZhiAgent();

            // Perform OTA check only, do not initiate WebSocket connection.
            var otaResponse = await agent.CheckOtaUpdate();

            if (otaResponse != null)
            {
                LogConsole.InfoLine("OTA check complete");

                // Check for firmware updates.
                if (otaResponse.Firmware != null && !string.IsNullOrEmpty(otaResponse.Firmware.Url))
                {
                    LogConsole.InfoLine($"Firmware update detected: {otaResponse.Firmware.Version}");
                    LogConsole.InfoLine($"Download link: {otaResponse.Firmware.Url}");

                    // Here you can add logic for downloading firmware.
                }
                else
                {
                    LogConsole.InfoLine("No firmware update");
                }

                // Display server time
                if (otaResponse.ServerTime != null)
                {
                    var serverTime = DateTimeOffset.FromUnixTimeMilliseconds(otaResponse.ServerTime.Timestamp);
                    LogConsole.InfoLine($"Server time: {serverTime}");
                    LogConsole.InfoLine($"Time zone: {otaResponse.ServerTime.Timezone}");
                }

                // Display activation information
                if (otaResponse.Activation != null)
                {
                    LogConsole.InfoLine($"Activation code: {otaResponse.Activation.Code}");
                    LogConsole.InfoLine($"Activation message: {otaResponse.Activation.Message}");
                }
            }
            else
            {
                LogConsole.InfoLine("OTA check failed");
            }
        }
    }
} 