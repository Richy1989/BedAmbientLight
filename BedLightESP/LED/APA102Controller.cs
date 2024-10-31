using System;
using System.Device.Spi;
using System.Diagnostics;
using System.Drawing;
using BedLightESP.Settings;
using nanoFramework.Hardware.Esp32;


namespace BedLightESP.LED
{
    /// <summary>
    /// Represents a controller for APA102 LED strip.
    /// </summary>
    internal class APA102Controller : ILedController
    {
        /// <summary>Gets or sets the count of LEDs in the strip.</summary>
        public int LedCount { get; set; }

        /// <summary>Gets or sets the colors of the pixels in the strip.</summary>
        public Color[] Pixels { get; set; }

        /// <summary>Gets or sets the global brightness of the LEDs.</summary>
        public int GlobalBrightness { get; set; } = 255;

        /// <summary>Size of the end frame.</summary>
        private readonly int endFrameSize;

        /// <summary>Represents the SPI device.</summary>
        private readonly SpiDevice spiDevice;

        /// <summary>Indicates whether the object has been disposed.</summary>
        private bool disposed;


        /// <summary>
        /// Initializes a new instance of the <see cref="APA102Controller"/> class.
        /// </summary>
        /// <param name="ledCount">The number of LEDs in the strip.</param>
        /// <param name="settingsManager">The settings manager to configure the SPI pins.</param>
        internal APA102Controller(int ledCount, ISettingsManager settingsManager)
        {
            Configuration.SetPinFunction(settingsManager.Settings.MosiPin, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(settingsManager.Settings.ClkPin, DeviceFunction.SPI1_CLOCK);

            var spiDevice = SpiDevice.Create(new SpiConnectionSettings(1, 12)
            {
                ClockFrequency = 20_000_000,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode0 // ensure data is ready at clock rising edge
            });

            Pixels = new Color[ledCount];
            LedCount = ledCount;

            endFrameSize = (int)Math.Ceiling((((double)LedCount) - 1.0) / 16.0);

            this.spiDevice = spiDevice;
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
