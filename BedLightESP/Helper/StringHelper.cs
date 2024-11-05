using System;
using System.Collections;

namespace BedLightESP.Helper
{
    /// <summary>
    /// Provides helper methods for string manipulation.
    /// </summary>
    public class StringHelper
    {
        /// <summary>
        /// Replaces a keyword in the given page with the specified message.
        /// </summary>
        /// <param name="page">The original string containing the keyword.</param>
        /// <param name="message">The message to replace the keyword with.</param>
        /// <param name="keyword">The keyword to be replaced.</param>
        /// <returns>A new string with the keyword replaced by the message.</returns>
        public static string ReplaceMessage(string page, string message, string keyword)
        {
            int index = page.IndexOf("{" + keyword + "}");
            if (index >= 0)
            {
                return page.Substring(0, index) + message + page.Substring(index + keyword.Length + 2);
            }

            return page;
        }

        /// <summary>
        /// Splits a byte array into a list of byte arrays using a specified byte array delimiter.
        /// </summary>
        /// <param name="data">The byte array to be split.</param>
        /// <param name="delimiter">The byte array delimiter used to split the data.</param>
        /// <returns>A list of byte arrays, split by the specified delimiter.</returns>
        public static IList CustomSplit(byte[] data, byte[] delimiter)
        {
            var result = new ArrayList();
            int start = 0;
            int index;

            // Loop to find each occurrence of the delimiter in the data
            while ((index = IndexOf(data, delimiter, start)) != -1)
            {
                // Length of the part to copy between delimiters
                int length = index - start;

                // Copy the part between the last position and the current delimiter
                byte[] part = new byte[length];
                Array.Copy(data, start, part, 0, length);
                if (part.Length > 0)
                    result.Add(part);

                // Move the start position past the current delimiter
                start = index + delimiter.Length;
            }

            // Add any remaining data after the last delimiter
            if (start < data.Length)
            {
                byte[] lastPart = new byte[data.Length - start];
                Array.Copy(data, start, lastPart, 0, lastPart.Length);
                result.Add(lastPart);
            }

            return result;
        }

        // Helper function to find the index of a delimiter in a byte array
        public static int IndexOf(byte[] data, byte[] delimiter, int start)
        {
            // Loop through data starting from the 'start' index
            for (int i = start; i <= data.Length - delimiter.Length; i++)
            {
                bool match = true;

                // Check if the delimiter matches the current segment in data
                for (int j = 0; j < delimiter.Length; j++)
                {
                    if (data[i + j] != delimiter[j])
                    {
                        match = false;
                        break;
                    }
                }

                // If match found, return the index
                if (match)
                {
                    return i;
                }
            }
            return -1; // Return -1 if the delimiter is not found
        }
    }
}
