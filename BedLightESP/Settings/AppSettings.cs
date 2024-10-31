using System;
using System.Drawing;
using BedLightESP.Helper;

namespace BedLightESP.Settings
{
    public class AppSettings
    {
        public string WifiSsid { get; set; }
        public string WifiPassword { get; set; }
        public string MqttServer { get; set; }
        public int MqttPort { get; set; }
        public string MqttUsername { get; set; }
        public string MqttPassword { get; set; }

        public string DefaultColor { get; set; } = ColorHelpler.ColorToHex(Color.FromArgb(239, 235, 216));

        public void Clone(AppSettings app)
        {
            WifiSsid = app.WifiSsid;
            WifiPassword = app.WifiPassword;
            MqttServer = app.MqttServer;
            MqttPort = app.MqttPort;
            MqttUsername = app.MqttUsername;
            MqttPassword = app.MqttPassword;
        }
    }
}
