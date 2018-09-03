using IWshRuntimeLibrary;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Parses Windows .lnk files.
    /// </summary>
    public class WindowsShortcutReader
    {
        /// <summary>
        /// The extension of shortcut files on Windows.
        /// </summary>
        public const string ShortcutExtension = ".lnk";

        /// <summary>
        /// Parses the given .lnk file.
        /// </summary>
        public static ShortcutInfo ReadShortcutInfo(string filePath)
        {
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(filePath);
            return new ShortcutInfo(link.TargetPath, link.WorkingDirectory, link.Arguments);
        }

        /// <summary>
        /// Result of parsing a .lnk file.
        /// </summary>
        public class ShortcutInfo
        {
            /// <summary>
            /// Path of the executable the link points to.
            /// </summary>
            public string TargetPath { get; }

            /// <summary>
            /// Working directory to use when starting the TargetPath.
            /// </summary>
            public string WorkingDirectory { get; }

            /// <summary>
            /// Command line arguments to pass to the started executable.
            /// </summary>
            public string Arguments { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            public ShortcutInfo(string targetPath, string workingDirectory, string arguments)
            {
                TargetPath = targetPath;
                WorkingDirectory = workingDirectory;
                Arguments = arguments;
            }
        }
    }
}