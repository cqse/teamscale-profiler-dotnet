
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    /// <summary>
    /// A Teamscale Ephemeral profiler in TGA mode.
    /// </summary>
    public class Profiler : IProfiler
    {
        /** The profiler's class ID. */
        private const string PROFILER_CLASS_ID = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

        /** Environment variable name to enable the profiler. */
        private const string PROFILER_ENABLE_KEY = "COR_ENABLE_PROFILING";

        /** Environment variable name to enable the upload daemon. */
        private const string PROFILER_UPLOAD_DAEMON_KEY = "PROFILER_UPLOAD_DAEMON";

        /** Environment variable name for the profiler's class ID. */
        private const string PROFILER_CLASS_ID_KEY = "COR_PROFILER";

        /** Environment variable name for the directory to store profiler traces. */
        private const string PROFILER_TARGETDIR_KEY = "COR_PROFILER_TARGETDIR";

        /** Environment variable name for the path to the profiler DLL. */
        private const string PROFILER_PATH_KEY = "COR_PROFILER_PATH";

        /** Environment variable name to enable the profiler's light mode. */
        private const string PROFILER_LIGHT_MODE_KEY = "COR_PROFILER_LIGHT_MODE";

        /** Environment variable to name a single process to trace */
        private const string PROFILER_PROCESS_KEY = "COR_PROFILER_PROCESS";

        /** Environment variable to point to the profiler config file */
        private const string PROFILER_CONFIG_FILE_KEY = "COR_PROFILER_CONFIG";

        /** Environment variable specifying the app pool Id */
        private const string PROFILER_APP_POOL_ID_KEY = "APP_POOL_ID";

#if DEBUG

        /// <summary>
        /// Field holding the build configuration, either 'Release' or 'Debug'
        /// </summary>
        protected static readonly string Configuration = "Debug";

#else

        /// <summary>
        /// Field holding the build configuration, either 'Release' or 'Debug'
        /// </summary>
        protected static readonly string Configuration = "Release";

#endif

        public readonly string Profiler32Dll;
        public readonly string Profiler64Dll;
        public readonly string AttachLog;

        private readonly DirectoryInfo targetDir;

        public bool LightMode { get; set; } = true;

        public string TargetProcessName { get; set; } = null;

        public string ConfigFilePath { get; set; } = null;

        public string AppPoolId { get; set; } = null;

        public Profiler(DirectoryInfo basePath, DirectoryInfo targetDir)
        {
            Profiler32Dll = $"{basePath}/Profiler/bin/{Configuration}/Profiler32.dll";
            Profiler64Dll = $"{basePath}/Profiler/bin/{Configuration}/Profiler64.dll";
            AttachLog = $"{basePath}/Profiler/bin/{Configuration}/attach.log";

            Assume.That(File.Exists(Profiler32Dll), "Could not find profiler 32bit DLL at " + Profiler32Dll);
            Assume.That(File.Exists(Profiler64Dll), "Could not find profiler 64bit DLL at " + Profiler64Dll);
            File.Delete(AttachLog);

            this.targetDir = targetDir;
        }

        private string GetProfilerDll(Bitness bitness)
        {
            string profilerDll = Profiler32Dll;
            if (bitness == Bitness.x64)
            {
                profilerDll = Profiler64Dll;
            }

            return Path.GetFullPath(profilerDll);
        }

        /// <inheritDoc/>
        public virtual void RegisterOn(ProcessStartInfo processInfo, Bitness? bitness = null)
        {
            ClearProfilerRegistration(processInfo);

            // set environment variables for the profiler
            processInfo.Environment[PROFILER_PATH_KEY] = GetProfilerDll(bitness ?? Bitness.x64);
            processInfo.Environment[PROFILER_TARGETDIR_KEY] = targetDir.FullName;
            processInfo.Environment[PROFILER_CLASS_ID_KEY] = PROFILER_CLASS_ID;
            processInfo.Environment[PROFILER_ENABLE_KEY] = "1";
            processInfo.Environment[PROFILER_UPLOAD_DAEMON_KEY] = "0";

            if (LightMode)
            {
                processInfo.Environment[PROFILER_LIGHT_MODE_KEY] = "1";
            }

            if (TargetProcessName != null)
            {
                processInfo.Environment[PROFILER_PROCESS_KEY] = TargetProcessName;
            }

            if (ConfigFilePath != null)
            {
                processInfo.Environment[PROFILER_CONFIG_FILE_KEY] = ConfigFilePath;
            }

            if (AppPoolId != null)
            {
                processInfo.Environment[PROFILER_APP_POOL_ID_KEY] = AppPoolId;
            }
        }

        /// <summary>
        /// Clears the profiler environment variables to guarantee a stable test even if
        /// the developer has variables set on their development machine.
        /// </summary>
        internal static void ClearProfilerRegistration(ProcessStartInfo processInfo)
        {
            foreach (string variable in processInfo.Environment.Keys)
            {
                if (variable.StartsWith("COR"))
                {
                    processInfo.Environment[variable] = null;
                }
            }
            processInfo.Environment[PROFILER_APP_POOL_ID_KEY] = null;
        }

        /// <summary>
        /// Asserts that the profiler produced a single trace file and returns its contents.
        /// </summary>
        public string[] GetSingleTrace() => File.ReadAllLines(GetSingleTraceFile().FullName);

        /// <summary>
        /// Asserts that the profiler produced a single trace file and returns it.
        /// </summary>
        public FileInfo GetSingleTraceFile()
        {
            List<FileInfo> traces = GetTraceFiles();
            Assert.That(traces, Has.Count.GreaterThan(0), "No coverage trace was written.");
            Assert.That(traces, Has.Count.LessThanOrEqualTo(1), "More than one coverage trace was written: " + string.Join(", ", traces));
            return traces[0];
        }

        /// <summary>
        /// Returns the trace files in the output directory.
        /// </summary>
        public List<FileInfo> GetTraceFiles()
            => this.targetDir.EnumerateFiles().Where(file => file.Name.StartsWith("coverage_")).ToList();
        
        /// <summary>
        /// Return the attach log contents.
        /// </summary>
        public string[] GetAttachLog() => File.ReadAllLines(AttachLog);
    }
}