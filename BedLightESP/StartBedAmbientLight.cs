using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using BedLightESP.Helper;
using BedLightESP.LED;
using BedLightESP.Logging;
using BedLightESP.Messages;
using BedLightESP.Settings;
using BedLightESP.Touch;
using BedLightESP.Web;
using BedLightESP.WiFi;
using Microsoft.Extensions.DependencyInjection;

namespace BedLightESP
{
    /// <summary>
    /// The main program class for the BedLightESP application.
    /// </summary>
    internal class StartBedAmbientLight
    {
        /// <summary>
        /// Gets or sets the available Wi-Fi networks.
        /// </summary>
        public static WifiAvailableNetwork[] AvailableNetworks { get; set; } = new WifiAvailableNetwork[0];

        private readonly ISettingsManager _settingsManager;
        private readonly ILedManager _ledManager;
        private readonly ITouchManager _touchManager;
        private readonly ILogger _logger;
        private readonly IWebManager _server;
        private readonly GpioController _gpio;
        private readonly ManualResetEvent manualResetEvent;
        private  WifiAdapter _wifi;
        private int _connectedCount = 0;


        public StartBedAmbientLight(ILogger logger, IWebManager webManager, ITouchManager touchManager, ISettingsManager settingsManager, ILedManager ledManager, GpioController gpioController)
        {
            manualResetEvent = new ManualResetEvent(false);
            _logger = logger;
            _settingsManager = settingsManager;
            _gpio = gpioController;
            _ledManager = ledManager;
            _touchManager = touchManager;
            _server = webManager;
        }

        public void Start()
        {
            Debug.WriteLine("Hello from Bed Ambient Light!");

            DebugHelper.StartMemoryDumpTask();

            _settingsManager.LoadSettings();

            _touchManager.Initialize();

            //For Debugging only use 10 LEDs
            _gpio.OpenPin(_settingsManager.Settings.DebugPin, PinMode.Input);
            if (_gpio.Read(_settingsManager.Settings.DebugPin) == PinValue.High)
                _ledManager.CreateLEDDevice(10);
            else
                _ledManager.CreateLEDDevice(_settingsManager.Settings.LedCount);

            _wifi = WifiAdapter.FindAllAdapters()[0];

            // Set up the NetworkAPStationChanged event to pick up when stations connect or disconnect
            NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;

            // Set up the AvailableNetworksChanged event to pick up when scan has completed
            _wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged;

            // Start WiFi scan
            try
            {
                _logger.Info("Starting Wi-Fi scan");
                if (Wireless80211.EnableForScan())
                {
                    _logger.Info("Sleeping 4 sec. to ensure wifi interface is enabled.");
                    Thread.Sleep(4000);
                }
                _wifi.ScanAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failure starting a scan operation: {ex.Message}");
                //Try to bring the Wifi or AP up anyways
                ConnectAndStartWebServer();
            }
        }

        public void Stop()
        {
            NetworkChange.NetworkAPStationChanged -= NetworkChange_NetworkAPStationChanged;
            _wifi.AvailableNetworksChanged -= Wifi_AvailableNetworksChanged;
            _server.Stop();
            manualResetEvent.Set();
        }

        /// <summary>
        /// Connects to the Wi-Fi network and starts the web server or enable the AP mode.
        /// </summary>
        private void ConnectAndStartWebServer()
        {
            bool forceAP = false;
            ////For Debugging only use 10 LEDs
            //gpio.OpenPin(_settingsManager.Settings.DebugPin, PinMode.Input);
            //if (gpio.Read(_settingsManager.Settings.DebugPin) == PinValue.High)
            //    forceAP = true;

            // Start WiFi Manager
            if (Wireless80211.IsEnabled() && !forceAP)
            {
                _logger.Info("Wireless80211 is enabled");
                Wireless80211.ConnectOrSetAp();
                if (!_server.IsRunning)
                    _server.Start();
            }
            else
            {
                _logger.Info("Wireless80211 is not enabled");
                WirelessAP.SetWifiAp();
            }
        }

        /// <summary>
        /// Event handler for when available Wi-Fi networks are changed.
        /// </summary>
        /// <param name="sender">The Wi-Fi adapter that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Wifi_AvailableNetworksChanged(WifiAdapter sender, object e)
        {
            _logger.Info("WiFi Networks Scanned!");
            AvailableNetworks = sender.NetworkReport.AvailableNetworks;

            // Connect to the network and start the web server when available networks are scanned
            ConnectAndStartWebServer();
        }

        /// <summary>
        /// Event handler for Stations connecting or Disconnecting
        /// </summary>
        /// <param name="NetworkIndex">The index of Network Interface raising event</param>
        /// <param name="e">Event argument</param>
        private void NetworkChange_NetworkAPStationChanged(int NetworkIndex, NetworkAPStationEventArgs e)
        {
            Debug.WriteLine($"NetworkAPStationChanged event Index:{NetworkIndex} Connected:{e.IsConnected} Station:{e.StationIndex} ");

            // if connected then get information on the connecting station 
            if (e.IsConnected)
            {
                WirelessAPConfiguration wapconf = WirelessAPConfiguration.GetAllWirelessAPConfigurations()[0];
                WirelessAPStation station = wapconf.GetConnectedStations(e.StationIndex);

                string macString = BitConverter.ToString(station.MacAddress);
                Debug.WriteLine($"Station mac {macString} Rssi:{station.Rssi} PhyMode:{station.PhyModes} ");

                _connectedCount++;

                // Start web server when it connects otherwise the bind to network will fail as 
                // no connected network. Start web server when first station connects 
                if (_connectedCount == 1)
                {
                    // Wait for Station to be fully connected before starting web server
                    // other you will get a Network error
                    Thread.Sleep(2000);
                    if (!_server.IsRunning)
                        _server.Start();
                }
            }
            else
            {
                // Station disconnected. When no more station connected then stop web server
                if (_connectedCount > 0)
                {
                    _connectedCount--;
                    if (_connectedCount == 0)
                        _server.Stop();
                }
            }
        }
    }
}