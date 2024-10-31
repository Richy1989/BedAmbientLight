using System.Net;
using nanoFramework.WebServer;
using System.Reflection;
using System.Text;
using System;
using BedLightESP.Helper;
using System.Web;
using nanoFramework.Networking;
using System.Device.Wifi;
using System.Collections;
using System.Diagnostics;
using BedLightESP.LogManager;
using BedLightESP.WiFi;
using BedLightESP.Settings;
using System.Threading;

namespace BedLightESP.Manager.WebManager
{
    public class WebController
    {
        [Route("favicon.ico")]
        public void Favico(WebServerEventArgs e)
        {
            WebServer.SendFileOverHTTP(e.Context.Response, "favico.ico", Resources.GetBytes(Resources.BinaryResources.favicon), "image/ico");
        }

        [Route("style.css")]
        public void Style(WebServerEventArgs e)
        {
            e.Context.Response.ContentType = "text/css";

            var bytes = Resources.GetBytes(Resources.BinaryResources.styleSheet);
            string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(bytes, 0, bytes.Length));

            WebServer.OutPutStream(e.Context.Response, page);
        }


        [Route("default.html"), Route("index.html"), Route("/")]
        public void DefaultRoute(WebServerEventArgs e )
        {
            PrintDefaultPage(e);
        }

        public void PrintDefaultPage(WebServerEventArgs e, string message = "")
        {
            //string route = $"The route asked is {e.Context.Request.RawUrl.TrimStart('/').Split('/')[0]}";
            e.Context.Response.ContentType = "text/html; charset=utf-8";

            var bytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
            string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
            string returnPage = StringHelper.ReplaceMessage(page, message, "message");

            StringBuilder networkEntries = new ();

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
            
            returnPage = StringHelper.ReplaceMessage(returnPage, ColorHelpler.ColorToHex(ColorHelpler.WarmWhite), "default_color");

            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttServer, "mqttServer");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.MqttPort}", "mqttPort");
            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttUsername, "mqttUsername");
            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttPassword, "mqttPassword");

            WebServer.OutPutStream(e.Context.Response, returnPage);
        }

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
            new Thread(() => { SettingsManager.WriteSettings(); } ).Start();

            Logger.Info("MQTT settings received.");
            PrintDefaultPage(e, $"MQTT Server: {server} configured");
        }

        [Route("select_color")]
        [Method("POST")]
        public void PostSelectColorSettings(WebServerEventArgs e)
        {
            Logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var color = (string)hashPars["color_selector"];

            var settings = SettingsManager.Settings;
            try
            {
                //Check if this does not throw exception
                ColorHelpler.HexToColor(color);
                settings.DefaultColor = color;
                new Thread(() => { SettingsManager.WriteSettings(); }).Start();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting color: {ex.Message}");
                PrintDefaultPage(e, $"Error setting color: {ex.Message}");
                return;
            }

            Logger.Info($"Selected Color: {color}");
            PrintDefaultPage(e, $"Set default color to: {color}");
        }
    }
}
