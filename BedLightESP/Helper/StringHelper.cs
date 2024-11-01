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
    }
}
