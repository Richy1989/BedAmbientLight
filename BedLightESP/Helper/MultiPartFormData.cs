using System;
using System.Text;

namespace BedLightESP.Helper
{
    public class MultiPartFormData
    {
        public MultiPartFormData(string contentType, string fileName, byte[] fileData)
        {
            ContentType = contentType;
            FileName = fileName;
            FileData = fileData;
        }

        public string ContentType { get; set; }
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
    }
}
