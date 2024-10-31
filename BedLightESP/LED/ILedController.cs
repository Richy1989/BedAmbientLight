using System;
using System.Drawing;

namespace BedLightESP.LED
{
    internal interface ILedController : IDisposable
    {
        /// <summary>Gets or sets the global brightness of the LEDs.</summary>
        int GlobalBrightness { get; set; }

        /// <summary>Gets or sets the colors of the pixels in the strip.</summary>
        Color[] Pixels { get; set; }

        /// <summary>
        /// Flushes the pixel data to the LED strip.
        /// </summary>
        void Flush();

        /// <summary>
        /// Increases the brightness of the LEDs.
        /// </summary>
        void IncreaseBrightness();
    }
}
