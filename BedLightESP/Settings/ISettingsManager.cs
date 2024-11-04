namespace BedLightESP.Settings
{
    internal interface ISettingsManager
    {
        /// <summary>
        /// Gets or sets the application settings.
        /// </summary>
        AppSettings Settings { get; set; }

        /// <summary>
        /// Gets a value indicating whether the configuration file exists.
        /// </summary>
        bool IsConfigFileExisting { get; }

        /// <summary>
        /// Deletes the configuration file if it exists.
        /// </summary>
        void ClearConfig();

        /// <summary>
        /// Writes the application settings to the configuration file.
        /// </summary>
        /// <param name="settings">The settings to write.</param>
        void WriteSettings();

        /// <summary>
        /// Loads the application settings from the configuration file.
        /// </summary>
        /// <returns>The loaded settings, or default settings if the file does not exist or an error occurred.</returns>
        void LoadSettings();
    }
}
