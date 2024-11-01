using System;

namespace BedLightESP.Messages
{
    /// <summary>
    /// Defines the contract for a message service that can send messages and register clients.
    /// </summary>
    internal interface IMessageService
    {
        /// <summary>
        /// Sends a message to the appropriate recipient.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        void SendMessage(IMessage message);

        /// <summary>
        /// Registers a client to receive messages of a specific type.
        /// </summary>
        /// <param name="type">The type of message the client is interested in.</param>
        /// <param name="action">The action to be executed when a message of the specified type is received.</param>
        void RegisterClient(MessageType type, IMessageReceiver action);
    }
}
