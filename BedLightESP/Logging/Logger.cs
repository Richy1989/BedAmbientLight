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


    public static class Logger
    {
        public static void Log(LogLevel level, string message)
        {
            if (Debugger.IsAttached)
            {
                string logMessage = $"{DateTime.UtcNow.ToString("o")} [{GetLogLevelString(level)}] {message}";
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        private static string GetLogLevelString(LogLevel level)
        {
            if(level == LogLevel.Info)
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
