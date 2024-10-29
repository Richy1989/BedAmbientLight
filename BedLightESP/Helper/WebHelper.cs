using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Web;

namespace BedLightESP.Helper
{
    public class WebHelper
    {
        public static Hashtable ParseParamsFromStream(Stream inputStream)
        {
            byte[] buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, (int)inputStream.Length);

            //return ParseParams(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length));
            return ParseParams(HttpUtility.UrlDecode(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length)));
        }

        public static Hashtable ParseParams(string rawParams)
        {
            Hashtable hash = new Hashtable();

            string[] parPairs = rawParams.Split('&');
            foreach (string pair in parPairs)
            {
                string[] nameValue = pair.Split('=');

                if (nameValue.Length > 1)
                {
                    hash.Add(nameValue[0], nameValue[1]);
                }
            }

            return hash;
        }
    }
}
