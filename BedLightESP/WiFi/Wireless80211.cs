//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Device.Wifi;
using System.Net.NetworkInformation;
using System.Threading;
using BedLightESP.LogManager;
using nanoFramework.Networking;

namespace BedLightESP.WiFi
{
    internal class Wireless80211
    {
        /// <summary>
        /// Checks if the wireless 802.11 interface is enabled.
        /// </summary>
        /// <returns>
        /// Returns true if the wireless 802.11 interface is enabled (i.e., the SSID is not null or empty), 
        /// otherwise returns false.
        /// </returns>
        public static bool IsEnabled()
        {
            Wireless80211Configuration wconf = GetConfiguration();
            return !string.IsNullOrEmpty(wconf.Ssid);
        }

        /// <summary>
        /// Get current IP address. Only valid if successfully provisioned and connected
        /// </summary>
        /// <returns>IP address string</returns>
        public static string GetCurrentIPAddress()
        {
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];

            // get first NI ( WiFi on ESP32 )
            return ni.IPv4Address.ToString();
        }

        /// <summary>
        /// Connects to the WiFi or sets the Access Point mode.
        /// </summary>
        /// <returns>True if access point is setup.</returns>
        public static bool ConnectOrSetAp()
        {
            if (IsEnabled())
            {
                if (!WifiNetworkHelper.Reconnect(true, token: new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token))
                {
                    Logger.Info($"Error connecting to WiFi");
                    WirelessAP.SetWifiAp();
                    return true;
                }

                Logger.Info($"Connected to WiFi {GetConfiguration().Ssid} - {WifiNetworkHelper.WifiAdapter}");
                Logger.Info($"IP Address: {GetInterface().IPv4Address}"); 
            }
            else
            {
                WirelessAP.SetWifiAp();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Disable the Wireless station interface.
        /// </summary>
        public static void Disable()
        {
            Wireless80211Configuration wconf = GetConfiguration();
            wconf.Options = Wireless80211Configuration.ConfigurationOptions.None | Wireless80211Configuration.ConfigurationOptions.SmartConfig;
            wconf.SaveConfiguration();
        }

        /// <summary>
        /// Configure and enable the Wireless station interface
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool Configure(string ssid, string password)
        {
            // Make sure we are disconnected before we start connecting otherwise
            // ConnectDHCP will just return success instead of reconnecting.
            WifiAdapter wa = WifiAdapter.FindAllAdapters()[0];
            wa.Disconnect();

            CancellationTokenSource cs = new(30_000);
            Logger.Debug("ConnectDHCP");
            WifiNetworkHelper.Disconnect();

            // Reconfigure properly the normal WiFi
            Wireless80211Configuration wconf = GetConfiguration();
            wconf.Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect | Wireless80211Configuration.ConfigurationOptions.Enable;
            wconf.Ssid = ssid;
            wconf.Password = password;
            wconf.SaveConfiguration();

            WifiNetworkHelper.Disconnect();
            bool success;

            success = WifiNetworkHelper.ConnectDhcp(ssid, password, WifiReconnectionKind.Automatic, true, token: cs.Token);

            if (!success)
            {
                wa.Disconnect();
                // Bug in network helper, we've most likely try to connect before, let's make it manual
                var res = wa.Connect(ssid, WifiReconnectionKind.Automatic, password);
                success = res.ConnectionStatus == WifiConnectionStatus.Success;
                Logger.Debug($"Connected: {res.ConnectionStatus}");
            }

            return success;
        }

        /// <summary>
        /// Get the Wireless station configuration.
        /// </summary>
        /// <returns>Wireless80211Configuration object</returns>
        public static Wireless80211Configuration GetConfiguration()
        {
            NetworkInterface ni = GetInterface();
            return Wireless80211Configuration.GetAllWireless80211Configurations()[ni.SpecificConfigId];
        }

        public static NetworkInterface GetInterface()
        {
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find WirelessAP interface
            foreach (NetworkInterface ni in Interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    return ni;
                }
            }
            return null;
        }
    }
}
