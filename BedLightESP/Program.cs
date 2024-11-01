﻿using System;
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
    internal class Program
    {
        /// <summary>
        /// Gets or sets the available Wi-Fi networks.
        /// </summary>
        public static WifiAvailableNetwork[] AvailableNetworks { get; set; } = new WifiAvailableNetwork[0];

        private static ISettingsManager _settingsManager;
        private static IWebManager _server;
        private static int _connectedCount = 0;
        private static GpioController gpio;

        //Main entry point
        public static void Main()
        {
            Debug.WriteLine("Hello from Bed Ambient Light!");

            DebugHelper.StartMemoryDumpTask();

            ServiceProvider services = ConfigureServices();

            //Load the settings
            _settingsManager = services.GetRequiredService(typeof(ISettingsManager)) as ISettingsManager;
            _settingsManager.LoadSettings();

            //Load the LED manager
            ILedManager ledManager = services.GetRequiredService(typeof(ILedManager)) as ILedManager;

            //Load the Touch manager
            ITouchManager touchManager = services.GetRequiredService(typeof(ITouchManager)) as ITouchManager;

            //Load gpio controller
            gpio = services.GetRequiredService(typeof(GpioController)) as GpioController;

            //For Debugging only use 10 LEDs
            gpio.OpenPin(_settingsManager.Settings.DebugPin, PinMode.Input);
            if (gpio.Read(_settingsManager.Settings.DebugPin) == PinValue.High)
                ledManager.CreateLEDDevice(10);
            else
                ledManager.CreateLEDDevice(_settingsManager.Settings.LedCount);

            //Load the web server
            _server = services.GetRequiredService(typeof(IWebManager)) as IWebManager;

            WifiAdapter wifi = WifiAdapter.FindAllAdapters()[0];

            // Set up the NetworkAPStationChanged event to pick up when stations connect or disconnect
            NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;

            // Set up the AvailableNetworksChanged event to pick up when scan has completed
            wifi.AvailableNetworksChanged += Wifi_AvailableNetworksChanged;

            // Start WiFi scan
            try
            {
                Logger.Info("Starting Wi-Fi scan");
                if (Wireless80211.EnableForScan())
                {
                    Logger.Info("Sleeping 4 sec. to ensure wifi interface is enabled.");
                    Thread.Sleep(4000);
                }
                wifi.ScanAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failure starting a scan operation: {ex.Message}");
                //Try to bring the Wifi or AP up anyways
                ConnectAndStartWebServer();
            }

            //Wait indefinitely
            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>Configure the Dependency Injection Services</summary>
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(typeof(GpioController))
                .AddSingleton(typeof(IMessageService), typeof(MessageService))
                .AddSingleton(typeof(ITouchManager), typeof(TouchManager))
                .AddSingleton(typeof(ILedManager), typeof(LEDManager))
                .AddSingleton(typeof(IWebManager), typeof(WebManager))
                .AddSingleton(typeof(ISettingsManager), typeof(SettingsManager))
                .BuildServiceProvider();
        }

        /// <summary>
        /// Connects to the Wi-Fi network and starts the web server or enable the AP mode.
        /// </summary>
        private static void ConnectAndStartWebServer()
        {
            bool forceAP = false;
            ////For Debugging only use 10 LEDs
            //gpio.OpenPin(_settingsManager.Settings.DebugPin, PinMode.Input);
            //if (gpio.Read(_settingsManager.Settings.DebugPin) == PinValue.High)
            //    forceAP = true;

            // Start WiFi Manager
            if (Wireless80211.IsEnabled() && !forceAP)
            {
                Logger.Info("Wireless80211 is enabled");
                Wireless80211.ConnectOrSetAp();
                if (!_server.IsRunning)
                    _server.Start();
            }
            else
            {
                Logger.Info("Wireless80211 is not enabled");
                WirelessAP.SetWifiAp();
            }
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

            // Connect to the network and start the web server when available networks are scanned
            ConnectAndStartWebServer();
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