using System.Drawing;
using BedLightESP.Enumerations;

namespace BedLightESP.LED
{
    internal interface ILedManager
    {
        /// <summary>
        /// Creates the LED device.
        /// </summary>
        /// <param name="length">The length of the LED strip.</param>
        void CreateLEDDevice(int length);

        /// <summary>
        /// Turns on the LED strip with an array of colors.
        /// </summary>
        /// <param name="side">The side of the LED strip to turn on.</param>
        /// <param name="color">The array of colors.</param>
        void TurnOnLEDStripArrayColor(LedStripSide side, Color[] color);

        /// <summary>
        /// Turns off the LED strip.
        /// </summary>
        void TurnOffLEDStrip();

        /// <summary>
        /// Turns on the LED strip with a single color.
        /// </summary>
        /// <param name="side">The side of the LED strip to turn on.</param>
        /// <param name="color">The color.</param>
        void TurnOnLEDStripOneColor(LedStripSide side, Color color);
    }
}
