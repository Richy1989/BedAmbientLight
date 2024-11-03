using System;
using System.Diagnostics;
using System.Threading;
using BedLightESP.Logging;

namespace BedLightESP.Helper
{
    internal static class DebugHelper
    {
        public static void StartMemoryDumpTask()
        {
            if (!Debugger.IsAttached)
                return;

            nanoFramework.Runtime.Native.GC.EnableGCMessages(true);

            new Thread(() =>
            {
                while (true)
                {
                    Logger.Debug($"Free memory = {nanoFramework.Runtime.Native.GC.Run(true)}");
                    Thread.Sleep(TimeSpan.FromMinutes(10));
                }
            }).Start();
        }
    }
}
