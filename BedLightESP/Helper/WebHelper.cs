using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace BedLightESP.Helper
{
    internal class WebHelper
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

        /// <summary>
        /// Parses the multipart form data from the given HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request containing the multipart form data.</param>
        /// <returns>A <see cref="MultiPartFormData"/> object containing the parsed data, or null if parsing fails.</returns>
        private static MultiPartFormData LocalParseMultipartFormData(HttpListenerRequest request)
        {
            // Read the file data from the request
            byte[] buffer = new byte[request.ContentLength64];

            long bytesRead;
            for (bytesRead = 0; bytesRead < request.ContentLength64;)
            {
                bytesRead += request.InputStream.Read(buffer, (int)bytesRead, (int)(request.ContentLength64 - bytesRead));
            }

            // Convert byte array to string for easy parsing
            string content = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            // Extract boundary
            string boundary = content.Substring(0, content.IndexOf("\r\n"));
            boundary = boundary.Trim(); // Remove any whitespace or new lines

            // Split content by boundary
            var parts = StringHelper.CustomSplit(buffer, Encoding.UTF8.GetBytes(boundary));

            foreach (var partOb in parts)
            {
                string part = Encoding.UTF8.GetString((byte[])partOb, 0, ((byte[])partOb).Length);

                // Look for Content-Disposition header to find the filename
                if (part.Contains("Content-Disposition"))
                {
                    // Extract the filename from Content-Disposition
                    int filenameIndex = part.IndexOf("filename=\"") + 10;
                    int filenameEnd = part.IndexOf("\"", filenameIndex);
                    string fileName = part.Substring(filenameIndex, filenameEnd - filenameIndex);

                    int contentTypeIndex = part.IndexOf("Content-Type: ") + 14;
                    int contentTypeEnd = part.IndexOf("\r\n", contentTypeIndex);
                    string contentType = part.Substring(contentTypeIndex, contentTypeEnd - contentTypeIndex);

                    int contentStart = StringHelper.IndexOf((byte[])partOb, Encoding.UTF8.GetBytes("\r\n\r\n"), 0) + 4;

                    byte[] fileContent = new byte[((byte[])partOb).Length - contentStart];

                    Array.Copy((byte[])partOb, contentStart, fileContent, 0, fileContent.Length);

                    return new MultiPartFormData(contentType, fileName, fileContent);
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the multipart form data from the given HTTP request.
        /// </summary>
        /// <param name="webServer">The web server instance.</param>
        /// <param name="request">The HTTP request containing the multipart form data.</param>
        /// <returns>A <see cref="MultiPartFormData"/> object containing the parsed data, or null if parsing fails.</returns>
        public static MultiPartFormData ParseMultipartFormData(HttpListenerRequest request)
        {
            return LocalParseMultipartFormData(request);
        }

        /// <summary>
        /// <summary>
        /// Receives a file over HTTP and saves it to the specified destination folder.
        /// </summary>
        /// <param name="request">The HTTP request containing the file data.</param>
        /// <param name="destinationFolder">The folder where the received file should be saved.</param>
        /// <returns>The file path of the saved file.</returns>
        /// <exception cref="Exception">Thrown when the file cannot be received over HTTP.</exception>
        public static string ReceiveFileOverHTTP(HttpListenerRequest request, string destinationFolder)
        {
            var data = LocalParseMultipartFormData(request);

            if (data != null)
            {
                string filePath = Path.Combine(destinationFolder, data.FileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Write the file to the destination folder
                File.WriteAllBytes(filePath, data.FileData);
                return filePath;
            }

            throw new Exception("Failed to receive file over HTTP");
        }
    }
}
