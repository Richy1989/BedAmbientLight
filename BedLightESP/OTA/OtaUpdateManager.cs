using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace BedLightESP.OTA
{
    /// <summary>
    /// Manages the OTA (Over The Air) update process for the BedLightESP application.
    /// </summary>
    internal class OtaUpdateManager
    {
        private const string OtaRunnerName = "BedLightESP.OtaRunner";
        private static Assembly toRun = null;
        private static MethodInfo stop = null;

        /// <summary>
        /// Gets or sets the root path for the OTA update files.
        /// </summary>
        public static string RootPath { get; set; } = "I:\\OTA";

        /// <summary>
        /// Updates the application by stopping the current running instance, loading new assemblies, and starting the updated instance.
        /// </summary>
        public static void Update()
        {
            if (stop == null)
            {
                var currentAssembly = Assembly.GetAssembly(typeof(OtaUpdateManager));
                Type typeToRun = currentAssembly.GetType(OtaRunnerName);
                stop = typeToRun.GetMethod("Stop");
            }

            try
            {
                //Stop the current running application
                stop.Invoke(null, null);

                // Process the update
                ProcessUpdate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Processes the update by loading new assemblies and starting the updated instance.
        /// </summary>
        private static void ProcessUpdate()
        {
            LoadAssemblies();

            if (toRun != null)
            {
                Type typeToRun = toRun.GetType(OtaRunnerName);
                var start = typeToRun.GetMethod("Start");
                stop = typeToRun.GetMethod("Stop");

                if (start != null)
                {
                    try
                    {
                        // See if all goes right
                        start.Invoke(null, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error starting {OtaRunnerName}. Exception {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Loads the assemblies from the specified root path.
        /// </summary>
        public static void LoadAssemblies()
        {
            var files = Directory.GetFiles(RootPath);
            foreach (var file in files)
            {
                if (file.EndsWith(".pe"))
                {
                    using FileStream fspe = new(file, FileMode.Open, FileAccess.Read);
                    Debug.WriteLine($"{file}: {fspe.Length}");
                    var buff = new byte[fspe.Length];
                    fspe.Read(buff, 0, buff.Length);
                    // Needed as so far, there seems to be an issue when loading them too fast
                    fspe.Close();
                    fspe.Dispose();
                    Thread.Sleep(20);

                    var ass = Assembly.Load(buff);
                    var typeToRun = ass.GetType(OtaRunnerName);
                    if (typeToRun != null)
                    {
                        toRun = ass;
                    }
                }
            }
        }
    }
}
