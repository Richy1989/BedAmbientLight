using System;
using System.IO;
using System.Text;
using System.Threading;
using BedLightESP.Logging;
using nanoFramework.Json;

namespace BedLightESP.Settings
{
    /// <summary>
    /// Helper class for managing application settings.
    /// </summary>
    internal class SettingsManager : ISettingsManager
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsManager"/> class.
        /// </summary>
        /// <param name="logger">The logger used to log messages.</param>
        public SettingsManager(ILogger logger)
        {
            _logger = logger;
        }   
        /// <summary>
        /// The path to the configuration file.
        /// </summary>
        private readonly string _configFile = "I:\\configuration.json";

        /// <summary>
        /// Gets or sets the application settings.
        /// </summary>
        public AppSettings Settings { get; set; } = new AppSettings();

        /// <summary>
        /// Gets a value indicating whether the configuration file exists.
        /// </summary>
        public bool IsConfigFileExisting => File.Exists(_configFile);

        /// <summary>
        /// Deletes the configuration file if it exists.
        /// </summary>
        public void ClearConfig()
        {
            if (File.Exists(_configFile))
            {
                File.Delete(_configFile);
            }
        }

        /// <summary>
        /// Writes the application settings to the configuration file.
        /// </summary>
        /// <param name="settings">The settings to write.</param>
        public void WriteSettings()
        {
            new Thread(() =>
            {
                try
                {
                    var configJson = JsonConvert.SerializeObject(Settings);

                    var json = new FileStream(_configFile, FileMode.Create);

                    byte[] buffer = Encoding.UTF8.GetBytes(configJson);
                    json.Write(buffer, 0, buffer.Length);
                    json.Dispose();

                    _logger.Debug("Settings saved");

                }
                catch (Exception ex)
                {
                    _logger.Error($"Error saving settings: {ex.Message}");
                }
            }).Start();
        }

        /// <summary>
        /// Loads the application settings from the configuration file.
        /// </summary>
        /// <returns>The loaded settings, or default settings if the file does not exist or an error occurred.</returns>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    var json = new FileStream(_configFile, FileMode.Open);
                    var settings = (AppSettings)JsonConvert.DeserializeObject(json, typeof(AppSettings));
                    Settings = settings;
                    _logger.Info($"Settings read.");
                    json.Close();
                    json.Dispose();
                    return;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                _logger.Error($"Error loading settings: {ex.Message}");
            }

            _logger.Info("No config found. Creating new one.");
            // Return default settings if file does not exist or an error occurred
            Settings = new AppSettings();
        }
    }
}
