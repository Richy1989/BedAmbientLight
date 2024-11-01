using System;
using BedLightESP.Enumerations;

namespace BedLightESP.Messages
{
    internal class TouchMessage : IMessage
    {
        /// <summary>
        /// Gets the position of the button.
        /// </summary>
        public ButtonPosition ButtonPosition { get; }

        /// <summary>
        /// Gets the type of click performed on the button.
        /// </summary>
        public ClickType ClickType { get; }

        /// <summary>
        /// Gets the time when the button was pressed.
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// Gets the type of message.
        /// </summary>
        public MessageType MessageType => MessageType.Touch;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonPressEventArgs"/> class.
        /// </summary>
        /// <param name="buttonPosition">The position of the button.</param>
        /// <param name="clickType">The type of click performed on the button.</param>
        /// <param name="time">The time when the button was pressed.</param>
        public TouchMessage(ButtonPosition buttonPosition, ClickType clickType, DateTime time)
        {
            ButtonPosition = buttonPosition;
            Time = time;
            ClickType = clickType;
        }
    }
}
