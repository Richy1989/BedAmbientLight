using System.Collections;

namespace BedLightESP.Messages
{
    /// <summary>
    /// Centralized Message Service to send messages from one manager to multiple others. 
    /// </summary>
    internal class MessageService : IMessageService
    {
        // List containing all registered services and actions
        private readonly IDictionary messageList;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageService"/> class.
        /// </summary>
        public MessageService()
        {
            messageList = new Hashtable();
        }

        /// <summary>
        /// Sends a message to the appropriate recipient.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public void SendMessage(IMessage message)
        {
            if (messageList.Contains(message.MessageType))
            {
                var list = (ArrayList)messageList[message.MessageType];
                foreach (IMessageReceiver action in list)
                {
                    action.ExecuteMessage(message);
                }
            }
        }

        /// <summary>
        /// Registers a client to receive messages of a specific type.
        /// </summary>
        /// <param name="type">The type of message the client is interested in.</param>
        /// <param name="action">The action to be executed when a message of the specified type is received.</param>
        public void RegisterClient(MessageType type, IMessageReceiver action)
        {
            if (!messageList.Contains(type))
            {
                var list = new ArrayList
                {
                    action
                };

                messageList.Add(type, list);
            }
            else
            {
                ((ArrayList)(messageList[type])).Add(action);
            }
        }
    }
}
