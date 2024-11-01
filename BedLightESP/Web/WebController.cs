﻿using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using BedLightESP.Enumerations;
using BedLightESP.Helper;
using BedLightESP.Logging;
using BedLightESP.Messages;
using BedLightESP.Settings;
using BedLightESP.WiFi;
using nanoFramework.Json;
using nanoFramework.WebServer;

namespace BedLightESP.Web
{
    /// <summary>
    /// WebController class handles various web routes and their corresponding actions.
    /// </summary>
    internal class WebController
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IMessageService _messageService;
        public WebController(ISettingsManager settingsManager, IMessageService messageService)
        {
            _settingsManager = settingsManager;
            _messageService = messageService;
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

            string returnPage;
            try
            {
                var bytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(Resources.GetBytes(Resources.BinaryResources.mainPage), 0, bytes.Length));
                returnPage = StringHelper.ReplaceMessage(page, message, "message");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating main page resource. {ex.Message}");
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
                return;
            }

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

            networkEntries.Clear();

            var settings = _settingsManager.Settings;

            returnPage = StringHelper.ReplaceMessage(returnPage, _settingsManager.Settings.DefaultColor, "default_color");

            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttServer, "mqttServer");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.MqttPort}", "mqttPort");
            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttUsername, "mqttUsername");
            returnPage = StringHelper.ReplaceMessage(returnPage, settings.MqttPassword, "mqttPassword");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.LedCount}", "ledCount");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.MosiPin}", "mosi");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.ClkPin}", "clk");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.LeftSidePin}", "leftpin");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.RightSidePin}", "rightpin");
            returnPage = StringHelper.ReplaceMessage(returnPage, $"{settings.DebugPin}", "debugpin");
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

            var settings = _settingsManager.Settings;
            settings.MqttPort = int.Parse(port);
            settings.MqttServer = server;
            settings.MqttUsername = user;
            settings.MqttPassword = password;

            //Start a new thread to write the settings
            new Thread(() => { _settingsManager.WriteSettings(); }).Start();

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

            var settings = _settingsManager.Settings;
            try
            {
                //Set LED count
                settings.LedCount = int.Parse(ledCount);
                //Check if this does not throw exception
                ColorHelper.HexToColor(color);
                settings.DefaultColor = color;
                new Thread(() => { _settingsManager.WriteSettings(); }).Start();
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

        [Route("controlbutton_pressed")]
        [Method("POST")]
        public void PostControlButtonPressed(WebServerEventArgs e)
        {
            Logger.Debug(e.Context.Request.RawUrl);

            ControlButtonWebData button = null;
            try
            {
                button = (ControlButtonWebData)JsonConvert.DeserializeObject(e.Context.Request.InputStream, typeof(ControlButtonWebData));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error parsing control button data: {ex.Message}");
                PrintDefaultPage(e, $"Error parsing control button data: {ex.Message}");
                return;
            }

            new Thread(() =>
            {
                if (button.Side == "left")
                {
                    _messageService.SendMessage(new TouchMessage(ButtonPosition.Left, ClickType.Single, DateTime.UtcNow));
                }
                else if (button.Side == "right")
                {
                    _messageService.SendMessage(new TouchMessage(ButtonPosition.Right, ClickType.Single, DateTime.UtcNow));
                }
                else if (button.Side == "bothDefault")
                {
                    _messageService.SendMessage(new TouchMessage(ButtonPosition.Left, ClickType.Double, DateTime.UtcNow));
                }
                else if (button.Side == "bothRandom")
                {
                    _messageService.SendMessage(new TouchMessage(ButtonPosition.Left, ClickType.DoubleHold, DateTime.UtcNow));
                }
            }).Start();

            Logger.Info($"Control Button {button.Side} pressed.");
        }

        //POST method for gpio_settings route
        [Route("gpio_settings")]
        [Method("POST")]
        public void PostSetGpioSettings(WebServerEventArgs e)
        {
            Logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var mosi = (string)hashPars["mosi"];
            var clk = (string)hashPars["clk"];
            var leftpin = (string)hashPars["leftpin"];
            var rightpin = (string)hashPars["rightpin"];
            var debugpin = (string)hashPars["debugpin"];

            var settings = _settingsManager.Settings;

            var mosiPinInt = int.Parse(mosi);
            var clkPinInt = int.Parse(clk);
            var leftSidePinInt = int.Parse(leftpin);
            var rightSidePinInt = int.Parse(rightpin);
            var debugPinInt = int.Parse(debugpin);

            //check if all greater 0 and smaller 49
            if (mosiPinInt < 0 || mosiPinInt > 49 || clkPinInt < 0 || clkPinInt > 49 || leftSidePinInt < 0 || leftSidePinInt > 49 || rightSidePinInt < 0 || rightSidePinInt > 49 || debugPinInt < 0 || debugPinInt > 49)
            {
                Logger.Error("GPIO settings out of range.");
                PrintDefaultPage(e, "GPIO settings out of range.");
                return;
            }

            //assign the values to settings 
            settings.MosiPin = mosiPinInt;
            settings.ClkPin = clkPinInt;
            settings.LeftSidePin = leftSidePinInt;
            settings.RightSidePin = rightSidePinInt;
            settings.DebugPin = debugPinInt;

            //Start a new thread to write the settings
            new Thread(() => { _settingsManager.WriteSettings(); }).Start();

            Logger.Info("GPIO settings received.");
            PrintDefaultPage(e, $"GPIO settings configured");
        }
    }
}
