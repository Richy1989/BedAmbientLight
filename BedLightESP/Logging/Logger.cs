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

        private const int BufferSize = 20;

        // Fixed-size ring buffer — no dynamic allocation after construction.
        private readonly string[] _messages = new string[BufferSize];
        private int _index = 0;   // next write position
        private int _count = 0;   // number of valid entries (caps at BufferSize)

        private void Log(LogLevel level, string message)
        {
            if (Debugger.IsAttached)
            {
                string logMessage = $"{System.DateTime.UtcNow.ToString("o")} [{GetLogLevelString(level)}] {message}";
                _messages[_index] = logMessage;
                _index = (_index + 1) % BufferSize;
                if (_count < BufferSize) _count++;
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
        }

        public void Info(string message)    => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message)   => Log(LogLevel.Error, message);
        public void Debug(string message)   => Log(LogLevel.Debug, message);

        private static string GetLogLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:    return "INFO";
                case LogLevel.Warning: return "WARNING";
                case LogLevel.Error:   return "ERROR";
                case LogLevel.Debug:   return "DEBUG";
                default:               return "UNKNOWN";
            }
        }

        /// <summary>
        /// Returns the buffered log messages in chronological order.
        /// </summary>
        public IList GetLogMessages()
        {
            var result = new ArrayList();
            int start = _count < BufferSize ? 0 : _index; // oldest entry
            for (int i = 0; i < _count; i++)
                result.Add(_messages[(start + i) % BufferSize]);
            return result;
        }
    }
}
