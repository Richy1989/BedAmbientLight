using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Web;
using BedLightESP.Helper;
using BedLightESP.Logging;
using BedLightESP.Settings;
using BedLightESP.WiFi;
using nanoFramework.WebServer;

namespace BedLightESP.Web
{
    /// <summary>
    /// WebController class handles various web routes and their corresponding actions.
    /// </summary>
    internal class WebController
    {
        private readonly ISettingsManager SettingsManager;

        public WebController(ISettingsManager settingsManager)
        {
            this.SettingsManager = settingsManager;
        }

        /// <summary>
        /// Handles the request for the favicon.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("favicon.ico")]
        public void Favico(WebServerEventArgs e)
        {
            WebServer.SendFileOverHTTP(e.Context.Response, "favico.ico", Resources.GetBytes(Resources.BinaryResources.favicon), "image/ico");
        }

        /// <summary>
        /// Handles the request for the stylesheet.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("style.css")]
        public void Style(WebServerEventArgs e)
        {
            e.Context.Response.ContentType = "text/css";

            var bytes = Resources.GetBytes(Resources.BinaryResources.styleSheet);
            string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(bytes, 0, bytes.Length));

            WebServer.OutPutStream(e.Context.Response, page);
        }

        /// <summary>
        /// Handles the default route requests.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("default.html"), Route("index.html"), Route("/")]
        public void DefaultRoute(WebServerEventArgs e)
        {
            PrintDefaultPage(e);
        }

        /// <summary>
        /// Prints the default page with optional message.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        /// <param name="message">The optional message to display on the page.</param>
        public void PrintDefaultPage(WebServerEventArgs e, string message = "")
        {
            e.Context.Response.ContentType = "text/html; charset=utf-8";

            var bytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
            string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
            string returnPage = StringHelper.ReplaceMessage(page, message, "message");

            StringBuilder networkEntries = new();

            if (Program.AvailableNetworks != null)
            {
                foreach (var item in Program.AvailableNetworks)
                {
                    networkEntries.Append("<option value = \"");
                    networkEntries.Append(item.Ssid);
                    networkEntries.Append("\">");
                    networkEntries.Append(item.Ssid);
                    networkEntries.Append("</option>");
                }
            }

            returnPage = StringHelper.ReplaceMessage(returnPage, networkEntries.ToString(), "ssid");

            var settings = SettingsManager.Settings;

            returnPage = StringHelper.ReplaceMessage(returnPage, SettingsManager.Settings.DefaultColor, "default_color");

            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttServer, "mqttServer");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.MqttPort}", "mqttPort");
            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttUsername, "mqttUsername");
            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttPassword, "mqttPassword");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.LedCount}", "ledCount");

            WebServer.OutPutStream(e.Context.Response, returnPage);
        }

        /// <summary>
        /// Handles the POST request to save WiFi settings.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("save_wifi")]
        [Method("POST")]
        public void PostSetWiFiSettings(WebServerEventArgs e)
        {
            Logger.Debug(e.Context.Request.RawUrl);

            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var ssid = (string)hashPars["ssid"];
            var password = (string)hashPars["password"];

            if (ssid == null || ssid == string.Empty)
            {
                Logger.Info("SSID is empty");
                PrintDefaultPage(e, "SSID is empty");
                return;
            }

            try
            {
                Wireless80211.Configure(ssid, password);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting wireless parameters: {ex.Message}");
                PrintDefaultPage(e, $"Error setting wireless parameters: {ex.Message}");
                return;
            }

            Logger.Info($"Wireless parameters SSID:{ssid} PASSWORD:{password}");
            PrintDefaultPage(e, $"Set wireless parameters SSID: {ssid} PASSWORD: {password}");
        }

        /// <summary>
        /// Handles the POST request to save MQTT settings.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("save_mqtt")]
        [Method("POST")]
        public void PostSetMQTTSettings(WebServerEventArgs e)
        {
            Logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var user = (string)hashPars["mqttUsername"];
            var password = (string)hashPars["mqttPassword"];
            var server = (string)hashPars["mqttServer"];
            var port = (string)hashPars["mqttPort"];

            var settings = SettingsManager.Settings;
            settings.MqttPort = int.Parse(port);
            settings.MqttServer = server;
            settings.MqttUsername = user;
            settings.MqttPassword = password;

            //Start a new thread to write the settings
            new Thread(() => { SettingsManager.WriteSettings(); }).Start();

            Logger.Info("MQTT settings received.");
            PrintDefaultPage(e, $"MQTT Server: {server} configured");
        }

        /// <summary>
        /// Handles the POST request to select led and color settings.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("led_settings")]
        [Method("POST")]
        public void PostSetLEDSettings(WebServerEventArgs e)
        {
            Logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var color = (string)hashPars["color_selector"];
            var ledCount = (string)hashPars["ledCount"];

            var settings = SettingsManager.Settings;
            try
            {
                //Set LED count
                settings.LedCount = int.Parse(ledCount);
                //Check if this does not throw exception
                ColorHelper.HexToColor(color);
                settings.DefaultColor = color;
                new Thread(() => { SettingsManager.WriteSettings(); }).Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting LED settings: {ex.Message}");
                PrintDefaultPage(e, $"Error setting LED settings: {ex.Message}");
                return;
            }

            Logger.Info($"Selected LED settings to Count {ledCount} and Color {color}.");
            PrintDefaultPage(e, $"Selected LED settings to Count {ledCount} and Color {color}.");
        }
    }
}
