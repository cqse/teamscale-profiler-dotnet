namespace ProfilerGUI.Source.Shared
{
    /// <summary>
    /// Constants used when running the profiler.
    /// </summary>
    public static class ProfilerConstants
    {
        /// <summary>
        /// The environment variable to enable profiling.
        /// </summary>
        public const string EnableProfilingEnvironmentVariable = "COR_ENABLE_PROFILING";

        /// <summary>
        /// The environment variable that stores the GUID of the profiler.
        /// </summary>
        public const string ProfilerIdEnvironmentVariable = "COR_PROFILER";

        /// <summary>
        /// The GUID of the profiler.
        /// </summary>
        public const string ProfilerGuid = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

        /// <summary>
        /// The environment variable that stores the path to the profiler DLL.
        /// </summary>
        public const string ProfilerPathEnvironmentVariable = "COR_PROFILER_PATH";

        /// <summary>
        /// The environment variable to enable light mode.
        /// </summary>
        public const string LightModeEnvironmentVariable = "COR_PROFILER_LIGHT_MODE";

        /// <summary>
        /// The environment variable that stores the path to which trace files will be written.
        /// </summary>
        public const string TargetDirectoryEnvironmentVariable = "COR_PROFILER_TARGETDIR";

        /// <summary>
        /// The name of the 32bit profiler DLL.
        /// </summary>
        public const string ProfilerDll32Bit = "Profiler32.dll";

        /// <summary>
        /// The name of the 64bit profiler DLL.
        /// </summary>
        public const string ProfilerDll64Bit = "Profiler64.dll";
    }
}