using System;
using System.Diagnostics;
using System.Text;
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

            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    Logger.Debug($"Free memory = {nanoFramework.Runtime.Native.GC.Run(true)}");
                }
            }).Start();
        }
    }
}
