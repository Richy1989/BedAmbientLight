using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using BedLightESP.LED;
using BedLightESP.Logging;
using BedLightESP.Settings;
using BedLightESP.Touch;
using BedLightESP.Web;
using BedLightESP.WiFi;
using Microsoft.Extensions.DependencyInjection;
using nanoFramework.Hardware.Esp32;

namespace BedLightESP
{
    internal class Program
    {
        public static WifiAvailableNetwork[] AvailableNetworks { get; set; }

        private static IWebManager _server;
        private static bool _wifiApMode = false;
        private static int _connectedCount = 0;

        public static void Main()
        {
            Debug.WriteLine("Hello from Bed Ambient Light!");

            ServiceProvider services = ConfigureServices();

            //Load the settings
            ISettingsManager settingsManager = services.GetRequiredService(typeof(ISettingsManager)) as ISettingsManager;
            settingsManager.LoadSettings();

            //Load the LED manager
            ILedManager ledManager = services.GetRequiredService(typeof(ILedManager)) as ILedManager;

            var gpio = services.GetRequiredService(typeof(GpioController)) as GpioController;
            
            //For Debugging only use 10 LEDs
            gpio.OpenPin(32, PinMode.Input);
            if (gpio.Read(32) == PinValue.High)
                ledManager.CreateLEDDevice(10);
            else
                ledManager.CreateLEDDevice(settingsManager.Settings.LedCount);

            //Load the web server
            _server = services.GetRequiredService(typeof(IWebManager)) as IWebManager;

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
            wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged;

            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>Configure the Dependency Injection Services</summary>
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                //.AddSingleton(typeof())
                .AddSingleton(typeof(GpioController))
                .AddSingleton(typeof(ISettingsManager), typeof(SettingsManager))
                .AddSingleton(typeof(ITouchManager), typeof(TouchManager))
                .AddSingleton(typeof(ILedManager), typeof(LEDManager))
                .AddSingleton(typeof(IWebManager), typeof(WebManager))
                .AddSingleton(typeof(ISettingsManager), typeof(SettingsManager))
                .BuildServiceProvider();
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