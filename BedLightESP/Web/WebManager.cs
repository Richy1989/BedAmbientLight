using System;
using System.Diagnostics;
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
        private readonly ILogger _logger;
        private Thread runner;

        /// <summary>
        /// Gets a value indicating whether the web server is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="WebManager"/> class.</summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="logger">The logger.</param>
        public WebManager(IServiceProvider serviceProvider, ILogger logger)
        {
            _logger = logger;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Starts the web server if it is not already running.
        /// </summary>
        public void Start()
        {

            if (IsRunning) return;

            runner = new Thread(() =>
            {
                try
                {
                    server = new(80, HttpProtocol.Http, new Type[] { typeof(WebController) }, ServiceProvider);
                    server.Start();
                    _logger.Debug("Web server started.");
                    IsRunning = true;
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadAbortException)
                {
                }
            });

            runner.Start();
        }


        /// <summary>
        /// Stops the web server if it is running.
        /// </summary>
        /// <remarks>
        /// This method will attempt to stop the web server.
        /// </remarks>
        public void Stop()
        {
            _logger.Debug("Stopping web server.");
            server.Stop();
            IsRunning = false;
            runner?.Abort();
        }
    }
}
