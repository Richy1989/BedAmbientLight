using System.Device.Spi;
using System.Drawing;
using BedLightESP.Enumerations;
using BedLightESP.EventArgsHelper;
using BedLightESP.Helper;
using BedLightESP.LED;
using BedLightESP.Logging;
using nanoFramework.Hardware.Esp32;

namespace BedLightESP.Manager
{
    internal class LEDManager
    {
        private APA102Controller apa102;
        private readonly TouchManager touchManager;
        private bool leftIsOn = false;
        private bool rightIsOn = false;
        private bool wholeIsOn = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LEDManager"/> class.
        /// </summary>
        /// <param name="length">The length of the LED strip.</param>
        /// <param name="touchManager">The touch manager.</param>
        public LEDManager(int length, TouchManager touchManager)
        {
            CreateLEDDevice(length);
            this.touchManager = touchManager;
            this.touchManager.ButtonPressed += OnButtonPressed;
        }

        /// <summary>
        /// Creates the LED device.
        /// </summary>
        /// <param name="length">The length of the LED strip.</param>
        public void CreateLEDDevice(int length)
        {
            Configuration.SetPinFunction(23, DeviceFunction.SPI1_MISO);
            Configuration.SetPinFunction(19, DeviceFunction.SPI1_MISO);
            Configuration.SetPinFunction(18, DeviceFunction.SPI1_CLOCK);

            var spiDevice = SpiDevice.Create(new SpiConnectionSettings(1, 12)
            {
                ClockFrequency = 20_000_000,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode0 // ensure data is ready at clock rising edge
            });

            apa102 = new APA102Controller(spiDevice, length);

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

            for (var i = 0; i < apa102.Pixels.Length; i++)
            {
                if (side == LedStripSide.Left)
                {
                    if (i < apa102.Pixels.Length / 2)
                    {
                        if (!leftIsOn)
                            apa102.Pixels[i] = color[i];
                        else
                            apa102.Pixels[i] = Color.Black;
                    }
                }
                else if (side == LedStripSide.Right)
                {
                    if (i >= apa102.Pixels.Length / 2)
                    {
                        if (!rightIsOn)
                            apa102.Pixels[i] = color[i];
                        else
                            apa102.Pixels[i] = Color.Black;
                    }
                }
                else if (side == LedStripSide.Whole) // Fixed spelling error
                {
                    apa102.Pixels[i] = color[i];
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

            apa102.Flush();
        }

        /// <summary>
        /// Turns off the LED strip.
        /// </summary>
        public void TurnOffLEDStrip()
        {
            for (var i = 0; i < apa102.Pixels.Length; i++)
            {
                apa102.Pixels[i] = Color.Black;
            }

            leftIsOn = false;
            rightIsOn = false;
            wholeIsOn = false; // Fixed spelling error

            apa102.Flush();
        }

        /// <summary>
        /// Turns on the LED strip with a single color.
        /// </summary>
        /// <param name="side">The side of the LED strip to turn on.</param>
        /// <param name="color">The color.</param>
        public void TurnOnLEDStripOneColor(LedStripSide side, Color color)
        {
            Color[] colors = new Color[apa102.Pixels.Length];

            //populate colors with color
            for (var i = 0; i < apa102.Pixels.Length; i++)
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
            if (e.ButtonPosition == ButtonPosition.Left)
            {
                if (e.ClickType.Equals(ClickType.Single))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Left, ColorHelper.WarmWhite);
                }
                if (e.ClickType.Equals(ClickType.Double))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Whole, ColorHelper.WarmWhite); // Fixed spelling error
                }
                if (e.ClickType.Equals(ClickType.DoubleHold))
                {
                    Logger.Info("Random Color!");
                    TurnOnLEDStripArrayColor(LedStripSide.Whole, ColorHelper.CalculateRandomGradientHUE(apa102.Pixels.Length)); // Fixed spelling error
                }
            }
            else if (e.ButtonPosition == ButtonPosition.Right)
            {
                if (e.ClickType.Equals(ClickType.Single))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Right, ColorHelper.WarmWhite);
                }
                if (e.ClickType.Equals(ClickType.Double))
                {
                    TurnOnLEDStripOneColor(LedStripSide.Whole, ColorHelper.WarmWhite); // Fixed spelling error
                }
                if (e.ClickType.Equals(ClickType.DoubleHold))
                {
                    Logger.Info("Random Color!");
                    TurnOnLEDStripArrayColor(LedStripSide.Whole, ColorHelper.CalculateRandomGradientHUE(apa102.Pixels.Length)); // Fixed spelling error
                }
            }

            if (e.ClickType == ClickType.SingleHold)
            {
                apa102.IncreaseBrightness();
                apa102.Flush();
            }
        }
    }
}
