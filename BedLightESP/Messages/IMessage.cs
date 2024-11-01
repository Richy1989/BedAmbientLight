using System;

namespace BedLightESP.Messages
{
    internal interface IMessage
    {
        public MessageType MessageType { get; }
    }
}
