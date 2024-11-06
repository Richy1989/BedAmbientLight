using System.Device.Gpio;
using System.Device.Wifi;
using BedLightESP.LED;
using BedLightESP.Logging;
using BedLightESP.Messages;
using BedLightESP.Settings;
using BedLightESP.Touch;
using BedLightESP.Web;
using Microsoft.Extensions.DependencyInjection;

namespace BedLightESP
{
    internal class OtaRunner
    {
        /// <summary>
        /// Gets or sets the available Wi-Fi networks.
        /// </summary>
        public static WifiAvailableNetwork[] AvailableNetworks { get; set; } = new WifiAvailableNetwork[0];
        public static StartBedAmbientLight startBedAmbientLight;

        /// <summary>
        /// Starts the Bed Ambient Light application.
        /// </summary>
        /// <remarks>
        /// This method initializes the necessary components and starts the Bed Ambient Light application.
        /// </remarks>
        public static void Start()
        {
            ServiceProvider services = ConfigureServices();
            startBedAmbientLight = (StartBedAmbientLight)services.GetService(typeof(StartBedAmbientLight));
            startBedAmbientLight.Start();
        }

        /// <summary>
        /// Stops the Bed Ambient Light application.
        /// </summary>
        public static void Stop()
        {
            startBedAmbientLight.Stop();
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
