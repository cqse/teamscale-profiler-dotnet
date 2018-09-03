using IWshRuntimeLibrary;

namespace ProfilerGUI.Source.Configurator
{
    public class WindowsShortcutReader
    {
        public const string ShortcutExtension = ".lnk";

        public static ShortcutInfo ReadShortcutInfo(string filePath)
        {
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(filePath);
            return new ShortcutInfo(link.TargetPath, link.WorkingDirectory, link.Arguments);
        }


        public class ShortcutInfo
        {
            public string TargetPath { get; }
            public string WorkingDirectory { get; }
            public string Arguments { get; }

            public ShortcutInfo(string targetPath, string workingDirectory, string arguments)
            {
                TargetPath = targetPath;
                WorkingDirectory = workingDirectory;
                Arguments = arguments;
            }
        }
    }
}
