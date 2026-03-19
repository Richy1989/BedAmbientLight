using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using BedLightESP.Enumerations;
using BedLightESP.Helper;
using BedLightESP.Logging;
using BedLightESP.Messages;
using BedLightESP.OTA;
using BedLightESP.Settings;
using BedLightESP.WiFi;
using nanoFramework.Json;
using nanoFramework.Runtime.Native;
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
        private readonly ILogger _logger;

        // The stylesheet bytes are cached on first request so the resource is read once.
        private static byte[] _cachedStyleSheetBytes = null;

        // The main page is pre-parsed into alternating segments:
        //   byte[]  → static HTML bytes written directly to the socket
        //   string  → placeholder key whose encoded value is substituted at render time
        // This avoids building a full result string on every request.
        private static ArrayList _pageTokens = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebController"/> class.
        /// </summary>
        /// <param name="settingsManager"></param>
        /// <param name="messageService"></param>
        /// <param name="logger"></param>
        public WebController(ISettingsManager settingsManager, IMessageService messageService, ILogger logger)
        {
            _logger = logger;
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

            if (_cachedStyleSheetBytes == null)
                _cachedStyleSheetBytes = Resources.GetBytes(Resources.BinaryResources.styleSheet);

            e.Context.Response.ContentLength64 = _cachedStyleSheetBytes.Length;
            e.Context.Response.OutputStream.Write(_cachedStyleSheetBytes, 0, _cachedStyleSheetBytes.Length);
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

            if (_pageTokens == null && !TryParsePageTemplate())
            {
                _logger.Error("Error loading default page.");
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
                return;
            }

            StringBuilder ssidEntries = new();
            if (StartBedAmbientLight.AvailableNetworks != null)
            {
                foreach (var item in StartBedAmbientLight.AvailableNetworks)
                {
                    ssidEntries.Append("<option value=\"");
                    ssidEntries.Append(item.Ssid);
                    ssidEntries.Append("\">");
                    ssidEntries.Append(item.Ssid);
                    ssidEntries.Append("</option>");
                }
            }

            var settings = _settingsManager.Settings;

            // Pre-encode all substitution values to UTF-8 bytes once.
            // Each value is small (a few bytes), so this is cheap.
            Hashtable valueBytes = new Hashtable();
            valueBytes["message"]       = Encode(message);
            valueBytes["ssid"]          = Encode(ssidEntries.ToString());
            valueBytes["default_color"] = Encode(settings.DefaultColor);
            valueBytes["mqttServer"]    = Encode(settings.MqttServer);
            valueBytes["mqttPort"]      = Encode(settings.MqttPort.ToString());
            valueBytes["mqttUsername"]  = Encode(settings.MqttUsername);
            valueBytes["mqttPassword"]  = Encode(settings.MqttPassword);
            valueBytes["ledCount"]      = Encode(settings.LedCount.ToString());
            valueBytes["mosi"]          = Encode(settings.SpiSettings.MosiPin.ToString());
            valueBytes["miso"]          = Encode(settings.SpiSettings.MisoPin.ToString());
            valueBytes["clk"]           = Encode(settings.SpiSettings.ClkPin.ToString());
            valueBytes["leftpin"]       = Encode(settings.LeftSidePin.ToString());
            valueBytes["rightpin"]      = Encode(settings.RightSidePin.ToString());
            valueBytes["debugpin"]      = Encode(settings.DebugPin.ToString());

            try
            {
                // Compute Content-Length by summing segment sizes.
                int contentLength = 0;
                for (int i = 0; i < _pageTokens.Count; i++)
                {
                    object token = _pageTokens[i];
                    if (token is byte[])
                        contentLength += ((byte[])token).Length;
                    else if (token is string && valueBytes.Contains((string)token))
                        contentLength += ((byte[])valueBytes[(string)token]).Length;
                }

                e.Context.Response.ContentLength64 = contentLength;

                // Stream each segment directly to the socket — no full result string built.
                var stream = e.Context.Response.OutputStream;
                for (int i = 0; i < _pageTokens.Count; i++)
                {
                    object token = _pageTokens[i];
                    if (token is byte[])
                    {
                        byte[] seg = (byte[])token;
                        stream.Write(seg, 0, seg.Length);
                    }
                    else if (token is string)
                    {
                        string key = (string)token;
                        if (valueBytes.Contains(key))
                        {
                            byte[] seg = (byte[])valueBytes[key];
                            stream.Write(seg, 0, seg.Length);
                        }
                    }
                }

                stream.Flush();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error sending default page. {ex.Message}");
            }
        }

        /// <summary>
        /// Parses the main page resource into pre-encoded byte segments and placeholder keys.
        /// Called once on first request; the full template string is freed when this returns.
        /// </summary>
        private static bool TryParsePageTemplate()
        {
            try
            {
                var rawBytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                string template = Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length);

                var tokens = new ArrayList();
                int start = 0;
                int len = template.Length;

                while (start < len)
                {
                    int open = template.IndexOf("{", start);
                    if (open < 0)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes(template.Substring(start)));
                        break;
                    }

                    int close = template.IndexOf("}", open + 1);
                    if (close < 0)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes(template.Substring(start)));
                        break;
                    }

                    string key = template.Substring(open + 1, close - open - 1);

                    if (open > start)
                        tokens.Add(Encoding.UTF8.GetBytes(template.Substring(start, open - start)));

                    if (IsSimpleKey(key))
                        tokens.Add(key);   // placeholder — resolved at render time
                    else
                        tokens.Add(Encoding.UTF8.GetBytes(template.Substring(open, close - open + 1)));

                    start = close + 1;
                }

                _pageTokens = tokens;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryParsePageTemplate failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsSimpleKey(string key)
        {
            if (key.Length == 0) return false;
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_'))
                    return false;
            }
            return true;
        }

        private static byte[] Encode(string s)
        {
            if (s == null || s.Length == 0) return new byte[0];
            return Encoding.UTF8.GetBytes(s);
        }

        /// <summary>
        /// Handles the POST request to save WiFi settings.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("save_wifi")]
        [Method("POST")]
        public void PostSetWiFiSettings(WebServerEventArgs e)
        {
            _logger.Debug(e.Context.Request.RawUrl);

            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var ssid = (string)hashPars["ssid"];
            var password = (string)hashPars["password"];

            hashPars.Clear();

            if (ssid == null || ssid == string.Empty)
            {
                _logger.Info("SSID is empty");
                PrintDefaultPage(e, "SSID is empty");
                return;
            }

            try
            {
                Wireless80211.Configure(ssid, password);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error setting wireless parameters: {ex.Message}");
                PrintDefaultPage(e, $"Error setting wireless parameters: {ex.Message}");
                return;
            }

            _logger.Info($"WiFi credentials saved for SSID: {ssid} — rebooting.");
            PrintDefaultPage(e, $"Credentials saved for '{ssid}'. Device is rebooting and will connect automatically...");

            // Credentials are saved to flash. Reboot so the device starts cleanly in
            // station mode instead of trying a live AP→STA switch, which fails on ESP32.
            new Thread(() =>
            {
                Thread.Sleep(2000);
                Power.RebootDevice();
            }).Start();
        }

        /// <summary>
        /// Handles the POST request to save MQTT settings.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("save_mqtt")]
        [Method("POST")]
        public void PostSetMQTTSettings(WebServerEventArgs e)
        {
            _logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var user = (string)hashPars["mqttUsername"];
            var password = (string)hashPars["mqttPassword"];
            var server = (string)hashPars["mqttServer"];
            var port = (string)hashPars["mqttPort"];

            hashPars.Clear();

            var settings = _settingsManager.Settings;
            settings.MqttPort = int.Parse(port);
            settings.MqttServer = server;
            settings.MqttUsername = user;
            settings.MqttPassword = password;

            //Start a new thread to write the settings
            _settingsManager.WriteSettings();

            _logger.Info("MQTT settings received.");
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
            _logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var color = (string)hashPars["color_selector"];
            var ledCount = (string)hashPars["ledCount"];
            var ledController = (string)hashPars["ledController"];

            hashPars.Clear();

            var settings = _settingsManager.Settings;
            try
            {
                //Add new LED controller types here 
                if (ledController == "apa102")
                {
                    settings.LedControllerType = LedControllerType.APA102;
                }

                //Set LED count
                settings.LedCount = int.Parse(ledCount);
                //Check if this does not throw exception
                ColorHelper.HexToColor(color);
                settings.DefaultColor = color;
                _settingsManager.WriteSettings();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error setting LED settings: {ex.Message}");
                PrintDefaultPage(e, $"Error setting LED settings: {ex.Message}");
                return;
            }

            _logger.Info($"Selected LED settings to Count {ledCount} and Color {color}.");
            PrintDefaultPage(e, $"Selected LED settings to Count {ledCount} and Color {color}.");
        }

        [Route("controlbutton_pressed")]
        [Method("POST")]
        public void PostControlButtonPressed(WebServerEventArgs e)
        {
            _logger.Debug(e.Context.Request.RawUrl);

            ControlButtonWebData button = null;
            try
            {
                button = (ControlButtonWebData)JsonConvert.DeserializeObject(e.Context.Request.InputStream, typeof(ControlButtonWebData));
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing control button data: {ex.Message}");
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

            _logger.Info($"Control Button {button.Side} pressed.");
        }

        [Route("gpio_settings")]
        [Method("POST")]
        public void PostSetGpioSettings(WebServerEventArgs e)
        {
            _logger.Debug(e.Context.Request.RawUrl);
            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);

            var mosi = (string)hashPars["mosi"];
            var clk = (string)hashPars["clk"];
            var miso = (string)hashPars["miso"];
            var leftpin = (string)hashPars["leftpin"];
            var rightpin = (string)hashPars["rightpin"];
            var debugpin = (string)hashPars["debugpin"];

            hashPars.Clear();

            var settings = _settingsManager.Settings;

            var mosiPinInt = int.Parse(mosi);
            var clkPinInt = int.Parse(clk);
            var misoPinInt = int.Parse(miso);
            var leftSidePinInt = int.Parse(leftpin);
            var rightSidePinInt = int.Parse(rightpin);
            var debugPinInt = int.Parse(debugpin);

            //check if all greater 0 and smaller 49
            if (mosiPinInt < 0 || mosiPinInt > 49 || clkPinInt < 0 || clkPinInt > 49 || misoPinInt < 0 || misoPinInt > 49 || leftSidePinInt < 0 || leftSidePinInt > 49 || rightSidePinInt < 0 || rightSidePinInt > 49 || debugPinInt < 0 || debugPinInt > 49)
            {
                _logger.Error("GPIO settings out of range.");
                PrintDefaultPage(e, "GPIO settings out of range.");
                return;
            }

            //assign the values to settings 
            settings.SpiSettings.MosiPin = mosiPinInt;
            settings.SpiSettings.ClkPin = clkPinInt;
            settings.SpiSettings.MisoPin = misoPinInt;
            settings.LeftSidePin = leftSidePinInt;
            settings.RightSidePin = rightSidePinInt;
            settings.DebugPin = debugPinInt;

            //Start a new thread to write the settings
            _settingsManager.WriteSettings();

            _logger.Info("GPIO settings received.");
            PrintDefaultPage(e, $"GPIO settings configured");
        }

        /// <summary>
        /// Handles the request to get log messages.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("get_logs")]
        [Method("GET")]
        public void GetLogs(WebServerEventArgs e)
        {
            _logger.Debug(e.Context.Request.RawUrl);

            // Assuming _logger has a method to get log messages
            var logs = _logger.GetLogMessages();
            LogsWebData logData = new()
            {
                Logs = logs
            };

            e.Context.Response.ContentType = "application/json";
            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(logData));
            e.Context.Response.ContentLength64 = jsonBytes.Length;
            e.Context.Response.OutputStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        /// <summary>
        /// Handles the POST request to upload a file.
        /// </summary>
        /// <param name="e">The event arguments containing the context of the web request.</param>
        [Route("upload_file")]
        [Method("POST")]
        public void PostUploadFile(WebServerEventArgs e)
        {
            _logger.Debug(e.Context.Request.RawUrl);

            try
            {
                DirectoryInfo otaDir = new(OtaUpdateManager.RootPath);
                otaDir.Create();

                string path = WebHelper.ReceiveFileOverHTTP(e.Context.Request, OtaUpdateManager.RootPath);

                foreach (var file in otaDir.GetFiles())
                {
                    _logger.Debug($"Found file: {file.Name} size: {file.Length} Byte");
                }

                e.Context.Response.StatusCode = (int)HttpStatusCode.OK;
                e.Context.Response.ContentLength64 = 0;

                new Thread(() =>
                {
                    _logger.Info("Starting OTA update process.");
                    OtaUpdateManager.Update();
                }).Start();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error uploading file: {ex.Message}");
            }
        }
    }
}
