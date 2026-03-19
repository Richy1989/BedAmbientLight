using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
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
        /// <returns>True if the AP and DHCP server started successfully; false otherwise.</returns>
        public static bool SetWifiAp()
        {
            // Disable station mode so the AP interface gets full control of the radio.
            Wireless80211.Disable();

            if (Setup() == false)
            {
                // AP configuration was just written for the first time (or changed).
                // A reboot is required for the nanoFramework network stack to activate
                // the new AP settings — AutoStart will bring it up on the next boot.
                Logger.Instance?.Warning("Soft AP configuration saved — rebooting to activate.");
                Power.RebootDevice();
            }

            // The AP interface is now active. Start the DHCP server so connecting
            // clients receive an IP address automatically.
            var dhcpServer = new DhcpServer
            {
                CaptivePortalUrl = $"http://{SoftApIP}"
            };

            // Retry DHCP initialisation a few times before giving up.
            // On a freshly flashed device or after a rapid restart, the network stack
            // may not be fully ready on the first attempt.
            const int maxDhcpRetries = 3;
            bool dhcpInitResult = false;

            for (int attempt = 1; attempt <= maxDhcpRetries; attempt++)
            {
                dhcpInitResult = dhcpServer.Start(IPAddress.Parse(SoftApIP), new IPAddress(new byte[] { 255, 255, 255, 0 }));

                if (dhcpInitResult)
                    break;

                Logger.Instance?.Warning($"DHCP start attempt {attempt}/{maxDhcpRetries} failed — retrying...");
                Thread.Sleep(500);
            }

            if (!dhcpInitResult)
            {
                // All retries exhausted — reboot and try again from scratch.
                Logger.Instance?.Error($"DHCP server failed to start after {maxDhcpRetries} attempts — rebooting.");
                Power.RebootDevice();
            }

            Logger.Instance?.Info($"Soft AP running — SSID: {SoftApSsid}, IP: {GetIP()}");
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
        /// <returns>True if the AP is already configured and active; false if configuration
        /// was just written (caller must reboot to activate).</returns>
        public static bool Setup()
        {
            NetworkInterface ni = GetInterface();
            WirelessAPConfiguration wapconf = GetConfiguration();

            // Check whether the AP is already configured correctly.
            // Note: the IPv4Address is intentionally NOT checked here because right after a
            // reboot with AutoStart the static IP may not yet be assigned by the time this
            // runs, which would cause a false negative and trigger an unnecessary reboot loop.
            // The Options flags and SSID are sufficient to confirm the configuration is active.
            if (wapconf.Options == (WirelessAPConfiguration.ConfigurationOptions.Enable |
                                    WirelessAPConfiguration.ConfigurationOptions.AutoStart)
                && wapconf.Ssid == SoftApSsid)
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

