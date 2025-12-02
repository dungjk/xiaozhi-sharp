using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    public class SystemInfo
    {
        /// <summary>
        /// Get MAC address
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddress()
        {
            string macAddresses = "";

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only common interface types such as Ethernet, WLAN, and VPN are considered.
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     nic.NetworkInterfaceType == NetworkInterfaceType.Ppp))
                {
                    PhysicalAddress address = nic.GetPhysicalAddress();
                    byte[] bytes = address.GetAddressBytes();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        macAddresses += bytes[i].ToString("X2");
                        if (i != bytes.Length - 1)
                        {
                            macAddresses += ":";
                        }
                    }
                    break; // Typically, only the first MAC address that meets the criteria is selected.
                }
            }

            return macAddresses.ToLower();
        }

        /// <summary>
        /// Generate client UUID (UUID v4 format)
        /// </summary>
        /// <returns>UUID string</returns>
        public static string GenerateClientId()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Get application version
        /// </summary>
        /// <returns>Version string</returns>
        public static string GetApplicationVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }

        /// <summary>
        // Get the User-Agent string
        // </summary>
        // <param name="appName">Application Name</param>
        // <param name="version">Version Number</param>
        // <returns>User-Agent string</returns>
        public static string GetUserAgent(string appName = "xiaozhi-sharp", string? version = null)
        {
            version ??= GetApplicationVersion();
            return $"{appName}/{version}";
        }
    }
}