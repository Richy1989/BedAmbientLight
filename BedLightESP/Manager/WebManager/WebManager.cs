using System;
using System.Threading;
using BedLightESP.LogManager;
using nanoFramework.WebServer;

namespace BedLightESP.Manager.WebManager
{
    internal class WebManager
    {
        private WebServer server;
        private ManualResetEvent waitHandle = new (false);
        public bool IsRunning { get; private set; }
        public void Start()
        {
            server = new(80, HttpProtocol.Http, new Type[] { typeof(WebController) });
            server.Start();
            IsRunning = true;
            waitHandle.WaitOne();
            Logger.Debug("Web server stopped");
            IsRunning = false;
            //Thread.Sleep(Timeout.Infinite);
        }

        public void Stop()
        {
            waitHandle.Set();
            server.Stop();
        }
    }
}
