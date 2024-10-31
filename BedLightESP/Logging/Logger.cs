using System;
using System.Diagnostics;

namespace BedLightESP.Logging
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    /// <summary>
    /// Provides logging functionality with different log levels.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The level of the log message.</param>
        /// <param name="message">The message to log.</param>
        public static void Log(LogLevel level, string message)
        {
            if (Debugger.IsAttached)
            {
                string logMessage = $"{DateTime.UtcNow.ToString("o")} [{GetLogLevelString(level)}] {message}";
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message to log.</param>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Converts the log level enum to its string representation.
        /// </summary>
        /// <param name="level">The log level to convert.</param>
        /// <returns>A string representation of the log level.</returns>
        private static string GetLogLevelString(LogLevel level)
        {
            if (level == LogLevel.Info)
            {
                return "INFO";
            }
            else if (level == LogLevel.Warning)
            {
                return "WARNING";
            }
            else if (level == LogLevel.Error)
            {
                return "ERROR";
            }
            else if (level == LogLevel.Debug)
            {
                return "DEBUG";
            }
            else
            {
                return "UNKNOWN";
            }
        }
    }
}
