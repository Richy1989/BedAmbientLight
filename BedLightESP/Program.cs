using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using BedLightESP.LED;
using BedLightESP.LogManager;
using BedLightESP.Manager;
using BedLightESP.Manager.WebManager;
using BedLightESP.Settings;
using BedLightESP.WiFi;

namespace BedLightESP
{
    /// <summary>
    /// The main program class for the BedLightESP application.
    /// </summary>
    public class Program
    {
        public static WifiAvailableNetwork[] AvailableNetworks { get; set; }

        private static WebManager _server;
        private static bool _wifiApMode = false;
        private static int _connectedCount = 0;

        /// <summary>
        /// The entry point of the application.
        /// </summary>
        public static void Main()
        {
            Logger.Info("Hello from nanoFramework!");

            //Initialize Settings Manager
            SettingsManager.LoadSettings();
            
            //Initialize Touch Manager
            GpioController gpio = new();
            TouchManager touchManager = new(gpio);
            
            //For Debugging only use 10 LEDs
            int ledCount = 58;
            gpio.OpenPin(32, PinMode.Input);
            if (gpio.Read(32) == PinValue.High)
                ledCount = 10;

            //Start LED Manager
            _ = new LEDManager(ledCount, touchManager);

            //Initialize Web Server
            _server = new();

            WifiAdapter wifi = WifiAdapter.FindAllAdapters()[0];

            // Start WiFi scan
            try
            {
                Logger.Info("Starting Wi-Fi scan");
                wifi.ScanAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failure starting a scan operation: {ex}");
            }

            // Start WiFi Manager
            if (Wireless80211.IsEnabled())
            {
                Logger.Info("Wireless80211 is enabled");
                Wireless80211.ConnectOrSetAp();
            }
            else
            {
                _wifiApMode = true;
                Logger.Info("Wireless80211 is not enabled");
                var success = WirelessAP.SetWifiAp();
            }

            // Set up the NetworkAPStationChanged event to pick up when stations connect or disconnect
            NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;

            // Set up the AvailableNetworksChanged event to pick up when scan has completed
            wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged; ;

            //// Signal successful startup with blue onboard LED
            ////gpio.OpenPin(2, PinMode.Output);
            ////gpio.Write(2, PinValue.High);
            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// Event handler for when available Wi-Fi networks are changed.
        /// </summary>
        /// <param name="sender">The Wi-Fi adapter that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private static void Wifi_AvailableNetworksChanged(WifiAdapter sender, object e)
        {
            Logger.Info("WiFi Networks Scanned!");
            AvailableNetworks = sender.NetworkReport.AvailableNetworks;

            if (!_server.IsRunning && !_wifiApMode)
                _server.Start();
        }

        /// <summary>
        /// Event handler for Stations connecting or Disconnecting
        /// </summary>
        /// <param name="NetworkIndex">The index of Network Interface raising event</param>
        /// <param name="e">Event argument</param>
        private static void NetworkChange_NetworkAPStationChanged(int NetworkIndex, NetworkAPStationEventArgs e)
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
