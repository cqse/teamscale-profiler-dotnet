namespace ProfilerGUI.Source.Shared
{
    public static class ProfilerConstants
    {
        public const string DefaultConfigFile = "profiler_config.json";

        public const string EnableProfilingArg = "COR_ENABLE_PROFILING";

        public const string ProfilerIdArgName = "COR_PROFILER";

        public const string ProfilerIdArgValue = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

        public const string ProfilerPathArg = "COR_PROFILER_PATH";

        // TODO (FS) is this missing the COR_ prefix or is this on purpose?
        // if so, please document
        public const string ProfilerLightModeArg = "PROFILER_LIGHT_MODE";

        public const string TargetDirectoryForTracesArg = "COR_PROFILER_TARGETDIR";

        public const string ProfilerDll32Bit = "Profiler32.dll";

        public const string ProfilerDll64Bit = "Profiler64.dll";
    }
}
