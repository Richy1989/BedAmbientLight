﻿using System.Net;
using System.Net.NetworkInformation;
using BedLightESP.Logging;
using Iot.Device.DhcpServer;
using nanoFramework.Runtime.Native;

namespace BedLightESP.WiFi
{
    /// <summary>
    /// Provides methods and properties to manage a wireless access point.
    /// </summary>
    public static class WirelessAP
    {
        /// <summary>
        /// Gets or sets the IP address of the Soft AP.
        /// </summary>
        public static string SoftApIP { get; set; } = "192.168.4.1";

        /// <summary>
        /// Gets or sets the SSID of the Soft AP.
        /// </summary>
        public static string SoftApSsid { get; set; } = $"BedAmbient_{Wireless80211.GetPhysicalAddressString()}";

        /// <summary>
        /// Sets the configuration for the wireless access point.
        /// </summary>
        public static bool SetWifiAp()
        {
            Wireless80211.Disable();
            if (Setup() == false)
            {
                // Reboot device to Activate Access Point on restart
                Logger.Instance?.Warning($"Setup Soft AP, Rebooting device");
                Power.RebootDevice();
            }

            var dhcpServer = new DhcpServer
            {
                CaptivePortalUrl = $"http://{SoftApIP}"
            };
            
            var dhcpInitResult = dhcpServer.Start(IPAddress.Parse(SoftApIP), new IPAddress(new byte[] { 255, 255, 255, 0 }));
            
            if (!dhcpInitResult)
            {
                Logger.Instance?.Error($"Error initializing DHCP server.");
                // This happens after a very freshly flashed device
                Power.RebootDevice();
            }

            Logger.Instance?.Info($"Running Soft AP, waiting for client to connect");
            Logger.Instance?.Info($"Soft AP IP address: {GetIP()} and SSID: {SoftApSsid}");
            Logger.Instance?.Info($"DHCP Init Result: {dhcpInitResult}");
            return dhcpInitResult;
        }

        /// <summary>
        /// Disable the Soft AP for next restart.
        /// </summary>
        public static void Disable()
        {
            WirelessAPConfiguration wapconf = GetConfiguration();
            wapconf.Options = WirelessAPConfiguration.ConfigurationOptions.None;
            wapconf.SaveConfiguration();
        }

        /// <summary>
        /// Set-up the Wireless AP settings, enable and save
        /// </summary>
        /// <returns>True if already set-up</returns>
        public static bool Setup()
        {
            NetworkInterface ni = GetInterface();
            WirelessAPConfiguration wapconf = GetConfiguration();

            // Check if already Enabled and return true
            if (wapconf.Options == (WirelessAPConfiguration.ConfigurationOptions.Enable |
                                    WirelessAPConfiguration.ConfigurationOptions.AutoStart) && ni.IPv4Address == SoftApIP && wapconf.Ssid == SoftApSsid)
            {
                return true;
            }

            // Set up IP address for Soft AP
            ni.EnableStaticIPv4(SoftApIP, "255.255.255.0", SoftApIP);

            // Set Options for Network Interface
            //
            // Enable    - Enable the Soft AP ( Disable to reduce power )
            // AutoStart - Start Soft AP when system boots.
            // HiddenSSID- Hide the SSID
            wapconf.Options = WirelessAPConfiguration.ConfigurationOptions.AutoStart |
                            WirelessAPConfiguration.ConfigurationOptions.Enable;

            // Set the SSID for Access Point.
            wapconf.Ssid = SoftApSsid;

            // Maximum number of simultaneous connections, reserves memory for connections
            wapconf.MaxConnections = 1;

            // To set-up Access point with no Authentication
            wapconf.Authentication = System.Net.NetworkInformation.AuthenticationType.Open;
            wapconf.Password = "";

            // To set up Access point with no Authentication. Password minimum 8 chars.
            //wapconf.Authentication = AuthenticationType.WPA2;
            //wapconf.Password = "password";

            // Save the configuration so on restart Access point will be running.
            wapconf.SaveConfiguration();

            return false;
        }

        /// <summary>
        /// Find the Wireless AP configuration
        /// </summary>
        /// <returns>Wireless AP configuration or NUll if not available</returns>
        public static WirelessAPConfiguration GetConfiguration()
        {
            NetworkInterface ni = GetInterface();
            return WirelessAPConfiguration.GetAllWirelessAPConfigurations()[ni.SpecificConfigId];
        }

        /// <summary>
        /// Gets the network interface for the wireless access point.
        /// </summary>
        /// <returns>The network interface for the wireless access point.</returns>
        public static NetworkInterface GetInterface()
        {
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find WirelessAP interface
            foreach (NetworkInterface ni in Interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.WirelessAP)
                {
                    return ni;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the IP address of the Soft AP
        /// </summary>
        /// <returns>IP address</returns>
        public static string GetIP()
        {
            NetworkInterface ni = GetInterface();
            return ni.IPv4Address;
        }
    }
}

