using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BedLightESP.OTA
{
    internal class OtaUpdateManager
    {
        private const string OtaRunnerName = "BedLightESP.OTA.OtaRunner";
        Assembly toRun = null;
        MethodInfo stop = null;
        bool isRunning = false;

        public static string RootPath { get; set; } = "I:\\OTA";

        public void Update()
        {
            // Now load the assemblies, they must be on the disk
            if (!isRunning)
            {
                LoadAssemblies();

                isRunning = false;
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
                            isRunning = true;
                        }
                        catch (Exception)
                        {
                            isRunning = false;
                        }
                    }
                }
            }
        }

        public void LoadAssemblies()
        {
            string RootPath = "";

            var files = Directory.GetFiles(RootPath);
            foreach (var file in files)
            {
                if (file.EndsWith(".pe"))
                {
                    using FileStream fspe = new (file, FileMode.Open, FileAccess.Read);
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
