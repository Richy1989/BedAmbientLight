using System;
using System.Collections;
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
    internal class Logger : ILogger
    {
        /// <summary>
        /// Gets or sets the instance of the logger.
        /// </summary>
        public static ILogger Instance { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class and sets the instance.
        /// </summary>
        public Logger()
        {
            Instance = this;
        }

        /// <summary>
        /// Buffer to store the last 20 log messages.
        /// </summary>
        private readonly IList lastMessages = new ArrayList();

        /// <summary>
        /// Index to keep track of the last message in the buffer.
        /// </summary>
        private int lastMessageIndex = 0;
        private bool listIsFull = false;

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The level of the log message.</param>
        /// <param name="message">The message to log.</param>
        private void Log(LogLevel level, string message)
        {
            if (Debugger.IsAttached)
            {
                string logMessage = $"{DateTime.UtcNow.ToString("o")} [{GetLogLevelString(level)}] {message}";
                AddToBuffer(logMessage);
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Adds a message to the buffer.
        /// </summary>
        /// <param name="message">The message to add to the buffer.</param>
        private void AddToBuffer(string message)
        {
            if (lastMessages.Count >= 20)
            {
                listIsFull = true;
                lastMessageIndex = 0;
            }

            if(!listIsFull)
            {
                lastMessages.Add(message);
            }
            else
            {
                lastMessages[lastMessageIndex] = message;

            }
            lastMessageIndex++;
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message to log.</param>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Converts the log level enum to its string representation.
        /// </summary>
        /// <param name="level">The log level to convert.</param>
        /// <returns>A string representation of the log level.</returns>
        private string GetLogLevelString(LogLevel level)
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

        /// <summary>
        /// Retrieves the last 20 log messages.
        /// </summary>
        /// <returns>An array of the last 20 log messages.</returns>
        public IList GetLogMessages()
        {
            return lastMessages;
        }
    }
}
