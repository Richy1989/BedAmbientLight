using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using BedLightESP.LED;
using BedLightESP.Logging;
using BedLightESP.Messages;
using BedLightESP.Settings;
using BedLightESP.Touch;
using BedLightESP.Web;
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

        //Main entry point
        public static void Main()
        {
            Debug.WriteLine("Hello from Bed Ambient Light!");
            ServiceProvider services = ConfigureServices();
            StartBedAmbientLight startBedAmbientLight = (StartBedAmbientLight)services.GetService(typeof(StartBedAmbientLight));
            startBedAmbientLight.Start();
        }

        /// <summary>Configure the Dependency Injection Services</summary>
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(typeof(StartBedAmbientLight))
                .AddSingleton(typeof(ILogger), typeof(Logger))
                .AddSingleton(typeof(GpioController))
                .AddSingleton(typeof(IMessageService), typeof(MessageService))
                .AddSingleton(typeof(ITouchManager), typeof(TouchManager))
                .AddSingleton(typeof(ILedManager), typeof(LEDManager))
                .AddSingleton(typeof(IWebManager), typeof(WebManager))
                .AddSingleton(typeof(ISettingsManager), typeof(SettingsManager))
                .BuildServiceProvider();
        }
    }
}