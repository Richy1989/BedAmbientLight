using System.Net;
using nanoFramework.WebServer;
using System.Reflection;
using System.Text;
using System;
using BedLightESP.Helper;
using System.Web;
using nanoFramework.Networking;
using System.Device.Wifi;
using System.Collections;
using System.Diagnostics;
using BedLightESP.LogManager;

namespace BedLightESP.Manager.WebManager
{
    public class WebController
    {
        [Route("favicon.ico")]
        public void Favico(WebServerEventArgs e)
        {
            WebServer.SendFileOverHTTP(e.Context.Response, "favico.ico", Resources.GetBytes(Resources.BinaryResources.favicon), "image/ico");
        }

        [Route("style.css")]
        public void Style(WebServerEventArgs e)
        {
            e.Context.Response.ContentType = "text/css";

            var bytes = Resources.GetBytes(Resources.BinaryResources.styleSheet);
            string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(bytes, 0, bytes.Length));

            WebServer.OutPutStream(e.Context.Response, page);
        }


        [Route("default.html"), Route("index.html"), Route("/")]
        public void DefaultRoute(WebServerEventArgs e )
        {
            PrintDefaultPage(e);
        }

        public void PrintDefaultPage(WebServerEventArgs e, string message = "")
        {
            //string route = $"The route asked is {e.Context.Request.RawUrl.TrimStart('/').Split('/')[0]}";
            e.Context.Response.ContentType = "text/html; charset=utf-8";

            var bytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
            string page = HttpUtility.UrlDecode(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
            string returnPage = StringHelper.ReplaceMessage(page, message, "message");

            StringBuilder networkEntries = new ();

            if (Program.AvailableNetworks != null)
            {
                foreach (var item in Program.AvailableNetworks)
                {
                    networkEntries.Append("<option value = \"");
                    networkEntries.Append(item.Ssid);
                    networkEntries.Append("\">");
                    networkEntries.Append(item.Ssid);
                    networkEntries.Append("</option>");
                }
            }

            returnPage = StringHelper.ReplaceMessage(returnPage, networkEntries.ToString(), "ssid");

            WebServer.OutPutStream(e.Context.Response, returnPage);
        }

        [Route("saveWifi")]
        [Method("POST")]
        public void SelectSettings(WebServerEventArgs e)
        {
            Debug.WriteLine(e.Context.Request.RawUrl);

            Hashtable hashPars = WebHelper.ParseParamsFromStream(e.Context.Request.InputStream);
            
            var ssid = (string)hashPars["ssid"];
            var password = (string)hashPars["password"];

            if (ssid == null || ssid == string.Empty)
            {
                Logger.Info("SSID is empty");
                PrintDefaultPage(e, "SSID is empty");
                return;
            }

            Logger.Info($"Wireless parameters SSID:{ssid} PASSWORD:{password}");
            PrintDefaultPage(e, $"Set wireless parameters SSID: {ssid} PASSWORD: {password}");
        }
    }
}
