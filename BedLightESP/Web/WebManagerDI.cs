using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using nanoFramework.WebServer;

namespace BedLightESP.Web
{
    /// <summary>
    /// A custom web server that supports dependency injection.
    /// </summary>
    internal class WebServerDI : WebServer
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerDI"/> class.
        /// </summary>
        /// <param name="port">The port on which the server listens.</param>
        /// <param name="protocol">The HTTP protocol used by the server.</param>
        /// <param name="controllers">The array of controller types.</param>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        public WebServerDI(int port, HttpProtocol protocol, Type[] controllers, IServiceProvider serviceProvider) : base(port, protocol, controllers)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Invokes the route callback with dependency injection.
        /// </summary>
        /// <param name="route">The route to invoke.</param>
        /// <param name="context">The HTTP listener context.</param>
        protected override void InvokeRoute(CallbackRoutes route, HttpListenerContext context)
        {
            route.Callback.Invoke(ActivatorUtilities.CreateInstance(_serviceProvider, route.Callback.DeclaringType), new object[] { new WebServerEventArgs(context) });
        }
    }
}