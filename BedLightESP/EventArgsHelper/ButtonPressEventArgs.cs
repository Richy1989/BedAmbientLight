using BedLightESP.Enumerations;
using System;

namespace BedLightESP.EventArgsHelper
{
    /// <summary>
    /// Represents the event arguments for a button press event.
    /// </summary>
    public class ButtonPressEventArgs : EventArgs
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
        /// Initializes a new instance of the <see cref="ButtonPressEventArgs"/> class.
        /// </summary>
        /// <param name="buttonPosition">The position of the button.</param>
        /// <param name="clickType">The type of click performed on the button.</param>
        /// <param name="time">The time when the button was pressed.</param>
        public ButtonPressEventArgs(ButtonPosition buttonPosition, ClickType clickType, DateTime time)
        {
            ButtonPosition = buttonPosition;
            Time = time;
            ClickType = clickType;
        }
    }
}
