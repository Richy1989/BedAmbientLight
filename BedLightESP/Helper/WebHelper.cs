using System.Collections;
using System.IO;
using System.Text;
using System.Web;

namespace BedLightESP.Helper
{
    public class WebHelper
    {
        /// <summary>
        /// Parses parameters from a given input stream.
        /// </summary>
        /// <param name="inputStream">The input stream containing the parameters.</param>
        /// <returns>A hash table containing the parsed parameters.</returns>
        public static Hashtable ParseParamsFromStream(Stream inputStream)
        {
            byte[] buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, (int)inputStream.Length);

            //return ParseParams(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length));
            return ParseParams(HttpUtility.UrlDecode(Encoding.UTF8.GetString(buffer, 0, buffer.Length)));
        }
        /// <summary>
        /// Parses parameters from a given raw parameter string.
        /// </summary>
        /// <param name="rawParams">The raw parameter string.</param>
        /// <returns>A hash table containing the parsed parameters.</returns>
        public static Hashtable ParseParams(string rawParams)
        {
            Hashtable hash = new ();

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
