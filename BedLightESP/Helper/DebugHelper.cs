using System;
using System.Diagnostics;
using System.Threading;
using BedLightESP.Logging;
using nanoFramework.Hardware.Esp32;

namespace BedLightESP.Helper
{
    /// <summary>
    /// Provides helper methods for debugging, including memory dump tasks.
    /// </summary>
    internal static class DebugHelper
    {
        /// <summary>
        /// Starts a background task that periodically prints memory information if a debugger is attached.
        /// </summary>
        public static void StartMemoryDumpTask()
        {
            if (!Debugger.IsAttached)
                return;

            nanoFramework.Runtime.Native.GC.EnableGCMessages(true);

            new Thread(() =>
            {
                while (true)
                {
                    PrintMemory();
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }).Start();
        }

        /// <summary>
        /// Prints the current memory information to the debug output.
        /// </summary>
        public static void PrintMemory()
        {
            NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out uint totalSize, out uint totalFree, out uint largestFree);
            Logger.Instance?.Debug($"Internal Mem:  Total Internal: {totalSize} Free: {totalFree} Largest: {largestFree}");
            Logger.Instance?.Debug($"Free nanoFramework Mem:  {nanoFramework.Runtime.Native.GC.Run(false)}");
        }
    }
}
