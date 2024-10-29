using System;
using System.Text;

namespace BedLightESP.Helper
{
    public class StringHelper
    {
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
