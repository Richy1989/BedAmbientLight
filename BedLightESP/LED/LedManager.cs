using System.Drawing;
using BedLightESP.Enumerations;
using BedLightESP.EventArgsHelper;
using BedLightESP.Helper;
using BedLightESP.Logging;
using BedLightESP.Settings;
using BedLightESP.Touch;

namespace BedLightESP.LED
{
    internal class LEDManager : ILedManager
    {
        private readonly ITouchManager touchManager;
        private readonly ISettingsManager _settingsManager;
        private bool leftIsOn = false;
        private bool rightIsOn = false;
        private bool wholeIsOn = false;
        private ILedController ledController;

        /// <summary>
        /// Initializes a new instance of the <see cref="LEDManager"/> class.
        /// </summary>
        /// <param name="length">The length of the LED strip.</param>
        /// <param name="touchManager">The touch manager.</param>
        public LEDManager(ITouchManager touchManager, ISettingsManager settingsManager)
        {
            this.touchManager = touchManager;
            this._settingsManager = settingsManager;
            this.touchManager.ButtonPressed += OnButtonPressed;
        }

        /// <summary>
        /// Creates the LED device.
        /// </summary>
        /// <param name="length">The length of the LED strip.</param>
        public void CreateLEDDevice(int length)
        {
            // Create the LED controller
            ledController = new APA102Controller(length, _settingsManager);

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
        /// Event handler for button pressed event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressEventArgs e)
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
                    TurnOnLEDStripOneColor(LedStripSide.Whole, defaultColor); // Fixed spelling error
                }
                if (e.ClickType.Equals(ClickType.DoubleHold))
                {
                    Logger.Info("Random Color!");
                    TurnOnLEDStripArrayColor(LedStripSide.Whole, ColorHelper.CalculateRandomGradientHUE(ledController.Pixels.Length)); // Fixed spelling error
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
                    TurnOnLEDStripOneColor(LedStripSide.Whole, defaultColor); // Fixed spelling error
                }
                if (e.ClickType.Equals(ClickType.DoubleHold))
                {
                    Logger.Info("Random Color!");
                    TurnOnLEDStripArrayColor(LedStripSide.Whole, ColorHelper.CalculateRandomGradientHUE(ledController.Pixels.Length)); // Fixed spelling error
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
