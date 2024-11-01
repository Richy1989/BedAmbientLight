using System;
using System.Text;

namespace BedLightESP.Messages
{
    internal interface IMessageReceiver
    {
        void ExecuteMessage(IMessage message);
    }
}
