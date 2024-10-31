using System;
using System.Threading;
using BedLightESP.LogManager;
using nanoFramework.WebServer;

namespace BedLightESP.Manager.WebManager
{
    /// <summary>
    /// Manages the web server for the BedLightESP application.
    /// </summary>
    internal class WebManager
    {
        private WebServer server;
        private readonly ManualResetEvent waitHandle = new(false);

        /// <summary>
        /// Gets a value indicating whether the web server is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts the web server if it is not already running.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            new Thread(() =>
            {
                server = new(80, HttpProtocol.Http, new Type[] { typeof(WebController) });
                server.Start();
                Logger.Debug("Web server started.");
                IsRunning = true;
                waitHandle.WaitOne();
                Logger.Debug("Web server stopped.");
                IsRunning = false;
            }).Start();

            //Thread.Sleep(Timeout.Infinite);
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
            new Thread(() =>
            {
                while (server.IsRunning && !new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(10));
                }
                waitHandle.Set();
            }).Start();
        }
    }
}
