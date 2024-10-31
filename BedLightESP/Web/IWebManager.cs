using System;
using System.Text;

namespace BedLightESP.Web
{
    internal interface IWebManager
    {
        /// <summary>
        /// Gets a value indicating whether the web server is running.
        /// </summary>
        public bool IsRunning { get; }

        /// <summary>
        /// Starts the web server if it is not already running.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the web server if it is running.
        /// </summary>
        /// <remarks>
        /// This method will attempt to stop the web server and wait for up to 10 seconds for the server to stop.
        /// </remarks>
        void Stop();
    }
}
