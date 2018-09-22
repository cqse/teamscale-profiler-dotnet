using IWshRuntimeLibrary;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Represents the relevant info from a .lnk file.
    /// </summary>
    public class Shortcut
    {
        /// <summary>
        /// The extension of shortcut files on Windows.
        /// </summary>
        public const string Extension = ".lnk";

        /// <summary>
        /// Parses the given .lnk file.
        /// </summary>
        public static Shortcut ReadShortcutFile(string filePath)
        {
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(filePath);
            return new Shortcut(link.TargetPath, link.WorkingDirectory, link.Arguments);
        }

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
        public Shortcut(string targetPath, string workingDirectory, string arguments)
        {
            TargetPath = targetPath;
            WorkingDirectory = workingDirectory;
            Arguments = arguments;
        }
    }
}