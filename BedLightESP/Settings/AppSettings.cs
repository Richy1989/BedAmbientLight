using System;
using System.Drawing;
using BedLightESP.Helper;

namespace BedLightESP.Settings
{
    /// <summary>
    /// Represents the application settings for BedLightESP.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the WiFi SSID.
        /// </summary>
        public string WifiSsid { get; set; }

        /// <summary>
        /// Gets or sets the WiFi password.
        /// </summary>
        public string WifiPassword { get; set; }

        /// <summary>
        /// Gets or sets the MQTT server address.
        /// </summary>
        public string MqttServer { get; set; }

        /// <summary>
        /// Gets or sets the MQTT server port.
        /// </summary>
        public int MqttPort { get; set; }

        /// <summary>
        /// Gets or sets the MQTT username.
        /// </summary>
        public string MqttUsername { get; set; }

        /// <summary>
        /// Gets or sets the MQTT password.
        /// </summary>
        public string MqttPassword { get; set; }

        /// <summary>
        /// Gets or sets the default color in hexadecimal format.
        /// </summary>
        public string DefaultColor { get; set; } = ColorHelper.ColorToHex(Color.FromArgb(239, 235, 216));

        /// <summary>
        /// Gets or sets the number of LEDs.
        /// </summary>
        public int LedCount { get; set; } = 58;
    }
}
