using System.Drawing;
using BedLightESP.Enumerations;
using BedLightESP.Helper;
using BedLightESP.Messages;
using BedLightESP.Settings;

namespace BedLightESP.LED
{
    /// <summary>
    /// Manages the LED strip, including turning it on and off, setting colors, and handling touch messages.
    /// </summary>
    internal class LEDManager : ILedManager, IMessageReceiver
    {
        private readonly ISettingsManager _settingsManager;
        private bool leftIsOn = false;
        private bool rightIsOn = false;
        private bool wholeIsOn = false;
        private ILedController ledController;

        /// <summary>
        /// Initializes a new instance of the <see cref="LEDManager"/> class.
        /// </summary>
        /// <param name="settingsManager">The settings manager.</param>
        /// <param name="messageService">The message service.</param>
        public LEDManager(ISettingsManager settingsManager, IMessageService messageService)
        {
            this._settingsManager = settingsManager;

            // Register the LED manager as a message receiver
            messageService.RegisterClient(MessageType.Touch, this);
        }

        /// <summary>
        /// Executes the received message.
        /// </summary>
        /// <param name="message">The message to execute.</param>
        public void ExecuteMessage(IMessage message)
        {
            OnTouchMessageReceived(message as TouchMessage);
        }

        /// <summary>
        /// Creates the LED device.
        /// </summary>
        /// <param name="length">The length of the LED strip.</param>
        public void CreateLEDDevice(int length)
        {
            ledController = _settingsManager.Settings.LedControllerType switch
            {
                // Create the LED controller
                LedControllerType.APA102 => new APA102Controller(length, _settingsManager),
                _ => throw new System.Exception("Invalid LED controller type."),
            };

            // Turn off the LED strip by default when starting the application
            TurnOffLEDStrip();
        }

        /// <summary>
        /// Turns on the LED strip with an array of colors.
        /// </summary>
        /// <param name="side">The side of the LED strip to turn on.</param>
        /// <param name="color">The array of colors.</param>
        public void TurnOnLEDStripArrayColor(LedStripSide side, Color[] color)
        {
            if (wholeIsOn && side != LedStripSide.Whole) // Fixed spelling error
            {
                TurnOffLEDStrip();
                return;
            }

            for (var i = 0; i < ledController.Pixels.Length; i++)
            {
                if (side == LedStripSide.Left)
                {
                    if (i < ledController.Pixels.Length / 2)
                    {
                        if (!leftIsOn)
                            ledController.Pixels[i] = color[i];
                        else
                            ledController.Pixels[i] = Color.Black;
                    }
                }
                else if (side == LedStripSide.Right)
                {
                    if (i >= ledController.Pixels.Length / 2)
                    {
                        if (!rightIsOn)
                            ledController.Pixels[i] = color[i];
                        else
                            ledController.Pixels[i] = Color.Black;
                    }
                }
                else if (side == LedStripSide.Whole) // Fixed spelling error
                {
                    ledController.Pixels[i] = color[i];
                    wholeIsOn = true; // Fixed spelling error
                }
            }

            if (side == LedStripSide.Left)
            {
                leftIsOn = !leftIsOn;
            }
            else if (side == LedStripSide.Right)
            {
                rightIsOn = !rightIsOn;
            }

            ledController.Flush();
        }

        /// <summary>
        /// Turns off the LED strip.
        /// </summary>
        public void TurnOffLEDStrip()
        {
            for (var i = 0; i < ledController.Pixels.Length; i++)
            {
                ledController.Pixels[i] = Color.Black;
            }

            leftIsOn = false;
            rightIsOn = false;
            wholeIsOn = false; // Fixed spelling error

            ledController.Flush();
        }

        /// <summary>
        /// Turns on the LED strip with a single color.
        /// </summary>
        /// <param name="side">The side of the LED strip to turn on.</param>
        /// <param name="color">The color.</param>
        public void TurnOnLEDStripOneColor(LedStripSide side, Color color)
        {
            Color[] colors = new Color[ledController.Pixels.Length];

            //populate colors with color
            for (var i = 0; i < ledController.Pixels.Length; i++)
            {
                colors[i] = color;
            }

            TurnOnLEDStripArrayColor(side, colors);
        }

        /// <summary>
        /// Controls the LED strip based on the received touch message.
        /// </summary>
        /// <param name="side">The side of the LED strip to turn on.</param>
        /// <param name="color">The color.</param>
        private void OnTouchMessageReceived(TouchMessage e)
        {
            Color defaultColor = ColorHelper.HexToColor(_settingsManager.Settings.DefaultColor);

            if (e.ButtonPosition == ButtonPosition.Left)
            {
                if (e.ClickType.Equals(ClickType.Single))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Left, defaultColor);
                }
                if (e.ClickType.Equals(ClickType.Double))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Whole, defaultColor);
                }
                if (e.ClickType.Equals(ClickType.DoubleHold))
                {
                    TurnOnLEDStripArrayColor(LedStripSide.Whole, ColorHelper.CalculateRandomGradientHUE(ledController.Pixels.Length));
                }
            }
            else if (e.ButtonPosition == ButtonPosition.Right)
            {
                if (e.ClickType.Equals(ClickType.Single))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Right, defaultColor);
                }
                if (e.ClickType.Equals(ClickType.Double))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Whole, defaultColor); 
                }
                if (e.ClickType.Equals(ClickType.DoubleHold))
                {
                    TurnOnLEDStripArrayColor(LedStripSide.Whole, ColorHelper.CalculateRandomGradientHUE(ledController.Pixels.Length));
                }
            }

            if (e.ClickType == ClickType.SingleHold)
            {
                ledController.IncreaseBrightness();
                ledController.Flush();
            }
        }
    }
}
