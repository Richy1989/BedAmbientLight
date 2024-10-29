using System;
using System.Device.Spi;
using System.Diagnostics;
using System.Drawing;


namespace BedLightESP.LED
{
    /// <summary>
    /// Represents a controller for APA102 LED strip.
    /// </summary>
    public class APA102Controller : IDisposable
    {
        /// <summary>Gets or sets the count of LEDs in the strip.</summary>
        public int LedCount { get; set; }

        /// <summary>Gets or sets the colors of the pixels in the strip.</summary>
        public Color[] Pixels { get; set; }

        /// <summary>Gets or sets the global brightness of the LEDs.</summary>
        public int GlobalBrightness { get; set; } = 255;

        /// <summary>Object for communicating with the LED strip.</summary>
        private readonly SpiDevice spiDevice;

        /// <summary>Size of the end frame.</summary>
        private readonly int endFrameSize;

        /// <summary>Indicates whether the object has been disposed.</summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="APA102Controller"/> class.
        /// </summary>
        /// <param name="device">The SPI device used for communication.</param>
        /// <param name="ledCount">The count of LEDs in the strip.</param>
        public APA102Controller(SpiDevice device, int ledCount)
        {
            Pixels = new Color[ledCount];
            LedCount = ledCount;

            endFrameSize = (int)Math.Ceiling((((double)LedCount) - 1.0) / 16.0);

            spiDevice = device;
        }

        /// <summary>
        /// Flushes the pixel data to the LED strip.
        /// </summary>
        public void Flush()
        {
            var buffer = new byte[(LedCount + 1) * 4 + endFrameSize];

            // Insert Start Frame
            for (int i = 0; i < 4; i++)
            {
                buffer[i] = 0x00;
            }

            // Insert End Frame
            for (int i = (LedCount + 1) * 4; i < ((LedCount + 1) * 4) + endFrameSize; i++)
            {
                buffer[i] = 0x00;
            }

            // Insert Pixel Data
            for (int i = 0; i < Pixels.Length; i++)
            {
                SpanByte pixel = buffer;
                pixel = pixel.Slice((i + 1) * 4);
                pixel[0] = (byte)((GlobalBrightness >> 3) | 0b11100000); // global brightness (alpha)
                pixel[1] = Pixels[i].B; // blue
                pixel[2] = Pixels[i].G; // green
                pixel[3] = Pixels[i].R; // red
            }

            // Send data to the LED strip
            try
            {
                spiDevice.Write(buffer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Releases the resources used by the <see cref="APA102Controller"/> object.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                spiDevice?.Dispose();
            }

            disposed = true;
        }

        /// <summary>
        /// Releases the resources used by the <see cref="APA102Controller"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Increases the global brightness of the LEDs by 5 units. 
        /// If the brightness exceeds 255, it wraps around to 5.
        /// </summary>
        public void IncreaseBrightness()
        {
            GlobalBrightness += 10;
            if (GlobalBrightness >= 255)
            {
                GlobalBrightness = 20;
            }
        }
    }
}
