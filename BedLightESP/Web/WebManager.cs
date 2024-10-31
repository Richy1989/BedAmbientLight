using System;
using System.Threading;
using BedLightESP.Logging;
using nanoFramework.WebServer;

namespace BedLightESP.Web
{
    /// <summary>
    /// Manages the web server for the BedLightESP application.
    /// </summary>
    internal class WebManager : IWebManager
    {
        private WebServerDI server;
        private IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets a value indicating whether the web server is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="WebManager"/> class.</summary>
        /// <param name="serviceProvider">The service provider.</param>
        public WebManager(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Starts the web server if it is not already running.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            new Thread(() =>
            {
                server = new(80, HttpProtocol.Http, new Type[] { typeof(WebController) }, ServiceProvider);
                server.Start();
                Logger.Debug("Web server started.");
                IsRunning = true;
                Thread.Sleep(Timeout.Infinite);
            }).Start();
        }

        /// <summary>
        /// Stops the web server if it is running.
        /// </summary>
        /// <remarks>
        /// This method will attempt to stop the web server and wait for up to 10 seconds for the server to stop.
        /// </remarks>
        public void Stop()
        {
            Logger.Debug("Stopping web server.");
            server.Stop();
            IsRunning = false;
        }
    }
}
