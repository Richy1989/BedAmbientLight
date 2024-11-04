namespace BedLightESP.Settings
{
    internal class SpiSettings
    {
        /// <summary>
        /// Gets or sets the GPIO pin number for the MOSI (Master Out Slave In).
        /// </summary>
        public int MosiPin { get; set; } = 11;
        /// <summary>
        /// Gets or sets the GPIO pin number for the MISO (Master In Slave Out).
        /// </summary>
        public int MisoPin { get; set; } = 13;

        /// <summary>
        /// Gets or sets the GPIO pin number for the clock (CLK).
        /// </summary>
        public int ClkPin { get; set; } = 12;
        /// <summary>
        /// Gets or sets the GPIO pin number for the chip select (CS).
        /// </summary>
        public int ChipSelectPin { get; set; } = 10;
        /// <summary>
        /// Gets or sets the GPIO pin number for the bus ID.
        /// </summary>
        public int BusIDPin { get; set; } = 2;
    }
}
