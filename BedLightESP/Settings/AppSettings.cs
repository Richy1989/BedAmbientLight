using System.Drawing;
using BedLightESP.Enumerations;
using BedLightESP.Helper;

namespace BedLightESP.Settings
{
    /// <summary>
    /// Represents the application settings for BedLightESP.
    /// </summary>
    internal class AppSettings
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
        /// Gets or sets the MQTT client ID.
        /// </summary>
        public string MqttClientID { get; set; }

        /// <summary>
        /// Gets or sets the default color in hexadecimal format.
        /// </summary>
        public string DefaultColor { get; set; } = ColorHelper.ColorToHex(Color.FromArgb(239, 235, 216));

        /// <summary>
        /// Gets or sets the number of LEDs.
        /// </summary>
        public int LedCount { get; set; } = 58;

        /// <summary>
        /// Gets or sets the GPIO pin number for the left side LEDs.
        /// </summary>
        public int LeftSidePin { get; set; } = 6;
        /// <summary>
        /// Gets or sets the GPIO pin number for the right side LEDs.
        /// </summary>
        public int RightSidePin { get; set; } = 7;

        /// <summary>
        /// Gets or sets the GPIO pin number for the debug pin.
        /// </summary>
        public int DebugPin { get; set; } = 15;

        /// <summary>
        /// Gets or sets the type of LED controller.
        /// </summary>
        public LedControllerType LedControllerType { get; set; } = LedControllerType.APA102;

        /// <summary>
        /// Gets or sets the SPI settings.
        /// </summary>
        public SpiSettings SpiSettings { get; set; } = new SpiSettings();
    }
}
