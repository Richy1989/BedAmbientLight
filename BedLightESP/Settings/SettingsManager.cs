using System;
using System.IO;
using System.Text;
using BedLightESP.Logging;
using nanoFramework.Json;

namespace BedLightESP.Settings
{
    /// <summary>
    /// Helper class for managing application settings.
    /// </summary>
    public class SettingsManager
    {
        /// <summary>
        /// The path to the configuration file.
        /// </summary>
        private static readonly string _configFile = "I:\\configuration.json";

        /// <summary>
        /// Gets or sets the application settings.
        /// </summary>
        public static AppSettings Settings { get; set; } = new AppSettings();

        /// <summary>
        /// Gets a value indicating whether the configuration file exists.
        /// </summary>
        public static bool IsConfigFileExisting => File.Exists(_configFile);

        /// <summary>
        /// Deletes the configuration file if it exists.
        /// </summary>
        public static void ClearConfig()
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
        /// <returns>True if the settings were successfully written; otherwise, false.</returns>
        public static bool WriteSettings()
        {
            try
            {
                var configJson = JsonConvert.SerializeObject(Settings);

                var json = new FileStream(_configFile, FileMode.Create);

                byte[] buffer = Encoding.UTF8.GetBytes(configJson);
                json.Write(buffer, 0, buffer.Length);
                json.Dispose();

                Logger.Debug("Settings saved");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads the application settings from the configuration file.
        /// </summary>
        /// <returns>The loaded settings, or default settings if the file does not exist or an error occurred.</returns>
        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    var json = new FileStream(_configFile, FileMode.Open);
                    var settings = (AppSettings)JsonConvert.DeserializeObject(json, typeof(AppSettings));
                    Settings = settings;
                    Logger.Info($"Settings read.");
                    json.Close();
                    json.Dispose();
                    return;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Logger.Error($"Error loading settings: {ex.Message}");
            }

            Logger.Info("No config found. Creating new one.");
            // Return default settings if file does not exist or an error occurred
            Settings = new AppSettings();
        }
    }
}
